using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Common.Utils;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Data.EfFunctions;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.ServiceModels.MessageCandidate;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class PollRepository
{
    private readonly ILogger<PollRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public PollRepository(
        ILogger<PollRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    private sealed class MutualRelations
    {
        public required DiplomaticRelation Outgoing { get; set; }
        public required DiplomaticRelation Incoming { get; set; }
    }

    public async Task SendVoteApproachingRemindersAsync(
        SendVoteApproachingReminderCallback sendReminderCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendReminderCallback);

        _logger.LogDebug("Finding potential slots that haven't been utilized yet");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(utcNow);
        var tomorrow = today.AddDays(1);
        var maxTimeBeforeNotice = TimeSpan.FromHours(4);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        /* find those diplomatic relations that are mutual */
        var joined = applicationDbContext.DiplomaticRelations
            .TagWithCallSite()
            .AsExpandableEFCore()
            .Join(applicationDbContext.DiplomaticRelations,
                outgoing => new { SourceChatId = outgoing.SourceChatId, TargetChatId = outgoing.TargetChatId },
                incoming => new { SourceChatId = incoming.TargetChatId, TargetChatId = incoming.SourceChatId },
                (outgoing, incoming) => new MutualRelations { Outgoing = outgoing, Incoming = incoming });

        var relations = await joined
            .Select(relation => relation.Outgoing)
            .Union(joined.Select(relation => relation.Incoming))
            .Include(relation => relation.SourceChat)
            .ThenInclude(sourceChat => sourceChat.SlotTemplate)
            .Include(relation => relation.TargetChat)
            .Where(relation => relation.SourceChat.SlotTemplate != null
                               /* vote start time is near for this chat */
                               && ((DateTimeExpr.FromParts.Invoke(today, relation.SourceChat.SlotTemplate.VoteStartAt) > utcNow
                                    && DateTimeExpr.FromParts.Invoke(today, relation.SourceChat.SlotTemplate.VoteStartAt) - utcNow < maxTimeBeforeNotice)
                                   || (DateTimeExpr.FromParts.Invoke(tomorrow, relation.SourceChat.SlotTemplate.VoteStartAt) > utcNow
                                       && DateTimeExpr.FromParts.Invoke(tomorrow, relation.SourceChat.SlotTemplate.VoteStartAt) - utcNow < maxTimeBeforeNotice)))
            .Where(relation =>
                /* there are no upcoming SlotInstances for this source->target relation */
                !applicationDbContext.SlotInstances.Any(slot =>
                    slot.SourceChatId == relation.SourceChatId
                    && slot.TargetChatId == relation.TargetChatId
                    && ((slot.Date == today
                         && DateTimeExpr.FromParts.Invoke(today, slot.Template.VoteStartAt) > utcNow)
                        || (slot.Date == tomorrow
                            && DateTimeExpr.FromParts.Invoke(tomorrow, slot.Template.VoteStartAt) > utcNow))
                    )
                )
            .ToListAsync(cancellationToken: cancellationToken);

        _logger.LogDebug("Found {Amount} potential slots", relations.Count);
        var i = 1;

        foreach (var relation in relations)
        {
            _logger.LogInformation("Sending reminder for relation {SourceChatId} -> {TargetChatId} (processing {RelationNumber} of {RelationsCount} relations)", relation.SourceChatId, relation.TargetChatId, i, relations.Count);

            var slotTemplate = relation.SourceChat.SlotTemplate;
            if (slotTemplate == null)
            {
                continue;
            }

            try
            {
                /* creating a slot instance, so that the reminder is not sent again */
                var voteStartAt = slotTemplate.VoteStartAt;
                var nextSlotDate = SlotDateUtils.GetNearestVoteStartDate(utcNow, voteStartAt);
                var newSlotInstance = new SlotInstance
                {
                    Status = SlotInstanceStatus.Collecting,
                    Date = nextSlotDate,
                    Template = slotTemplate,
                    SourceChat = relation.SourceChat,
                    TargetChat = relation.TargetChat,
                };
                applicationDbContext.SlotInstances.Add(newSlotInstance);
                await applicationDbContext.SaveChangesAsync(cancellationToken);

                var timeLeft = new DateTime(nextSlotDate, voteStartAt, DateTimeKind.Utc) - utcNow;

                await sendReminderCallback(
                    new RegisteredChatSm
                    {
                        Id = relation.SourceChat.Id,
                        ChatId = relation.SourceChat.ChatId,
                        ChatTitle = relation.SourceChat.ChatTitle,
                        ChatAlias = relation.SourceChat.ChatAlias,
                        CreatedAt = relation.SourceChat.CreatedAt,
                    },
                    new RegisteredChatSm
                    {
                        Id = relation.TargetChat.Id,
                        ChatId = relation.TargetChat.ChatId,
                        ChatTitle = relation.TargetChat.ChatTitle,
                        ChatAlias = relation.TargetChat.ChatAlias,
                        CreatedAt = relation.TargetChat.CreatedAt,
                    },
                    timeLeft,
                    cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating new slot instance for relation {SourceChatId} -> {TargetChatId}", relation.SourceChatId, relation.TargetChatId);
            }

            i++;
        }
    }

    private async Task OpenPendingPollAsync(
        ApplicationDbContext applicationDbContext,
        SlotInstance slotInstance,
        DateTime utcNow,
        DateOnly today,
        SendChosenCandidateInfoMessageCallback sendMessageCallback,
        SendPollCallback sendPollCallback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opening poll for slot instance {SlotInstanceId}", slotInstance.Id);

        var relations = await applicationDbContext.DiplomaticRelations
            .TagWithCallSite()
            .Where(x => (x.SourceChatId == slotInstance.SourceChatId && x.TargetChatId == slotInstance.TargetChatId)
                        || (x.SourceChatId == slotInstance.TargetChatId && x.TargetChatId == slotInstance.SourceChatId))
            .ToListAsync(cancellationToken);
        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.SourceChatId && x.TargetChatId == slotInstance.TargetChatId);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.TargetChatId && x.TargetChatId == slotInstance.SourceChatId);

        if (outgoingRelation is null || incomingRelation is null)
        {
            _logger.LogInformation("Relations for slot instance {SlotInstanceId} not found; removing it", slotInstance.Id);
            slotInstance.Status = SlotInstanceStatus.Archived;
            applicationDbContext.SlotInstances.Remove(slotInstance);
        }
        else
        {
            slotInstance.Status = SlotInstanceStatus.Voting;

            var candidates = await applicationDbContext.MessageCandidates
                .TagWithCallSite()
                .Where(x => x.SlotInstanceId == slotInstance.Id)
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("No candidates for slot instance {SlotInstanceId}; removing it", slotInstance.Id);

                applicationDbContext.SlotInstances.Remove(slotInstance);
            }
            else if (candidates.Count == 1)
            {
                _logger.LogInformation("One candidate for slot instance {SlotInstanceId}; choosing it", slotInstance.Id);

                var voteStartsAtDateTime = new DateTime(today, slotInstance.Template.VoteStartAt, DateTimeKind.Utc);
                var voteEndsAtDateTime = new DateTime(today, slotInstance.Template.VoteEndAt, DateTimeKind.Utc);
                if (voteEndsAtDateTime <= voteStartsAtDateTime)
                {
                    voteEndsAtDateTime = voteEndsAtDateTime.AddDays(1);
                }

                var timeLeft = voteEndsAtDateTime - utcNow;

                var messageCandidate = candidates.OrderBy(x => x.Id).First();

                await sendMessageCallback(
                    new RegisteredChatSm
                    {
                        Id = slotInstance.SourceChat.Id,
                        ChatId = slotInstance.SourceChat.ChatId,
                        ChatTitle = slotInstance.SourceChat.ChatTitle,
                        ChatAlias = slotInstance.SourceChat.ChatAlias,
                        CreatedAt = slotInstance.SourceChat.CreatedAt,
                    },
                    new RegisteredChatSm
                    {
                        Id = slotInstance.TargetChat.Id,
                        ChatId = slotInstance.TargetChat.ChatId,
                        ChatTitle = slotInstance.TargetChat.ChatTitle,
                        ChatAlias = slotInstance.TargetChat.ChatAlias,
                        CreatedAt = slotInstance.TargetChat.CreatedAt,
                    },
                    timeLeft,
                    new MessageCandidateSm
                    {
                        MessageId = messageCandidate.MessageId,
                        AuthorName = messageCandidate.AuthorName,
                        Preview = messageCandidate.Preview,
                    },
                    cancellationToken);

                applicationDbContext.SlotPolls.Add(new SlotPoll
                {
                    Status = PollStatus.Opened,
                    MessageId = candidates.OrderBy(x => x.Id).First().MessageId,
                    CreatedAt = utcNow,
                    SlotInstance = slotInstance,
                });
            }
            else
            {
                _logger.LogInformation("Multiple candidates for slot instance {SlotInstanceId}; opening poll", slotInstance.Id);

                var pollOptions = candidates
                    .OrderBy(x => x.CreatedAt)
                    .Take(Defaults.MaxPollOptionCount)
                    .Select(x => new MessageCandidateSm
                    {
                        MessageId = x.MessageId,
                        AuthorName = x.AuthorName,
                        Preview = x.Preview.TryLeft(Defaults.PollOptionMaxChars),
                    })
                    .ToList();

                var pollMessageId = await sendPollCallback(
                    new RegisteredChatSm
                    {
                        Id = slotInstance.SourceChat.Id,
                        ChatId = slotInstance.SourceChat.ChatId,
                        ChatTitle = slotInstance.SourceChat.ChatTitle,
                        ChatAlias = slotInstance.SourceChat.ChatAlias,
                        CreatedAt = slotInstance.SourceChat.CreatedAt,
                    },
                    new RegisteredChatSm
                    {
                        Id = slotInstance.TargetChat.Id,
                        ChatId = slotInstance.TargetChat.ChatId,
                        ChatTitle = slotInstance.TargetChat.ChatTitle,
                        ChatAlias = slotInstance.TargetChat.ChatAlias,
                        CreatedAt = slotInstance.TargetChat.CreatedAt,
                    },
                    pollOptions,
                    cancellationToken);

                _logger.LogInformation("Poll for slot instance {SlotInstanceId} will be opened with message ID {PollMessageId}", slotInstance.Id, pollMessageId);

                applicationDbContext.SlotPolls.Add(new SlotPoll
                {
                    Status = PollStatus.Opened,
                    MessageId = pollMessageId,
                    CreatedAt = utcNow,
                    SlotInstance = slotInstance,
                });
            }
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Done processing poll for slot instance {SlotInstanceId}", slotInstance.Id);
    }

    public async Task OpenPendingPollsAsync(
        SendChosenCandidateInfoMessageCallback sendMessageCallback,
        SendPollCallback sendPollCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendMessageCallback);
        ArgumentNullException.ThrowIfNull(sendPollCallback);

        _logger.LogDebug("Opening pending polls");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var slotInstancesToOpenPoll = await applicationDbContext.SlotInstances
            .TagWithCallSite()
            .Include(slot => slot.Template)
            .Include(slot => slot.SourceChat)
            .Include(slot => slot.TargetChat)
            .Where(slot =>
                slot.Status == SlotInstanceStatus.Collecting
                && slot.Date == today
                && slot.Template.VoteStartAt <= timeNow
                && slot.Template.VoteEndAt >= timeNow
                && !applicationDbContext.SlotPolls.Any(poll => poll.SlotInstanceId == slot.Id))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var slotInstance in slotInstancesToOpenPoll)
        {
            _logger.LogInformation("Opening poll for slot instance {SlotInstanceId} (processing {SlotInstanceNumber} of {SlotInstancesCount} slot instances)", slotInstance.Id, i, slotInstancesToOpenPoll.Count);

            try
            {
                await OpenPendingPollAsync(applicationDbContext, slotInstance, utcNow, today, sendMessageCallback, sendPollCallback, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error opening poll for slot instance {SlotInstanceId}", slotInstance.Id);
            }

            i++;
        }

        _logger.LogDebug("{Amount} pending polls processed", i - 1);
    }

    private async Task CloseExpiredPollAsync(
        ApplicationDbContext applicationDbContext,
        SlotPoll pollToClose,
        DateTime utcNow,
        StopPollCallback stopPollCallback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Closing poll {PollId}", pollToClose.Id);

        var slotInstance = pollToClose.SlotInstance;
        var relations = await applicationDbContext.DiplomaticRelations
            .TagWithCallSite()
            .Where(x => (x.SourceChatId == slotInstance.SourceChatId && x.TargetChatId == slotInstance.TargetChatId)
                        || (x.SourceChatId == slotInstance.TargetChatId && x.TargetChatId == slotInstance.SourceChatId))
            .ToListAsync(cancellationToken);
        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.SourceChatId && x.TargetChatId == slotInstance.TargetChatId);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.TargetChatId && x.TargetChatId == slotInstance.SourceChatId);

        pollToClose.Status = PollStatus.Closed;
        pollToClose.ClosedAt = utcNow;
        pollToClose.SlotInstance.Status = SlotInstanceStatus.Archived;

        if (outgoingRelation is null || incomingRelation is null)
        {
            _logger.LogInformation("Relations for poll {PollId} not found; removing it", pollToClose.Id);
            applicationDbContext.SlotPolls.Remove(pollToClose);
            applicationDbContext.SlotInstances.Remove(slotInstance);
        }
        else
        {
            var candidates = await applicationDbContext.MessageCandidates
                .TagWithCallSite()
                .Where(c => c.SlotInstanceId == pollToClose.SlotInstanceId)
                .ToListAsync(cancellationToken: cancellationToken);

            if (candidates.Count > 1)
            {
                _logger.LogInformation("Poll {PollId} has {CandidatesCount} potential candidates; trying to stop it", pollToClose.Id, candidates.Count);

                var chosenMessageIdResult = await stopPollCallback(pollToClose.SlotInstance.SourceChat.ChatId, pollToClose.MessageId, cancellationToken);

                _logger.LogInformation("Poll {PollId} stopped", pollToClose.Id);

                await chosenMessageIdResult.MatchAsync(
                    err =>
                    {
                        _logger.LogError(new EventId(err.Code), "Error stopping poll: {Msg}", err.Message);
                        return Task.FromResult(true);
                    },
                    async chosenMessageId =>
                    {
                        _logger.LogInformation("Poll {PollId} stopped, chosen message ID: {ChosenMessageId}", pollToClose.Id, chosenMessageId);

                        var chosenCandidate = await applicationDbContext.MessageCandidates
                            .TagWithCallSite()
                            .Where(candidate => candidate.MessageId == chosenMessageId
                                        && candidate.SlotInstanceId == pollToClose.SlotInstanceId
                                        && candidate.SlotInstance.SourceChatId == pollToClose.SlotInstance.SourceChatId
                                        && candidate.SlotInstance.TargetChatId == pollToClose.SlotInstance.TargetChatId)
                            .OrderBy(x => x.Id)
                            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                        if (chosenCandidate is not null)
                        {
                            _logger.LogInformation("Adding message outbox record for candidate {CandidateId}", chosenCandidate.Id);

                            applicationDbContext.MessageOutbox.Add(new MessageOutbox
                            {
                                Status = MessageOutboxStatus.Pending,
                                StatusDetails = null,
                                Attempts = 0,
                                CreatedAt = utcNow,
                                SentAt = null,
                                SlotInstance = pollToClose.SlotInstance,
                                MessageCandidate = chosenCandidate,
                            });

                            return true;
                        }
                        else
                        {
                            _logger.LogError("Chosen candidate not found: {ChosenMessageId}", chosenMessageId);
                            return false;
                        }
                    });
            }
            else if (candidates.Count == 1)
            {
                _logger.LogInformation("Poll {PollId} has only one candidate; choosing it", pollToClose.Id);

                var chosenCandidate = candidates.OrderBy(x => x.Id).First();

                _logger.LogInformation("Adding message outbox record for candidate {CandidateId}", chosenCandidate.Id);

                applicationDbContext.MessageOutbox.Add(new MessageOutbox
                {
                    Status = MessageOutboxStatus.Pending,
                    StatusDetails = null,
                    Attempts = 0,
                    CreatedAt = utcNow,
                    SentAt = null,
                    SlotInstance = pollToClose.SlotInstance,
                    MessageCandidate = chosenCandidate,
                });
            }
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Poll {PollId} processed", pollToClose.Id);
    }

    public async Task CloseExpiredPollsAsync(
        StopPollCallback stopPollCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stopPollCallback);

        _logger.LogDebug("Closing expired polls");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var pollsToClose = await applicationDbContext.SlotPolls
            .TagWithCallSite()
            .Include(poll => poll.SlotInstance)
            .ThenInclude(slot => slot.SourceChat)
            .Where(x =>
                x.Status == PollStatus.Opened
                && ((x.SlotInstance.Date == today && x.SlotInstance.Template.VoteEndAt < timeNow)
                    || x.SlotInstance.Date < today))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var pollToClose in pollsToClose)
        {
            _logger.LogInformation("Closing poll {PollId} (processing {PollNumber} of {PollsCount} polls)", pollToClose.Id, i, pollsToClose.Count);

            try
            {
                await CloseExpiredPollAsync(applicationDbContext, pollToClose, utcNow, stopPollCallback, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error closing poll {PollId}", pollToClose.Id);
            }

            i++;
        }

        _logger.LogDebug("{Amount} expired polls processed", i - 1);
    }
}
