using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
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
public sealed class DiplomaticRelationRepositoryTests
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
    public async Task EstablishRelationsAsync_WhenValidInput_CreatesRelation(CancellationToken cancellationToken)
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
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.EstablishRelationsAsync(sourceChat.ChatId, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        var relationInfo = result.LeftToList().First();
        Assert.That(relationInfo.IsOutgoingRelationPresent, Is.True);
        Assert.That(relationInfo.IsIncomingRelationPresent, Is.False);
        Assert.That(relationInfo.SourceChatId, Is.EqualTo(sourceChat.ChatId));
        Assert.That(relationInfo.TargetChatId, Is.EqualTo(targetChat.ChatId));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var relation = await verifyContext.DiplomaticRelations
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(relation, Is.Not.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task EstablishRelationsAsync_WhenRelationExists_ReturnsError(CancellationToken cancellationToken)
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
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var existingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.DiplomaticRelations.Add(existingRelation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.EstablishRelationsAsync(sourceChat.ChatId, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.OutgoingRelationAlreadyExists.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task EstablishRelationsAsync_WhenSourceChatIsNull_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.EstablishRelationsAsync(123, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.SourceChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task EstablishRelationsAsync_WhenTargetChatIsNull_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.EstablishRelationsAsync(sourceChat.ChatId, "nonexistent", cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.TargetChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task EstablishRelationsAsync_WhenSourceAndTargetChatsAreSame_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.EstablishRelationsAsync(sourceChat.ChatId, sourceChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.CanNotEstablishRelationsWithSelf.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task BreakOffRelationsAsync_WhenValidInput_RemovesRelation(CancellationToken cancellationToken)
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
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);

        var existingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.DiplomaticRelations.Add(existingRelation);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.BreakOffRelationsAsync(sourceChat.ChatId, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        var relationInfo = result.LeftToList().First();
        Assert.That(relationInfo.IsOutgoingRelationPresent, Is.False);
        Assert.That(relationInfo.IsIncomingRelationPresent, Is.False);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var relation = await verifyContext.DiplomaticRelations
            .FirstOrDefaultAsync(x => x.SourceChatId == sourceChat.Id && x.TargetChatId == targetChat.Id, cancellationToken);

        Assert.That(relation, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task BreakOffRelationsAsync_WhenRelationDoesNotExist_ReturnsError(CancellationToken cancellationToken)
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
        await dbContext.RegisteredChats.AddRangeAsync(sourceChat, targetChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.BreakOffRelationsAsync(sourceChat.ChatId, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.OutgoingRelationDoesNotExist.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task BreakOffRelationsAsync_WhenSourceChatIsNull_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.BreakOffRelationsAsync(123, targetChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.SourceChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task BreakOffRelationsAsync_WhenTargetChatIsNull_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.BreakOffRelationsAsync(sourceChat.ChatId, "nonexistent", cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.TargetChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task BreakOffRelationsAsync_WhenSourceAndTargetChatsAreSame_ReturnsError(CancellationToken cancellationToken)
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

        var repository = new DiplomaticRelationRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticRelationRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.BreakOffRelationsAsync(sourceChat.ChatId, sourceChat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.CanNotBreakOffRelationsWithSelf.ToInt()));
    }
}
