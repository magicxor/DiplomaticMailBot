using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Entities;
using DiplomaticMailBot.Infra.Repositories.Implementations;
using DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;
using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using DiplomaticMailBot.Tests.Common;
using DiplomaticMailBot.Tests.Integration.Constants;
using DiplomaticMailBot.Tests.Integration.Exceptions;
using DiplomaticMailBot.Tests.Integration.Extensions;
using DiplomaticMailBot.Tests.Integration.Services;
using DiplomaticMailBot.Tests.Integration.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiplomaticMailBot.Tests.Integration.Tests;

[TestFixture]
[Parallelizable(scope: ParallelScope.Fixtures)]
public sealed class MessageOutboxRepositoryTests
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
    public async Task SendPendingMailsAsync_WhenMailsExist_ProcessesAndUpdatesThem(CancellationToken cancellationToken)
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
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(-1)), // Past date to ensure it's ready to send
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
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.MessageCandidates.Add(candidate);

        var outboxItem = new MessageOutbox
        {
            Status = MessageOutboxStatus.Pending,
            Attempts = 0,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
            MessageCandidate = candidate,
        };
        dbContext.MessageOutbox.Add(outboxItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageOutboxRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageOutboxRepository>(),
            dbContextFactory,
            timeProvider);

        var processedMails = new List<(RegisteredChatSm source, RegisteredChatSm target, MessageCandidateSm candidate)>();

        // Act
        await repository.SendPendingMailsAsync(
            async (source, target, mailCandidateSm, _) =>
            {
                processedMails.Add((source, target, mailCandidateSm));
                await Task.CompletedTask;
            },
            cancellationToken);

        // Assert
        Assert.That(processedMails, Has.Count.EqualTo(1));
        var processed = processedMails[0];

        Assert.That(processed.source.ChatId, Is.EqualTo(sourceChat.ChatId));
        Assert.That(processed.target.ChatId, Is.EqualTo(targetChat.ChatId));
        Assert.That(processed.candidate.MessageId, Is.EqualTo(candidate.MessageId));
        Assert.That(processed.candidate.Preview, Is.EqualTo(candidate.Preview));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var updatedOutboxItem = await verifyContext.MessageOutbox
            .FirstOrDefaultAsync(x => x.Id == outboxItem.Id, cancellationToken);

        Assert.That(updatedOutboxItem, Is.Not.Null);
        Assert.That(updatedOutboxItem!.Status, Is.EqualTo(MessageOutboxStatus.Sent));
        Assert.That(updatedOutboxItem.Attempts, Is.EqualTo(1));
        Assert.That(updatedOutboxItem.SentAt, Is.Not.Null);
    }

    [CancelAfter(TestDefaults.TestTimeout)]
    [Test]
    public async Task SendPendingMailsAsync_WhenProcessingFails_IncreasesAttemptCount(CancellationToken cancellationToken)
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
            Date = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
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

        var outboxItem = new MessageOutbox
        {
            Status = MessageOutboxStatus.Pending,
            Attempts = 0,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
            MessageCandidate = candidate,
        };
        dbContext.MessageOutbox.Add(outboxItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageOutboxRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageOutboxRepository>(),
            dbContextFactory,
            timeProvider);

        // Act
        await repository.SendPendingMailsAsync(
            (_, _, _, _) => throw new IntegrationTestException("Test exception"),
            cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var updatedOutboxItem = await verifyContext.MessageOutbox
            .FirstOrDefaultAsync(x => x.Id == outboxItem.Id, cancellationToken);

        Assert.That(updatedOutboxItem, Is.Not.Null);
        Assert.That(updatedOutboxItem!.Status, Is.EqualTo(MessageOutboxStatus.Pending));
        Assert.That(updatedOutboxItem.Attempts, Is.EqualTo(1));
        Assert.That(updatedOutboxItem.SentAt, Is.Null);
    }
}
