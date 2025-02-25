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

namespace DiplomaticMailBot.Tests.Integration.Tests;

[TestFixture]
[Parallelizable(scope: ParallelScope.Fixtures)]
public class DiplomaticMailPollRepositoryTests : IntegrationTestBase
{
    private RespawnableContextManager<ApplicationDbContext>? _contextManager;
    private TimeProvider _timeProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        _contextManager = await TestDbUtils.CreateNewRandomDbContextManagerAsync();
        _timeProvider = TimeProvider;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await _contextManager.StopIfNotNullAsync();
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithSingleCandidate_SendsMessageAndCreatesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = TimeProvider.GetUtcNow().UtcDateTime;
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
            FromChat = sourceChat,
            ToChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidate = new DiplomaticMailCandidate
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            CreatedAt = utcNow,
            SlotInstance = slotInstance,
        };
        dbContext.DiplomaticMailCandidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticMailPollRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticMailPollRepository>(),
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
        var poll = await verifyContext.DiplomaticMailPolls
            .FirstOrDefaultAsync(x => x.SlotInstanceId == slotInstance.Id, cancellationToken);

        Assert.That(poll, Is.Not.Null);
        Assert.That(poll!.Status, Is.EqualTo(DiplomaticMailPollStatus.Opened));
        Assert.That(poll.MessageId, Is.EqualTo(candidate.MessageId));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithMultipleCandidates_SendsPollAndCreatesPoll(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = TimeProvider.GetUtcNow().UtcDateTime;
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
            FromChat = sourceChat,
            ToChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidates = new[]
        {
            new DiplomaticMailCandidate
            {
                MessageId = 789,
                Preview = "Test message 1",
                SubmitterId = 101,
                AuthorId = 102,
                AuthorName = "Test Author 1",
                CreatedAt = utcNow,
                SlotInstance = slotInstance,
            },
            new DiplomaticMailCandidate
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
        await dbContext.DiplomaticMailCandidates.AddRangeAsync(candidates);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticMailPollRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticMailPollRepository>(),
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
        var poll = await verifyContext.DiplomaticMailPolls
            .FirstOrDefaultAsync(x => x.SlotInstanceId == slotInstance.Id, cancellationToken);

        Assert.That(poll, Is.Not.Null);
        Assert.That(poll!.Status, Is.EqualTo(DiplomaticMailPollStatus.Opened));
        Assert.That(poll.MessageId, Is.EqualTo(pollMessageId));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task OpenPendingPollsAsync_WithNoCandidates_RemovesSlotInstance(CancellationToken cancellationToken)
    {
        // Arrange
        var utcNow = TimeProvider.GetUtcNow().UtcDateTime;
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
            FromChat = sourceChat,
            ToChat = targetChat,
        };
        dbContext.SlotInstances.Add(slotInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticMailPollRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticMailPollRepository>(),
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
}
