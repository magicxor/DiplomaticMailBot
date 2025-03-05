using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Entities;
using DiplomaticMailBot.Infra.Repositories.Implementations;
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
public sealed class SeedRepositoryTests
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
    public async Task SeedDefaultSlotTemplateAsync_WhenNoTemplates_CreatesDefaultTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedDefaultSlotTemplateAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var templates = await verifyContext.SlotTemplates.ToListAsync(cancellationToken);

        Assert.That(templates, Has.Count.EqualTo(1));
        var template = templates[0];
        Assert.That(template.VoteStartAt, Is.EqualTo(new TimeOnly(14, 00)));
        Assert.That(template.VoteEndAt, Is.EqualTo(new TimeOnly(16, 00)));
        Assert.That(template.Number, Is.EqualTo(1));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SeedDefaultSlotTemplateAsync_WhenTemplatesExist_DoesNotCreateTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed existing template
        await using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.SlotTemplates.Add(new SlotTemplate
        {
            VoteStartAt = new TimeOnly(10, 00),
            VoteEndAt = new TimeOnly(12, 00),
            Number = 2,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedDefaultSlotTemplateAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var templates = await verifyContext.SlotTemplates.ToListAsync(cancellationToken);

        Assert.That(templates, Has.Count.EqualTo(1));
        var template = templates[0];
        Assert.That(template.VoteStartAt, Is.EqualTo(new TimeOnly(10, 00)));
        Assert.That(template.VoteEndAt, Is.EqualTo(new TimeOnly(12, 00)));
        Assert.That(template.Number, Is.EqualTo(2));
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task MigrateAsync_AppliesMigrations(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.MigrateAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var canConnect = await verifyContext.Database.CanConnectAsync(cancellationToken);
        Assert.That(canConnect, Is.True);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SeedChatSlotTemplatesAsync_WhenNoDefaultTemplate_DoesNotUpdateChats(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed chat without template
        await using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.RegisteredChats.Add(new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Chat 1",
            ChatAlias = "chat1",
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedChatSlotTemplatesAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var chat = await verifyContext.RegisteredChats.FirstAsync(cancellationToken);
        Assert.That(chat.SlotTemplateId, Is.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SeedChatSlotTemplatesAsync_WhenDefaultTemplateExists_UpdatesChatsWithoutTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed default template and chats
        await using var dbContext = dbContextFactory.CreateDbContext();
        var template = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(14, 00),
            VoteEndAt = new TimeOnly(16, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.RegisteredChats.AddRangeAsync(
            new RegisteredChat
            {
                ChatId = 123,
                ChatTitle = "Chat 1",
                ChatAlias = "chat1",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            },
            new RegisteredChat
            {
                ChatId = 124,
                ChatTitle = "Chat 2",
                ChatAlias = "chat2",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SlotTemplateId = template.Id,
            },
            new RegisteredChat
            {
                ChatId = 125,
                ChatTitle = "Chat 3",
                ChatAlias = "chat3",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            }
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedChatSlotTemplatesAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var chats = await verifyContext.RegisteredChats
            .OrderBy(x => x.ChatId)
            .ToListAsync(cancellationToken);

        Assert.That(chats, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(chats[0].SlotTemplateId, Is.EqualTo(template.Id), "First chat should be updated");
            Assert.That(chats[1].SlotTemplateId, Is.EqualTo(template.Id), "Second chat should remain unchanged");
            Assert.That(chats[2].SlotTemplateId, Is.EqualTo(template.Id), "Third chat should be updated");
        });
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SeedChatSlotTemplatesAsync_WhenAllChatsHaveTemplate_UpdatesNothing(CancellationToken cancellationToken)
    {
        // Arrange
        var timeProvider = FakeTimeProviderFactory.Create();
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed default template and chats
        await using var dbContext = dbContextFactory.CreateDbContext();
        var template = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(14, 00),
            VoteEndAt = new TimeOnly(16, 00),
            Number = 1,
        };
        dbContext.SlotTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.RegisteredChats.AddRangeAsync(
            new RegisteredChat
            {
                ChatId = 123,
                ChatTitle = "Chat 1",
                ChatAlias = "chat1",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SlotTemplateId = template.Id,
            },
            new RegisteredChat
            {
                ChatId = 456,
                ChatTitle = "Chat 2",
                ChatAlias = "chat2",
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                SlotTemplateId = template.Id,
            }
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedChatSlotTemplatesAsync(cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var chats = await verifyContext.RegisteredChats
            .OrderBy(x => x.ChatId)
            .ToListAsync(cancellationToken);

        Assert.That(chats, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(chats[0].SlotTemplateId, Is.EqualTo(template.Id), "First chat should remain unchanged");
            Assert.That(chats[1].SlotTemplateId, Is.EqualTo(template.Id), "Second chat should remain unchanged");
        });
    }
}
