using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;
using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Infra.Repositories.Implementations;

public sealed class MessageOutboxRepository : IMessageOutboxRepository
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
        var today = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var mailsToSend = await applicationDbContext.MessageOutbox
            .TagWithCallSite()
            .Include(x => x.SlotInstance)
            .Include(mailOutbox => mailOutbox.MessageCandidate)
            .ThenInclude(candidate => candidate.SlotInstance)
            .ThenInclude(slot => slot.SourceChat)
            .Include(mailOutbox => mailOutbox.MessageCandidate)
            .ThenInclude(candidate => candidate.SlotInstance)
            .ThenInclude(slot => slot.TargetChat)
            .Where(x =>
                x.Status == MessageOutboxStatus.Pending
                && x.SentAt == null
                && x.Attempts < 3
                && ((x.SlotInstance.Date == today && x.SlotInstance.Template.VoteEndAt < timeNow)
                    || x.SlotInstance.Date < today))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var mailToSend in mailsToSend)
        {
            _logger.LogInformation("Sending message {MailNumber} of {TotalMails}: {SourceChat} -> {TargetChat}. Attempts: {Attempts}. Preview: {Preview}",
                i,
                mailsToSend.Count,
                mailToSend.MessageCandidate.SlotInstance.SourceChat.ChatAlias,
                mailToSend.MessageCandidate.SlotInstance.TargetChat.ChatAlias,
                mailToSend.Attempts,
                mailToSend.MessageCandidate.Preview.TryLeft(50));

            try
            {
                try
                {
                    await processOutboxRecordCallback(
                        new RegisteredChatSm
                        {
                            Id = mailToSend.MessageCandidate.SlotInstance.SourceChat.Id,
                            ChatId = mailToSend.MessageCandidate.SlotInstance.SourceChat.ChatId,
                            ChatTitle = mailToSend.MessageCandidate.SlotInstance.SourceChat.ChatTitle,
                            ChatAlias = mailToSend.MessageCandidate.SlotInstance.SourceChat.ChatAlias,
                            CreatedAt = mailToSend.MessageCandidate.SlotInstance.SourceChat.CreatedAt,
                        },
                        new RegisteredChatSm
                        {
                            Id = mailToSend.MessageCandidate.SlotInstance.TargetChat.Id,
                            ChatId = mailToSend.MessageCandidate.SlotInstance.TargetChat.ChatId,
                            ChatTitle = mailToSend.MessageCandidate.SlotInstance.TargetChat.ChatTitle,
                            ChatAlias = mailToSend.MessageCandidate.SlotInstance.TargetChat.ChatAlias,
                            CreatedAt = mailToSend.MessageCandidate.SlotInstance.TargetChat.CreatedAt,
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
