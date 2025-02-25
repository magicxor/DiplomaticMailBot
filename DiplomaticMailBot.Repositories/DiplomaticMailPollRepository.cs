using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.ServiceModels.DiplomaticMailCandidate;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class DiplomaticMailPollRepository
{
    private readonly ILogger<DiplomaticMailPollRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;
    private readonly TimeProvider _timeProvider;

    public DiplomaticMailPollRepository(
        ILogger<DiplomaticMailPollRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
        _timeProvider = timeProvider;
    }

    private async Task OpenPendingPollAsync(
        ApplicationDbContext applicationDbContext,
        SlotInstance slotInstance,
        DateTime utcNow,
        DateOnly dateNow,
        SendMessageCallback sendMessageCallback,
        SendPollCallback sendPollCallback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Opening poll for slot instance {SlotInstanceId}", slotInstance.Id);

        var relations = await applicationDbContext.DiplomaticRelations
            .Where(x => (x.SourceChatId == slotInstance.FromChatId && x.TargetChatId == slotInstance.ToChatId)
                        || (x.SourceChatId == slotInstance.ToChatId && x.TargetChatId == slotInstance.FromChatId))
            .ToListAsync(cancellationToken);
        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.FromChatId && x.TargetChatId == slotInstance.ToChatId);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.ToChatId && x.TargetChatId == slotInstance.FromChatId);

        if (outgoingRelation is null || incomingRelation is null)
        {
            _logger.LogInformation("Relations for slot instance {SlotInstanceId} not found; removing it", slotInstance.Id);
            slotInstance.Status = SlotInstanceStatus.Archived;
            applicationDbContext.SlotInstances.Remove(slotInstance);
        }
        else
        {
            slotInstance.Status = SlotInstanceStatus.Voting;

            var candidates = await applicationDbContext.DiplomaticMailCandidates
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

                var voteStartsAtDateTime = new DateTime(dateNow, slotInstance.Template!.VoteStartAt, DateTimeKind.Utc);
                var voteEndsAtDateTime = new DateTime(dateNow, slotInstance.Template!.VoteEndAt, DateTimeKind.Utc);
                if (voteEndsAtDateTime <= voteStartsAtDateTime)
                {
                    voteEndsAtDateTime = voteEndsAtDateTime.AddDays(1);
                }

                var timeLeft = voteEndsAtDateTime - utcNow;

                var diplomaticMailCandidate = candidates.OrderBy(x => x.Id).First();

                await sendMessageCallback(
                    new RegisteredChatSm
                    {
                        Id = slotInstance.FromChat!.Id,
                        ChatId = slotInstance.FromChat!.ChatId,
                        ChatTitle = slotInstance.FromChat!.ChatTitle,
                        ChatAlias = slotInstance.FromChat!.ChatAlias,
                        CreatedAt = slotInstance.FromChat!.CreatedAt,
                    },
                    new RegisteredChatSm
                    {
                        Id = slotInstance.ToChat!.Id,
                        ChatId = slotInstance.ToChat!.ChatId,
                        ChatTitle = slotInstance.ToChat!.ChatTitle,
                        ChatAlias = slotInstance.ToChat!.ChatAlias,
                        CreatedAt = slotInstance.ToChat!.CreatedAt,
                    },
                    timeLeft,
                    new DiplomaticMailCandidateSm
                    {
                        MessageId = diplomaticMailCandidate.MessageId,
                        AuthorName = diplomaticMailCandidate.AuthorName,
                        Preview = diplomaticMailCandidate.Preview,
                    },
                    cancellationToken);

                applicationDbContext.DiplomaticMailPolls.Add(new DiplomaticMailPoll
                {
                    Status = DiplomaticMailPollStatus.Opened,
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
                    .Take(10)
                    .Select(x => new DiplomaticMailCandidateSm
                    {
                        MessageId = x.MessageId,
                        AuthorName = x.AuthorName,
                        Preview = x.Preview,
                    })
                    .ToList();

                var pollMessageId = await sendPollCallback(
                    new RegisteredChatSm
                    {
                        Id = slotInstance.FromChat!.Id,
                        ChatId = slotInstance.FromChat!.ChatId,
                        ChatTitle = slotInstance.FromChat!.ChatTitle,
                        ChatAlias = slotInstance.FromChat!.ChatAlias,
                        CreatedAt = slotInstance.FromChat!.CreatedAt,
                    },
                    new RegisteredChatSm
                    {
                        Id = slotInstance.ToChat!.Id,
                        ChatId = slotInstance.ToChat!.ChatId,
                        ChatTitle = slotInstance.ToChat!.ChatTitle,
                        ChatAlias = slotInstance.ToChat!.ChatAlias,
                        CreatedAt = slotInstance.ToChat!.CreatedAt,
                    },
                    pollOptions,
                    cancellationToken);

                _logger.LogInformation("Poll for slot instance {SlotInstanceId} will be opened with message ID {PollMessageId}", slotInstance.Id, pollMessageId);

                applicationDbContext.DiplomaticMailPolls.Add(new DiplomaticMailPoll
                {
                    Status = DiplomaticMailPollStatus.Opened,
                    MessageId = pollMessageId,
                    CreatedAt = utcNow,
                    SlotInstance = slotInstance,
                });
            }
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Poll for slot instance {SlotInstanceId} opened", slotInstance.Id);
    }

    public async Task OpenPendingPollsAsync(
        SendMessageCallback sendMessageCallback,
        SendPollCallback sendPollCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sendMessageCallback);
        ArgumentNullException.ThrowIfNull(sendPollCallback);

        _logger.LogDebug("Opening pending polls");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var slotInstancesToOpenPoll = await applicationDbContext.SlotInstances
            .Include(slot => slot.Template)
            .Include(slot => slot.FromChat)
            .Include(slot => slot.ToChat)
            .Where(slot =>
                slot.Status == SlotInstanceStatus.Collecting
                && slot.Date == dateNow
                && slot.Template!.VoteStartAt <= timeNow
                && slot.Template!.VoteEndAt >= timeNow
                && !applicationDbContext.DiplomaticMailPolls.Any(poll => poll.SlotInstanceId == slot.Id))
            .ToListAsync(cancellationToken);
        var i = 1;

        foreach (var slotInstance in slotInstancesToOpenPoll)
        {
            _logger.LogInformation("Opening poll for slot instance {SlotInstanceId} (processing {SlotInstanceNumber} of {SlotInstancesCount} slot instances)", slotInstance.Id, i, slotInstancesToOpenPoll.Count);

            try
            {
                await OpenPendingPollAsync(applicationDbContext, slotInstance, utcNow, dateNow, sendMessageCallback, sendPollCallback, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error opening poll for slot instance {SlotInstanceId}", slotInstance.Id);
            }

            i++;
        }

        _logger.LogDebug("{Amount} pending polls opened", i - 1);
    }

    private async Task CloseExpiredPollAsync(
        ApplicationDbContext applicationDbContext,
        DiplomaticMailPoll pollToClose,
        DateTime utcNow,
        StopPollCallback stopPollCallback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Closing poll {PollId}", pollToClose.Id);

        var slotInstance = pollToClose.SlotInstance!;
        var relations = await applicationDbContext.DiplomaticRelations
            .Where(x => (x.SourceChatId == slotInstance.FromChatId && x.TargetChatId == slotInstance.ToChatId)
                        || (x.SourceChatId == slotInstance.ToChatId && x.TargetChatId == slotInstance.FromChatId))
            .ToListAsync(cancellationToken);
        var outgoingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.FromChatId && x.TargetChatId == slotInstance.ToChatId);
        var incomingRelation = relations.OrderBy(x => x.Id).FirstOrDefault(x => x.SourceChatId == slotInstance.ToChatId && x.TargetChatId == slotInstance.FromChatId);

        pollToClose.Status = DiplomaticMailPollStatus.Closed;
        pollToClose.ClosedAt = utcNow;
        pollToClose.SlotInstance!.Status = SlotInstanceStatus.Archived;

        if (outgoingRelation is null || incomingRelation is null)
        {
            _logger.LogInformation("Relations for poll {PollId} not found; removing it", pollToClose.Id);
            applicationDbContext.DiplomaticMailPolls.Remove(pollToClose);
            applicationDbContext.SlotInstances.Remove(slotInstance);
        }
        else
        {
            var candidates = await applicationDbContext.DiplomaticMailCandidates
                .Where(c => c.SlotInstanceId == pollToClose.SlotInstanceId)
                .ToListAsync(cancellationToken: cancellationToken);

            if (candidates.Count > 1)
            {
                _logger.LogInformation("Poll {PollId} has {CandidatesCount} potential candidates; trying to stop it", pollToClose.Id, candidates.Count);

                var chosenMessageIdResult = await stopPollCallback(pollToClose.SlotInstance.FromChat!.ChatId, pollToClose.MessageId, cancellationToken);

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

                        var chosenCandidate = await applicationDbContext.DiplomaticMailCandidates
                            .Where(candidate => candidate.MessageId == chosenMessageId
                                        && candidate.SlotInstanceId == pollToClose.SlotInstanceId
                                        && candidate.SlotInstance!.FromChatId == pollToClose.SlotInstance.FromChatId
                                        && candidate.SlotInstance!.ToChatId == pollToClose.SlotInstance.ToChatId)
                            .OrderBy(x => x.Id)
                            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                        _logger.LogInformation("Adding diplomatic mail outbox record for candidate {CandidateId}", chosenCandidate?.Id);

                        applicationDbContext.DiplomaticMailOutbox.Add(new DiplomaticMailOutbox
                        {
                            Status = DiplomaticMailOutboxStatus.Pending,
                            StatusDetails = null,
                            Attempts = 0,
                            CreatedAt = utcNow,
                            SentAt = null,
                            SlotInstance = pollToClose.SlotInstance,
                            DiplomaticMailCandidate = chosenCandidate,
                        });
                        return true;
                    });
            }
            else if (candidates.Count == 1)
            {
                _logger.LogInformation("Poll {PollId} has only one candidate; choosing it", pollToClose.Id);

                var chosenCandidate = candidates.OrderBy(x => x.Id).First();

                _logger.LogInformation("Adding diplomatic mail outbox record for candidate {CandidateId}", chosenCandidate.Id);

                applicationDbContext.DiplomaticMailOutbox.Add(new DiplomaticMailOutbox
                {
                    Status = DiplomaticMailOutboxStatus.Pending,
                    StatusDetails = null,
                    Attempts = 0,
                    CreatedAt = utcNow,
                    SentAt = null,
                    SlotInstance = pollToClose.SlotInstance,
                    DiplomaticMailCandidate = chosenCandidate,
                });
            }

            await applicationDbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Poll {PollId} closed", pollToClose.Id);
    }

    public async Task CloseExpiredPollsAsync(
        StopPollCallback stopPollCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stopPollCallback);

        _logger.LogDebug("Closing expired polls");

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var pollsToClose = await applicationDbContext.DiplomaticMailPolls
            .Include(poll => poll.SlotInstance)
            .ThenInclude(slot => slot!.FromChat)
            .Where(x =>
                x.Status == DiplomaticMailPollStatus.Opened
                && ((x.SlotInstance!.Date == dateNow && x.SlotInstance!.Template!.VoteEndAt < timeNow)
                    || x.SlotInstance!.Date < dateNow))
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

        _logger.LogDebug("{Amount} expired polls closed", i - 1);
    }
}
