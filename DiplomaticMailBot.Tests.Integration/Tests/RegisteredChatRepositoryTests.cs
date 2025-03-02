using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
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
public sealed class RegisteredChatRepositoryTests
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
    public async Task ListRegisteredChatsAsync_ReturnsAllChats(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var chats = new[]
        {
            new RegisteredChat
            {
                ChatId = 123,
                ChatTitle = "Chat 1",
                ChatAlias = "chat1",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            },
            new RegisteredChat
            {
                ChatId = 456,
                ChatTitle = "Chat 2",
                ChatAlias = "chat2",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            },
        };
        await dbContext.RegisteredChats.AddRangeAsync(chats);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.ListRegisteredChatsAsync(cancellationToken);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(x => x.ChatId), Is.EquivalentTo(chats.Select(x => x.ChatId)));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CreateOrUpdateAsync_WhenNewChat_CreatesChat(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        await using var dbContext = dbContextFactory.CreateDbContext();
        await dbContext.SlotTemplates.AddAsync(new SlotTemplate
        {
            VoteStartAt = new TimeOnly(14, 00),
            VoteEndAt = new TimeOnly(16, 00),
            Number = 1,
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        var request = new RegisteredChatCreateOrUpdateRequestSm
        {
            ChatId = 123,
            ChatTitle = "Test Chat",
            ChatAlias = "test",
        };

        // Act
        var result = await repository.CreateOrUpdateAsync(request, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        var createResult = result.LeftToList().First();
        Assert.That(createResult.IsCreated, Is.True);
        Assert.That(createResult.IsUpdated, Is.False);
        Assert.That(createResult.ChatId, Is.EqualTo(request.ChatId));
        Assert.That(createResult.ChatAlias, Is.EqualTo(request.ChatAlias));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var chat = await verifyContext.RegisteredChats
            .FirstOrDefaultAsync(x => x.ChatId == request.ChatId, cancellationToken);

        Assert.That(chat, Is.Not.Null);
        Assert.That(chat!.ChatTitle, Is.EqualTo(request.ChatTitle));
        Assert.That(chat.ChatAlias, Is.EqualTo(request.ChatAlias));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CreateOrUpdateAsync_WhenExistingChat_UpdatesChat(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var existingChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Old Title",
            ChatAlias = "old",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(-31), // Past the update cooldown
        };
        dbContext.RegisteredChats.Add(existingChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        var request = new RegisteredChatCreateOrUpdateRequestSm
        {
            ChatId = existingChat.ChatId,
            ChatTitle = "New Title",
            ChatAlias = "new",
        };

        // Act
        var result = await repository.CreateOrUpdateAsync(request, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        var updateResult = result.LeftToList().First();
        Assert.That(updateResult.IsCreated, Is.False);
        Assert.That(updateResult.IsUpdated, Is.True);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var chat = await verifyContext.RegisteredChats
            .FirstOrDefaultAsync(x => x.ChatId == request.ChatId, cancellationToken);

        Assert.That(chat, Is.Not.Null);
        Assert.That(chat!.ChatTitle, Is.EqualTo(request.ChatTitle));
        Assert.That(chat.ChatAlias, Is.EqualTo(request.ChatAlias));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task CreateOrUpdateAsync_WhenAliasTaken_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var existingChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Existing Chat",
            ChatAlias = "taken",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.RegisteredChats.Add(existingChat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        var request = new RegisteredChatCreateOrUpdateRequestSm
        {
            ChatId = 456,
            ChatTitle = "New Chat",
            ChatAlias = "taken", // Same alias as existing chat
        };

        // Act
        var result = await repository.CreateOrUpdateAsync(request, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.AliasIsTaken.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task DeleteAsync_WhenValidInput_DeletesChat(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var chat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Test Chat",
            ChatAlias = "test",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.RegisteredChats.Add(chat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.DeleteAsync(chat.ChatId, chat.ChatAlias, cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.False);
        Assert.That(result.LeftToList().First(), Is.True);

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var deletedChat = await verifyContext.RegisteredChats
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ChatId == chat.ChatId, cancellationToken);

        Assert.That(deletedChat, Is.Not.Null);
        Assert.That(deletedChat!.IsDeleted, Is.True);
        Assert.That(deletedChat.ChatAlias, Is.Not.Empty);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task DeleteAsync_WhenChatNotFound_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.DeleteAsync(123, "test", cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.RegisteredChatNotFound.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task DeleteAsync_WhenAliasMismatch_ReturnsError(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var chat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Test Chat",
            ChatAlias = "test",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.RegisteredChats.Add(chat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.DeleteAsync(chat.ChatId, "wrong_alias", cancellationToken);

        // Assert
        Assert.That(result.IsRight, Is.True);
        var error = result.RightToList().First();
        Assert.That(error.Code, Is.EqualTo(EventCode.RegisteredChatAliasMismatch.ToInt()));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task GetChatSlotTemplateByTelegramChatIdAsync_WhenChatExistsWithTemplate_ReturnsTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var template = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(14, 00),
            VoteEndAt = new TimeOnly(16, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        var chat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Test Chat",
            ChatAlias = "test",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotTemplateId = template.Id,
            SlotTemplate = template,
        };
        dbContext.RegisteredChats.Add(chat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.GetChatSlotTemplateByTelegramChatIdAsync(chat.ChatId, cancellationToken);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(template.Id));
        Assert.That(result.VoteStartAt, Is.EqualTo(template.VoteStartAt));
        Assert.That(result.VoteEndAt, Is.EqualTo(template.VoteEndAt));
        Assert.That(result.Number, Is.EqualTo(template.Number));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task GetChatSlotTemplateByTelegramChatIdAsync_WhenChatExistsWithoutTemplate_ReturnsNull(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var chat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Test Chat",
            ChatAlias = "test",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        dbContext.RegisteredChats.Add(chat);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.GetChatSlotTemplateByTelegramChatIdAsync(chat.ChatId, cancellationToken);

        // Assert
        Assert.That(result, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task GetChatSlotTemplateByTelegramChatIdAsync_WhenChatDoesNotExist_ReturnsNull(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var repository = new RegisteredChatRepository(
            NullLoggerFactory.Instance.CreateLogger<RegisteredChatRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        var result = await repository.GetChatSlotTemplateByTelegramChatIdAsync(123, cancellationToken);

        // Assert
        Assert.That(result, Is.Null);
    }
}
