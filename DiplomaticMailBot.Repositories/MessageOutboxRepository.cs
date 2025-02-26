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

        var mailsToSend = await applicationDbContext.MessageOutbox
            .Include(x => x.SlotInstance)
            .Include(mailOutbox => mailOutbox.MessageCandidate)
            .ThenInclude(candidate => candidate.SlotInstance)
            .ThenInclude(slot => slot.FromChat)
            .Include(mailOutbox => mailOutbox.MessageCandidate)
            .ThenInclude(candidate => candidate.SlotInstance)
            .ThenInclude(slot => slot.ToChat)
            .Where(x =>
                x.Status == MessageOutboxStatus.Pending
                && x.SentAt == null
                && x.Attempts < 3
                && ((x.SlotInstance.Date == dateNow && x.SlotInstance.Template.VoteEndAt < timeNow)
                    || x.SlotInstance.Date < dateNow))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var mailToSend in mailsToSend)
        {
            _logger.LogInformation("Sending mail {MailNumber} of {TotalMails}: {FromChat} -> {ToChat}. Attempts: {Attempts}. Preview: {Preview}",
                i,
                mailsToSend.Count,
                mailToSend.MessageCandidate.SlotInstance.FromChat.ChatAlias,
                mailToSend.MessageCandidate.SlotInstance.ToChat.ChatAlias,
                mailToSend.Attempts,
                mailToSend.MessageCandidate.Preview.TryLeft(50));

            try
            {
                try
                {
                    await processOutboxRecordCallback(
                        new RegisteredChatSm
                        {
                            Id = mailToSend.MessageCandidate.SlotInstance.FromChat.Id,
                            ChatId = mailToSend.MessageCandidate.SlotInstance.FromChat.ChatId,
                            ChatTitle = mailToSend.MessageCandidate.SlotInstance.FromChat.ChatTitle,
                            ChatAlias = mailToSend.MessageCandidate.SlotInstance.FromChat.ChatAlias,
                            CreatedAt = mailToSend.MessageCandidate.SlotInstance.FromChat.CreatedAt,
                        },
                        new RegisteredChatSm
                        {
                            Id = mailToSend.MessageCandidate.SlotInstance.ToChat.Id,
                            ChatId = mailToSend.MessageCandidate.SlotInstance.ToChat.ChatId,
                            ChatTitle = mailToSend.MessageCandidate.SlotInstance.ToChat.ChatTitle,
                            ChatAlias = mailToSend.MessageCandidate.SlotInstance.ToChat.ChatAlias,
                            CreatedAt = mailToSend.MessageCandidate.SlotInstance.ToChat.CreatedAt,
                        },
                        new MessageCandidateSm
                        {
                            MessageId = mailToSend.MessageCandidate.MessageId,
                            AuthorName = mailToSend.MessageCandidate.AuthorName,
                            Preview = mailToSend.MessageCandidate.Preview,
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
