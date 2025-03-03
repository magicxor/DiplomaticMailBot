using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.MessageCandidate;
using DiplomaticMailBot.Tests.Common;
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
public sealed class MessageCandidateRepositoryTests
{
    private RespawnableContextManager<ApplicationDbContext>? _contextManager;

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

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenValidInput_ReturnsTrueAndSavesCandidate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync([outgoingRelation, incomingRelation], cancellationToken);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = slotTemplate.Id,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        Assert.That(result.LeftToList().First(), Is.True);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var savedCandidate = await verifyContext.MessageCandidates
            .FirstOrDefaultAsync(x => x.MessageId == input.MessageId, cancellationToken);

        Assert.That(savedCandidate, Is.Not.Null);
        Assert.That(savedCandidate!.Preview, Is.EqualTo(input.Preview.TryLeft(128)));
        Assert.That(savedCandidate.AuthorId, Is.EqualTo(input.AuthorId));
        Assert.That(savedCandidate.AuthorName, Is.EqualTo(input.AuthorName));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task WithdrawAsync_WhenValidInput_DeletesCandidateAndReturnsMessageId(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var slotInstanceTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotInstanceTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            SourceChat = sourceChat,
            TargetChat = targetChat,
            Template = slotInstanceTemplate,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidate = new MessageCandidate
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.MessageCandidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var deletedCandidatesCountResult = await repository.WithdrawAsync(sourceChat.ChatId, candidate.MessageId, candidate.SubmitterId, cancellationToken);

        // Assert
        Assert.That(deletedCandidatesCountResult.Match(err => err.Code, deletedCount => deletedCount), Is.EqualTo(1));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var deletedCandidate = await verifyContext.MessageCandidates
            .FirstOrDefaultAsync(x => x.MessageId == candidate.MessageId, cancellationToken);

        Assert.That(deletedCandidate, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task WithdrawAsync_WhenMessageNotFound_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddAsync(sourceChat, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.WithdrawAsync(sourceChat.ChatId, 789, 101, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.MessageCandidateNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenSourceChatIsNull_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddAsync(targetChat, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = 123, // non-existent chat
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = 1,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.SourceChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenTargetChatIsNull_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddAsync(sourceChat, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = "nonexistent", // non-existent chat
            SlotTemplateId = 1,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.TargetChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenSourceAndTargetChatsAreSame_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddAsync(sourceChat, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = sourceChat.ChatAlias, // same as source chat
            SlotTemplateId = 1,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.CanNotSendMessageToSelf.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenOutgoingRelationIsNull_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddAsync(incomingRelation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = 1,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.OutgoingRelationDoesNotExist.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenIncomingRelationIsNull_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddAsync(outgoingRelation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = 1,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.IncomingRelationDoesNotExist.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenSlotInstanceExists_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync([outgoingRelation, incomingRelation], cancellationToken);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            SourceChat = sourceChat,
            TargetChat = targetChat,
            Template = slotTemplate,
        };
        dbContext.SlotInstances.Add(slotInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = slotTemplate.Id,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        Assert.That(result.LeftToList().First(), Is.True);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenMessageAlreadyAdded_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync([outgoingRelation, incomingRelation], cancellationToken);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            SourceChat = sourceChat,
            TargetChat = targetChat,
            Template = slotTemplate,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var existingCandidate = new MessageCandidate
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.MessageCandidates.Add(existingCandidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = slotTemplate.Id,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.MessageCandidateAlreadyExists.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task PutAsync_WhenMaxPollOptionsReached_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        var timeProvider = FakeTimeProviderFactory.Create();

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.RegisteredChats.AddRangeAsync([sourceChat, targetChat], cancellationToken);

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync([outgoingRelation, incomingRelation], cancellationToken);

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(slotTemplate);

        var slotInstance = new SlotInstance
        {
            Status = SlotInstanceStatus.Collecting,
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
            SourceChat = sourceChat,
            TargetChat = targetChat,
            Template = slotTemplate,
        };
        dbContext.SlotInstances.Add(slotInstance);

        // Add max number of candidates
        for (var i = 1; i <= Defaults.MaxPollOptionCount; i++)
        {
            var candidate = new MessageCandidate
            {
                MessageId = i,
                Preview = $"Test message {i}",
                SubmitterId = 101,
                AuthorId = 102,
                AuthorName = "Test Author",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SlotInstance = slotInstance,
            };
            dbContext.MessageCandidates.Add(candidate);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            timeProvider);

        var input = new MessageCandidatePutSm
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            SourceChatId = sourceChat.ChatId,
            TargetChatAlias = targetChat.ChatAlias,
            SlotTemplateId = slotTemplate.Id,
            NextVoteSlotDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
        };

        // Act
        var result = await repository.PutAsync(input, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        Assert.That(result.RightToList().First().Code, Is.EqualTo(EventCode.MessageCandidateLimitReached.ToInt()));
    }
}
