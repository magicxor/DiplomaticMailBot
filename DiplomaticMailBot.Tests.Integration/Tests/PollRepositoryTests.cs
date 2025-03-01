using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.Tests.Integration.Constants;
using DiplomaticMailBot.Tests.Integration.Extensions;
using DiplomaticMailBot.Tests.Integration.Services;
using DiplomaticMailBot.Tests.Integration.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace DiplomaticMailBot.Tests.Integration.Tests;

[TestFixture]
[Parallelizable(scope: ParallelScope.Fixtures)]
public class PollRepositoryTests
{
    private RespawnableContextManager<ApplicationDbContext>? _contextManager;
    private FakeTimeProvider _timeProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _contextManager = await TestDbUtils.CreateNewRandomDbContextManagerAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _contextManager.StopIfNotNullAsync();
    }

    [SetUp]
    public void SetUp()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(1999, 2, 25, 16, 40, 39, TimeSpan.Zero));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithSingleCandidate_SendsMessageAndCreatesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddMinutes(-5),
            VoteEndAt = timeNow.AddHours(2),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidate = new MessageCandidate
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            CreatedAt = utcNow,
            SlotInstance = slotInstance,
        };
        dbContext.MessageCandidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var messageCallbackCalled = false;
        var pollCallbackCalled = false;

        // Act
        await repository.OpenPendingPollsAsync(
            async (source, target, timeLeft, candidateSm, ct) =>
            {
                messageCallbackCalled = true;
                Assert.That(source.ChatId, Is.EqualTo(sourceChat.ChatId));
                Assert.That(target.ChatId, Is.EqualTo(targetChat.ChatId));
                Assert.That(candidateSm.MessageId, Is.EqualTo(candidate.MessageId));
                await Task.CompletedTask;
            },
            async (source, target, candidates, ct) =>
            {
                pollCallbackCalled = true;
                await Task.CompletedTask;
                return 0;
            },
            cancellationToken);

        // Assert
        Assert.That(messageCallbackCalled, Is.True);
        Assert.That(pollCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var poll = await verifyContext.SlotPolls
            .FirstOrDefaultAsync(x => x.SlotInstanceId == slotInstance.Id, cancellationToken);

        Assert.That(poll, Is.Not.Null);
        Assert.That(poll!.Status, Is.EqualTo(PollStatus.Opened));
        Assert.That(poll.MessageId, Is.EqualTo(candidate.MessageId));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithMultipleCandidates_SendsPollAndCreatesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddMinutes(-5),
            VoteEndAt = timeNow.AddHours(2),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidates = new[]
        {
            new MessageCandidate
            {
                MessageId = 789,
                Preview = "Test message 1",
                SubmitterId = 101,
                AuthorId = 102,
                AuthorName = "Test Author 1",
                CreatedAt = utcNow,
                SlotInstance = slotInstance,
            },
            new MessageCandidate
            {
                MessageId = 790,
                Preview = "Test message 2",
                SubmitterId = 103,
                AuthorId = 104,
                AuthorName = "Test Author 2",
                CreatedAt = utcNow.AddMinutes(1),
                SlotInstance = slotInstance,
            },
        };
        await dbContext.MessageCandidates.AddRangeAsync(candidates);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var messageCallbackCalled = false;
        var pollCallbackCalled = false;
        const int pollMessageId = 999;

        // Act
        await repository.OpenPendingPollsAsync(
            async (source, target, timeLeft, candidateSm, ct) =>
            {
                messageCallbackCalled = true;
                await Task.CompletedTask;
            },
            async (source, target, pollCandidates, ct) =>
            {
                pollCallbackCalled = true;
                Assert.That(source.ChatId, Is.EqualTo(sourceChat.ChatId));
                Assert.That(target.ChatId, Is.EqualTo(targetChat.ChatId));
                Assert.That(pollCandidates, Has.Count.EqualTo(2));
                await Task.CompletedTask;
                return pollMessageId;
            },
            cancellationToken);

        // Assert
        Assert.That(messageCallbackCalled, Is.False);
        Assert.That(pollCallbackCalled, Is.True);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var poll = await verifyContext.SlotPolls
            .FirstOrDefaultAsync(x => x.SlotInstanceId == slotInstance.Id, cancellationToken);

        Assert.That(poll, Is.Not.Null);
        Assert.That(poll!.Status, Is.EqualTo(PollStatus.Opened));
        Assert.That(poll.MessageId, Is.EqualTo(pollMessageId));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithNoCandidates_RemovesSlotInstance(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddMinutes(-5),
            VoteEndAt = timeNow.AddHours(2),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        // Act
        await repository.OpenPendingPollsAsync(
            (_, _, _, _, _) => Task.CompletedTask,
            (_, _, _, _) => Task.FromResult(0),
            cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var verifySlotInstance = await verifyContext.SlotInstances
            .FirstOrDefaultAsync(x => x.Id == slotInstance.Id, cancellationToken);

        Assert.That(verifySlotInstance, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CloseExpiredPollsAsync_WhenPollExpiredToday_ClosesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(-3),
            VoteEndAt = timeNow.AddHours(-1), // Expired 1 hour ago
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Voting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidates = new List<MessageCandidate>
        {
            new()
            {
                MessageId = 100,
                Preview = "candidate 1",
                SubmitterId = 5,
                AuthorId = 6,
                AuthorName = "author 1",
                CreatedAt = utcNow.AddHours(-100),
                SlotInstance = slotInstance,
            },
            new()
            {
                MessageId = 101,
                Preview = "candidate 2",
                SubmitterId = 7,
                AuthorId = 8,
                AuthorName = "author 2",
                CreatedAt = utcNow.AddHours(-101),
                SlotInstance = slotInstance,
            },
        };
        dbContext.MessageCandidates.AddRange(candidates);

        var poll = new SlotPoll
        {
            Status = PollStatus.Opened,
            MessageId = 789,
            SlotInstance = slotInstance,
            CreatedAt = utcNow.AddHours(-2),
        };
        dbContext.SlotPolls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var stopPollCallbackCalled = false;
        var stopPollCallbackChatId = 0L;
        var stopPollCallbackMessageId = 0;

        // Act
        await repository.CloseExpiredPollsAsync(
            async (chatId, messageId, ct) =>
            {
                stopPollCallbackCalled = true;
                stopPollCallbackChatId = chatId;
                stopPollCallbackMessageId = messageId;
                return 100;
            },
            cancellationToken);

        // Assert
        Assert.That(stopPollCallbackCalled, Is.True);
        Assert.That(stopPollCallbackChatId, Is.EqualTo(sourceChat.ChatId));
        Assert.That(stopPollCallbackMessageId, Is.EqualTo(poll.MessageId));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var verifyPoll = await verifyContext.SlotPolls
            .Include(x => x.SlotInstance)
            .FirstOrDefaultAsync(x => x.Id == poll.Id, cancellationToken);

        Assert.That(verifyPoll, Is.Not.Null);
        Assert.That(verifyPoll!.Status, Is.EqualTo(PollStatus.Closed));
        Assert.That(verifyPoll.ClosedAt, Is.Not.Null);
        Assert.That(verifyPoll.SlotInstance.Status, Is.EqualTo(SlotInstanceStatus.Archived));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CloseExpiredPollsAsync_WhenPollFromPastDate_ClosesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(-3),
            VoteEndAt = timeNow.AddHours(1), // Not expired by time, but by date
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow.AddDays(-1), // Yesterday
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var poll = new SlotPoll
        {
            Status = PollStatus.Opened,
            MessageId = 789,
            SlotInstance = slotInstance,
            CreatedAt = utcNow.AddDays(-1),
        };
        dbContext.SlotPolls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        // Act
        await repository.CloseExpiredPollsAsync(
            async (chatId, messageId, ct) => 0,
            cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var verifyPoll = await verifyContext.SlotPolls
            .Include(x => x.SlotInstance)
            .FirstOrDefaultAsync(x => x.Id == poll.Id, cancellationToken);

        Assert.That(verifyPoll, Is.Not.Null);
        Assert.That(verifyPoll!.Status, Is.EqualTo(PollStatus.Closed));
        Assert.That(verifyPoll.ClosedAt, Is.Not.Null);
        Assert.That(verifyPoll.SlotInstance.Status, Is.EqualTo(SlotInstanceStatus.Archived));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CloseExpiredPollsAsync_WhenPollNotExpired_DoesNotClosePoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(-1),
            VoteEndAt = timeNow.AddHours(1), // Not expired
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow, // Today
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var poll = new SlotPoll
        {
            Status = PollStatus.Opened,
            MessageId = 789,
            SlotInstance = slotInstance,
            CreatedAt = utcNow.AddHours(-1),
        };
        dbContext.SlotPolls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var stopPollCallbackCalled = false;

        // Act
        await repository.CloseExpiredPollsAsync(
            async (chatId, messageId, ct) =>
            {
                stopPollCallbackCalled = true;
                return 0;
            },
            cancellationToken);

        // Assert
        Assert.That(stopPollCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var verifyPoll = await verifyContext.SlotPolls
            .Include(x => x.SlotInstance)
            .FirstOrDefaultAsync(x => x.Id == poll.Id, cancellationToken);

        Assert.That(verifyPoll, Is.Not.Null);
        Assert.That(verifyPoll!.Status, Is.EqualTo(PollStatus.Opened));
        Assert.That(verifyPoll.ClosedAt, Is.Null);
        Assert.That(verifyPoll.SlotInstance.Status, Is.EqualTo(SlotInstanceStatus.Collecting));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CloseExpiredPollsAsync_WhenRelationsRemoved_RemovesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(-3),
            VoteEndAt = timeNow.AddHours(-1),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var poll = new SlotPoll
        {
            Status = PollStatus.Opened,
            MessageId = 789,
            SlotInstance = slotInstance,
            CreatedAt = utcNow.AddHours(-2),
        };
        dbContext.SlotPolls.Add(poll);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var stopPollCallbackCalled = false;

        // Act
        await repository.CloseExpiredPollsAsync(
            async (chatId, messageId, ct) =>
            {
                stopPollCallbackCalled = true;
                return 0;
            },
            cancellationToken);

        // Assert
        Assert.That(stopPollCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var verifyPoll = await verifyContext.SlotPolls
            .FirstOrDefaultAsync(x => x.Id == poll.Id, cancellationToken);

        Assert.That(verifyPoll, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenVoteStartsInNextFourHours_SendsReminder(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTime(2025, 02, 27, 12, 03, 00, DateTimeKind.Utc));
        var voteStartTime = new TimeOnly(16, 00);
        var voteEndTime = new TimeOnly(18, 00);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = voteStartTime,
            VoteEndAt = voteEndTime,
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);
        sourceChat.SlotTemplate = slotTemplate;
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;
        RegisteredChat? reminderSourceChat = null;
        RegisteredChat? reminderTargetChat = null;
        var reminderTimeLeft = TimeSpan.Zero;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                reminderSourceChat = await dbContext.RegisteredChats.FirstOrDefaultAsync(x => x.Id == source.Id, ct);
                reminderTargetChat = await dbContext.RegisteredChats.FirstOrDefaultAsync(x => x.Id == target.Id, ct);
                reminderTimeLeft = timeLeft;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.True);
        Assert.That(reminderSourceChat, Is.Not.Null);
        Assert.That(reminderTargetChat, Is.Not.Null);
        Assert.That(reminderSourceChat!.ChatId, Is.EqualTo(sourceChat.ChatId));
        Assert.That(reminderTargetChat!.ChatId, Is.EqualTo(targetChat.ChatId));
        Assert.That(reminderTimeLeft, Is.EqualTo(voteStartTime - timeNow));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var slotInstance = await verifyContext.SlotInstances
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(slotInstance, Is.Not.Null);
        Assert.That(slotInstance!.Status, Is.EqualTo(SlotInstanceStatus.Collecting));
        Assert.That(slotInstance.Date, Is.EqualTo(dateNow));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenVoteStartsTomorrowInNextFourHours_SendsReminder(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTime(2025, 02, 25, 23, 03, 00, DateTimeKind.Utc));
        var voteStartTime = new TimeOnly(01, 00);
        var voteEndTime = new TimeOnly(05, 00);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);
        var tomorrow = dateNow.AddDays(1);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = voteStartTime,
            VoteEndAt = voteEndTime,
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);
        sourceChat.SlotTemplate = slotTemplate;
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;
        RegisteredChat? reminderSourceChat = null;
        RegisteredChat? reminderTargetChat = null;
        var reminderTimeLeft = TimeSpan.Zero;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                reminderSourceChat = await dbContext.RegisteredChats.FirstOrDefaultAsync(x => x.Id == source.Id, ct);
                reminderTargetChat = await dbContext.RegisteredChats.FirstOrDefaultAsync(x => x.Id == target.Id, ct);
                reminderTimeLeft = timeLeft;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.True);
        Assert.That(reminderSourceChat, Is.Not.Null);
        Assert.That(reminderTargetChat, Is.Not.Null);
        Assert.That(reminderSourceChat!.ChatId, Is.EqualTo(sourceChat.ChatId));
        Assert.That(reminderTargetChat!.ChatId, Is.EqualTo(targetChat.ChatId));
        Assert.That(reminderTimeLeft, Is.EqualTo(voteStartTime - timeNow));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var slotInstance = await verifyContext.SlotInstances
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(slotInstance, Is.Not.Null);
        Assert.That(slotInstance!.Status, Is.EqualTo(SlotInstanceStatus.Collecting));
        Assert.That(slotInstance.Date, Is.EqualTo(tomorrow));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenVoteStartsAfterFourHours_DoesNotSendReminder(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(5), // Vote starts in 5 hours (outside 4-hour window)
            VoteEndAt = timeNow.AddHours(7),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);
        sourceChat.SlotTemplate = slotTemplate;
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var slotInstance = await verifyContext.SlotInstances
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(slotInstance, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenSlotInstanceAlreadyExists_DoesNotSendReminder(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var timeNow = TimeOnly.FromDateTime(utcNow);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = timeNow.AddHours(2), // Vote starts in 2 hours (within 4-hour window)
            VoteEndAt = timeNow.AddHours(4),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);
        sourceChat.SlotTemplate = slotTemplate;

        // Add existing slot instance
        var existingSlotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = dateNow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(existingSlotInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var slotInstances = await verifyContext.SlotInstances
            .Where(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id)
            .ToListAsync(cancellationToken);

        Assert.That(slotInstances, Has.Count.EqualTo(1));
        Assert.That(slotInstances[0].Id, Is.EqualTo(existingSlotInstance.Id));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenNoSlotTemplate_DoesNotSendReminder(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
            SlotTemplate = null, // No slot template
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var slotInstance = await verifyContext.SlotInstances
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(slotInstance, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenVoteStartsTomorrowAfterFourHours_DoesNotSendReminder(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTime(2025, 02, 25, 12, 00, 00, DateTimeKind.Utc));
        var voteStartTime = new TimeOnly(17, 00);
        var voteEndTime = new TimeOnly(19, 00);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = voteStartTime,
            VoteEndAt = voteEndTime,
            Number = 1,
        };
        sourceChat.SlotTemplate = slotTemplate;
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.False);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WhenTomorrowSlotInstanceExists_DoesNotSendReminder(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTime(2025, 02, 25, 21, 00, 00, DateTimeKind.Utc));
        var voteStartTime = new TimeOnly(01, 00);
        var voteEndTime = new TimeOnly(03, 00);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(utcNow);
        var tomorrow = dateNow.AddDays(1);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = utcNow,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = utcNow,
        };
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = utcNow,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = utcNow,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = voteStartTime,
            VoteEndAt = voteEndTime,
            Number = 1,
        };
        sourceChat.SlotTemplate = slotTemplate;

        // Create a slot instance for tomorrow
        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = tomorrow,
            Template = slotTemplate,
            SourceChat = sourceChat,
            TargetChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminderCallbackCalled = false;

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminderCallbackCalled = true;
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(reminderCallbackCalled, Is.False);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendVoteApproachingRemindersAsync_WithMultipleChatsAndRelations_SendsRemindersOnlyForPendingSlots(CancellationToken cancellationToken)
    {
        // Arrange
        _timeProvider.SetUtcNow(new DateTime(2025, 02, 25, 22, 00, 00, DateTimeKind.Utc));
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var dateNow = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var tomorrow = dateNow.AddDays(1);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();

        // Create 4 chats with different vote schedules
        var chat1 = new RegisteredChat
        {
            ChatId = 111,
            ChatTitle = "Chat 1",
            ChatAlias = "chat1",
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        var slotTemplate1 = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(01, 00), // Tomorrow 01:00 UTC - should trigger reminder (in 3 hours, within maxTimeBeforeNotice)
            VoteEndAt = new TimeOnly(03, 00),
            Number = 1,
        };
        chat1.SlotTemplate = slotTemplate1;

        var chat2 = new RegisteredChat
        {
            ChatId = 222,
            ChatTitle = "Chat 2",
            ChatAlias = "chat2",
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        var slotTemplate2 = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(08, 00), // Tomorrow 08:00 UTC - too far (10 hours), no reminder
            VoteEndAt = new TimeOnly(10, 00),
            Number = 1,
        };
        chat2.SlotTemplate = slotTemplate2;

        var chat3 = new RegisteredChat
        {
            ChatId = 333,
            ChatTitle = "Chat 3",
            ChatAlias = "chat3",
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        var slotTemplate3 = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(02, 00), // Tomorrow 02:00 UTC - exactly 4 hours, no reminder due to non-inclusive comparison
            VoteEndAt = new TimeOnly(04, 00),
            Number = 1,
        };
        chat3.SlotTemplate = slotTemplate3;

        var chat4 = new RegisteredChat
        {
            ChatId = 444,
            ChatTitle = "Chat 4",
            ChatAlias = "chat4",
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };
        var slotTemplate4 = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(01, 30), // Tomorrow 01:30 UTC - should trigger reminder (in 3.5 hours, within maxTimeBeforeNotice)
            VoteEndAt = new TimeOnly(03, 30),
            Number = 1,
        };
        chat4.SlotTemplate = slotTemplate4;

        await dbContext.RegisteredChats.AddRangeAsync(chat1, chat2, chat3, chat4);
        await dbContext.SlotTemplates.AddRangeAsync(slotTemplate1, slotTemplate2, slotTemplate3, slotTemplate4);

        // Create mutual diplomatic relations between chats:
        // chat1 <-> chat2
        // chat1 <-> chat3
        // chat2 <-> chat3
        // chat3 <-> chat4
        var relations = new[]
        {
            new DiplomaticRelation { SourceChat = chat1, TargetChat = chat2, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat2, TargetChat = chat1, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat1, TargetChat = chat3, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat3, TargetChat = chat1, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat2, TargetChat = chat3, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat3, TargetChat = chat2, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat3, TargetChat = chat4, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
            new DiplomaticRelation { SourceChat = chat4, TargetChat = chat3, CreatedAt = _timeProvider.GetUtcNow().UtcDateTime },
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(relations);

        // Create some existing slot instances to verify they don't trigger reminders
        var existingSlots = new[]
        {
            new SlotInstance
            {
                Status = SlotInstanceStatus.Collecting,
                Date = tomorrow,
                Template = slotTemplate1,
                SourceChat = chat1,
                TargetChat = chat2,
            },
            new SlotInstance
            {
                Status = SlotInstanceStatus.Collecting,
                Date = tomorrow,
                Template = slotTemplate2,
                SourceChat = chat2,
                TargetChat = chat3,
            },
        };
        await dbContext.SlotInstances.AddRangeAsync(existingSlots);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new PollRepository(
            NullLoggerFactory.Instance.CreateLogger<PollRepository>(),
            dbContextFactory,
            _timeProvider);

        var reminders = new List<RelationResult>();

        // Act
        await repository.SendVoteApproachingRemindersAsync(
            async (source, target, timeLeft, ct) =>
            {
                reminders.Add(new RelationResult(source.ChatId, target.ChatId, timeLeft));
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        // Expected reminders:
        // 1. chat1 -> chat3 (vote at 01:00, in 3 hours)
        // 2. chat4 -> chat3 (vote at 01:30, in 3.5 hours)
        // No reminders for:
        // - chat2 (vote time too far)
        // - chat3 (vote time exactly at maxTimeBeforeNotice boundary)
        // - pairs with existing slots (chat1->chat2, chat2->chat3)
        Assert.That(reminders, Has.Count.EqualTo(2));

        // Verify chat1 -> chat3 reminder
        Assert.That(reminders, Has.Some.Matches<RelationResult>(
            r => r.SourceChatId == chat1.ChatId && r.TargetChatId == chat3.ChatId
            && MathUtils.AreEqual(r.TimeLeft.TotalHours, 3)));

        // Verify chat4 -> chat3 reminder
        Assert.That(reminders, Has.Some.Matches<RelationResult>(
            r => r.SourceChatId == chat4.ChatId && r.TargetChatId == chat3.ChatId
            && MathUtils.AreEqual(r.TimeLeft.TotalHours, 3.5)));

        // Verify no reminders for chat2 since its vote time is too far away
        Assert.That(reminders, Has.None.Matches<RelationResult>(
            r => r.SourceChatId == chat2.ChatId || r.TargetChatId == chat2.ChatId));

        // Verify no reminders for chat3 since its vote time is exactly at maxTimeBeforeNotice boundary
        Assert.That(reminders, Has.None.Matches<RelationResult>(
            r => r.SourceChatId == chat3.ChatId));

        // Verify no reminders for pairs that already have slot instances
        Assert.That(reminders, Has.None.Matches<RelationResult>(
            r => (r.SourceChatId == chat1.ChatId && r.TargetChatId == chat2.ChatId)
                 || (r.SourceChatId == chat2.ChatId && r.TargetChatId == chat3.ChatId)));
    }

    private sealed record RelationResult(long SourceChatId, long TargetChatId, TimeSpan TimeLeft);
}
