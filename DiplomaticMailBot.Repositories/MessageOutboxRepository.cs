using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.ServiceModels.MessageCandidate;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class MessageOutboxRepository
{
    private readonly ILogger<MessageOutboxRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public MessageOutboxRepository(
        ILogger<MessageOutboxRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task SendPendingMailsAsync(
        ProcessOutboxRecordCallback processOutboxRecordCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processOutboxRecordCallback);

        _logger.LogDebug("Sending pending mails");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var mailsToSend = await applicationDbContext.DiplomaticMailOutbox
            .Include(x => x.SlotInstance)
            .Include(mailOutbox => mailOutbox.DiplomaticMailCandidate)
            .ThenInclude(candidate => candidate!.SlotInstance)
            .ThenInclude(slot => slot!.FromChat)
            .Include(mailOutbox => mailOutbox.DiplomaticMailCandidate)
            .ThenInclude(candidate => candidate!.SlotInstance)
            .ThenInclude(slot => slot!.ToChat)
            .Where(x =>
                x.Status == MessageOutboxStatus.Pending
                && x.SentAt == null
                && x.Attempts < 3
                && ((x.SlotInstance!.Date == dateNow && x.SlotInstance!.Template!.VoteEndAt < timeNow)
                    || x.SlotInstance!.Date < dateNow))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var mailToSend in mailsToSend)
        {
            _logger.LogInformation("Sending mail {MailNumber} of {TotalMails}: {FromChat} -> {ToChat}. Attempts: {Attempts}. Preview: {Preview}",
                i,
                mailsToSend.Count,
                mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.ChatAlias,
                mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.ChatAlias,
                mailToSend.Attempts,
                mailToSend.DiplomaticMailCandidate!.Preview.TryLeft(50));

            try
            {
                try
                {
                    await processOutboxRecordCallback(
                        new RegisteredChatSm
                        {
                            Id = mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.Id,
                            ChatId = mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.ChatId,
                            ChatTitle = mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.ChatTitle,
                            ChatAlias = mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.ChatAlias,
                            CreatedAt = mailToSend.DiplomaticMailCandidate!.SlotInstance!.FromChat!.CreatedAt,
                        },
                        new RegisteredChatSm
                        {
                            Id = mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.Id,
                            ChatId = mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.ChatId,
                            ChatTitle = mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.ChatTitle,
                            ChatAlias = mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.ChatAlias,
                            CreatedAt = mailToSend.DiplomaticMailCandidate!.SlotInstance!.ToChat!.CreatedAt,
                        },
                        new MessageCandidateSm
                        {
                            MessageId = mailToSend.DiplomaticMailCandidate!.MessageId,
                            AuthorName = mailToSend.DiplomaticMailCandidate!.AuthorName,
                            Preview = mailToSend.DiplomaticMailCandidate!.Preview,
                        },
                        cancellationToken);

                    mailToSend.Status = MessageOutboxStatus.Sent;
                    mailToSend.SentAt = utcNow;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error sending diplomatic mail");
                }

                mailToSend.Attempts += 1;

                await applicationDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing diplomatic mail");
            }

            i++;
        }

        _logger.LogDebug("{Amount} mails sent", i - 1);
    }
}
