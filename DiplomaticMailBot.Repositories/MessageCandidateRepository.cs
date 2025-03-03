using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories.Extensions;
using DiplomaticMailBot.ServiceModels.MessageCandidate;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class MessageCandidateRepository
{
    private readonly ILogger<MessageCandidateRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public MessageCandidateRepository(
        ILogger<MessageCandidateRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<Either<bool, Error>> PutAsync(MessageCandidatePutSm sm, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sm);
        ArgumentOutOfRangeException.ThrowIfZero(sm.MessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sm.Preview);
        ArgumentOutOfRangeException.ThrowIfZero(sm.SubmitterId);
        ArgumentOutOfRangeException.ThrowIfZero(sm.AuthorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sm.AuthorName);
        ArgumentOutOfRangeException.ThrowIfZero(sm.SourceChatId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sm.TargetChatAlias);

        _logger.LogInformation("Putting the message to a vote: MessageId={MessageId}, Preview={Preview}, AuthorName={AuthorName}, SourceChatId={SourceChatId}, TargetChatAlias={TargetChatAlias}, SlotTemplateId={SlotTemplateId}, NextVoteSlotDate={NextVoteSlotDate}",
            sm.MessageId,
            sm.Preview,
            sm.AuthorName,
            sm.SourceChatId,
            sm.TargetChatAlias,
            sm.SlotTemplateId,
            sm.NextVoteSlotDate);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var chats = await applicationDbContext.RegisteredChats
            .Where(x => x.ChatId == sm.SourceChatId || x.ChatAlias == sm.TargetChatAlias)
            .ToListAsync(cancellationToken);

        var sourceChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatId == sm.SourceChatId);
        var targetChat = chats.OrderBy(x => x.Id).FirstOrDefault(x => x.ChatAlias == sm.TargetChatAlias);

        if (sourceChat is null)
        {
            _logger.LogInformation("Source chat not found: ChatId={ChatId}", sm.SourceChatId);
            return new DomainError(EventCode.SourceChatNotFound.ToInt(), "Source chat not found");
        }

        if (targetChat is null)
        {
            _logger.LogInformation("Target chat not found: ChatAlias={ChatAlias}", sm.TargetChatAlias);
            return new DomainError(EventCode.TargetChatNotFound.ToInt(), "Target chat not found");
        }

        if (sourceChat.IsSameAs(targetChat))
        {
            _logger.LogInformation("Can not send a message to self: SourceChatId={SourceChatId}, TargetChatId={TargetChatId}", sourceChat.Id, targetChat.Id);
            return new DomainError(EventCode.CanNotSendMessageToSelf.ToInt(), "Can not send a message to self");
        }

        var relations = await applicationDbContext.DiplomaticRelations
            .Where(x => (x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id) || (x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id))
            .ToListAsync(cancellationToken);

        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == targetChat.Id && x.TargetChatId == sourceChat.Id);

        if (outgoingRelation is null)
        {
            _logger.LogInformation("Outgoing relation does not exist: SourceChatId={SourceChatId}, TargetChatId={TargetChatId}", sourceChat.Id, targetChat.Id);
            return new DomainError(EventCode.OutgoingRelationDoesNotExist.ToInt(), "Outgoing relation does not exist");
        }

        if (incomingRelation is null)
        {
            _logger.LogInformation("Incoming relation does not exist: SourceChatId={SourceChatId}, TargetChatId={TargetChatId}", sourceChat.Id, targetChat.Id);
            return new DomainError(EventCode.IncomingRelationDoesNotExist.ToInt(), "Incoming relation does not exist");
        }

        var slotInstance = await applicationDbContext.SlotInstances
            .Include(x => x.Template)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(x => x.TemplateId == sm.SlotTemplateId
                && x.Status == SlotInstanceStatus.Collecting
                && x.Date == sm.NextVoteSlotDate
                && x.SourceChatId == sourceChat.Id
                && x.TargetChatId == targetChat.Id
                && !applicationDbContext.SlotPolls.Any(poll => poll.SlotInstanceId == x.Id)
                && !applicationDbContext.MessageOutbox.Any(obx => obx.SlotInstanceId == x.Id),
                cancellationToken);

        if (slotInstance is null)
        {
            _logger.LogInformation("Slot instance not found. Creating a new one: SlotTemplateId={SlotTemplateId}, NextVoteSlotDate={NextVoteSlotDate}, SourceChatId={SourceChatId}, TargetChatId={TargetChatId}",
                sm.SlotTemplateId,
                sm.NextVoteSlotDate,
                sourceChat.Id,
                targetChat.Id);

            slotInstance = new SlotInstance
            {
                Status = SlotInstanceStatus.Collecting,
                Date = sm.NextVoteSlotDate,
                TemplateId = sm.SlotTemplateId,
                SourceChat = sourceChat,
                TargetChat = targetChat,
            };
            applicationDbContext.SlotInstances.Add(slotInstance);
            await applicationDbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Slot instance found: SlotInstanceId={SlotInstanceId}, SlotInstanceDate={SlotInstanceDate}, SourceChatId={SourceChatId}, TargetChatId={TargetChatId}",
                slotInstance.Id,
                slotInstance.Date,
                sourceChat.Id,
                targetChat.Id);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var submitterId = sm.SubmitterId;
        var authorId = sm.AuthorId;
        var authorNameLong = sm.AuthorName.TryLeft(Defaults.DbAuthorNameMaxLength);
        var messagePreview = sm.Preview.TryLeft(Defaults.DbMessagePreviewMaxLength);

        if (await applicationDbContext.MessageCandidates
            .AnyAsync(candidate => candidate.MessageId == sm.MessageId
                && candidate.SlotInstanceId == slotInstance.Id,
                cancellationToken))
        {
            _logger.LogInformation("Mail candidate already exists: MessageId={MessageId}, SourceChatId={SourceChatId}, TargetChatId={TargetChatId}, SlotInstanceId={SlotInstanceId}",
                sm.MessageId,
                sourceChat.Id,
                targetChat.Id,
                slotInstance.Id);
            return new DomainError(EventCode.MessageCandidateAlreadyExists.ToInt(), "Mail candidate already exists");
        }

        if (await applicationDbContext.MessageCandidates
            .CountAsync(candidate => candidate.SlotInstanceId == slotInstance.Id, cancellationToken) >= Defaults.MaxPollOptionCount)
        {
            _logger.LogInformation("Mail candidate limit reached: SlotInstanceId={SlotInstanceId}", slotInstance.Id);
            return new DomainError(EventCode.MessageCandidateLimitReached.ToInt(), "Mail candidate limit reached");
        }

        applicationDbContext.MessageCandidates.Add(new MessageCandidate
        {
            MessageId = sm.MessageId,
            Preview = messagePreview,
            SubmitterId = submitterId,
            AuthorId = authorId,
            AuthorName = authorNameLong,
            CreatedAt = utcNow,
            SlotInstance = slotInstance,
        });
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Message put to a vote: MessageId={MessageId}, Preview={Preview}, AuthorName={AuthorName}, SourceChatId={SourceChatId}, TargetChatId={TargetChatId}, SlotInstanceId={SlotInstanceId}",
            sm.MessageId,
            messagePreview,
            authorNameLong,
            sourceChat.Id,
            targetChat.Id,
            slotInstance.Id);

        return true;
    }

    public async Task<Either<int, Error>> WithdrawAsync(long sourceChatId, int messageToWithdrawId, long commandSenderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Withdrawing the message from a vote: SourceChatId={SourceChatId}, MessageId={MessageId}, CommandSenderId={CommandSenderId}",
            sourceChatId,
            messageToWithdrawId,
            commandSenderId);

        ArgumentOutOfRangeException.ThrowIfZero(sourceChatId);
        ArgumentOutOfRangeException.ThrowIfZero(messageToWithdrawId);
        ArgumentOutOfRangeException.ThrowIfZero(commandSenderId);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var candidates = await applicationDbContext.MessageCandidates
            .Where(mailCandidate => mailCandidate.MessageId == messageToWithdrawId
                && (mailCandidate.AuthorId == commandSenderId || mailCandidate.SubmitterId == commandSenderId)
                && mailCandidate.SlotInstance.SourceChat.ChatId == sourceChatId
                && applicationDbContext
                    .SlotInstances
                    .Any(slot => slot.Id == mailCandidate.SlotInstanceId
                                          && slot.Status == SlotInstanceStatus.Collecting
                                          && !applicationDbContext.SlotPolls.Any(poll => poll.SlotInstanceId == slot.Id)
                                          && !applicationDbContext.MessageOutbox.Any(outbox => outbox.SlotInstanceId == slot.Id)))
            .ToListAsync(cancellationToken: cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogInformation("Mail candidate not found: SourceChatId={SourceChatId}, MessageId={MessageId}, CommandSenderId={CommandSenderId}",
                sourceChatId,
                messageToWithdrawId,
                commandSenderId);

            return new DomainError(EventCode.MessageCandidateNotFound.ToInt(), "Mail candidate not found");
        }
        else
        {
            _logger.LogInformation("Mail candidates found: Count={Count}, SourceChatId={SourceChatId}, MessageId={MessageId}, CommandSenderId={CommandSenderId}",
                candidates.Count,
                sourceChatId,
                messageToWithdrawId,
                commandSenderId);

            applicationDbContext.MessageCandidates.RemoveRange(candidates);
            await applicationDbContext.SaveChangesAsync(cancellationToken);

            return candidates.Count;
        }
    }
}
