using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.DiplomaticMailCandidate;
using DiplomaticMailBot.ServiceModels.RegisteredChat;
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
public class DiplomaticMailOutboxRepositoryTests : IntegrationTestBase
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
    public async Task SendPendingMailsAsync_WhenMailsExist_ProcessesAndUpdatesThem(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
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
            Date = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)), // Past date to ensure it's ready to send
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
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.DiplomaticMailCandidates.Add(candidate);

        var outboxItem = new DiplomaticMailOutbox
        {
            Status = DiplomaticMailOutboxStatus.Pending,
            Attempts = 0,
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
            DiplomaticMailCandidate = candidate,
        };
        dbContext.DiplomaticMailOutbox.Add(outboxItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticMailOutboxRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticMailOutboxRepository>(),
            dbContextFactory,
            _timeProvider);

        var processedMails = new List<(RegisteredChatSm source, RegisteredChatSm target, DiplomaticMailCandidateSm candidate)>();

        // Act
        await repository.SendPendingMailsAsync(
            async (source, target, mailCandidateSm, ct) =>
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
        var updatedOutboxItem = await verifyContext.DiplomaticMailOutbox
            .FirstOrDefaultAsync(x => x.Id == outboxItem.Id, cancellationToken);

        Assert.That(updatedOutboxItem, Is.Not.Null);
        Assert.That(updatedOutboxItem!.Status, Is.EqualTo(DiplomaticMailOutboxStatus.Sent));
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

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        var sourceChat = new RegisteredChat
        {
            ChatId = 123,
            ChatTitle = "Source Chat",
            ChatAlias = "source",
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
        };
        var targetChat = new RegisteredChat
        {
            ChatId = 456,
            ChatTitle = "Target Chat",
            ChatAlias = "target",
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
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
            Date = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            FromChat = sourceChat,
            ToChat = targetChat,
            Template = slotInstanceTemplate,
        };
        dbContext.SlotInstances.Add(slotInstance);

        var candidate = new DiplomaticMailCandidate
        {
            MessageId = 789,
            Preview = "Test message",
            SubmitterId = 101,
            AuthorId = 102,
            AuthorName = "Test Author",
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.DiplomaticMailCandidates.Add(candidate);

        var outboxItem = new DiplomaticMailOutbox
        {
            Status = DiplomaticMailOutboxStatus.Pending,
            Attempts = 0,
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
            DiplomaticMailCandidate = candidate,
        };
        dbContext.DiplomaticMailOutbox.Add(outboxItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new DiplomaticMailOutboxRepository(
            NullLoggerFactory.Instance.CreateLogger<DiplomaticMailOutboxRepository>(),
            dbContextFactory,
            _timeProvider);

        // Act
        await repository.SendPendingMailsAsync(
            (_, _, _, _) => throw new IntegrationTestException("Test exception"),
            cancellationToken);

        // Assert
        await using var verifyContext = dbContextFactory.CreateDbContext();
        var updatedOutboxItem = await verifyContext.DiplomaticMailOutbox
            .FirstOrDefaultAsync(x => x.Id == outboxItem.Id, cancellationToken);

        Assert.That(updatedOutboxItem, Is.Not.Null);
        Assert.That(updatedOutboxItem!.Status, Is.EqualTo(DiplomaticMailOutboxStatus.Pending));
        Assert.That(updatedOutboxItem.Attempts, Is.EqualTo(1));
        Assert.That(updatedOutboxItem.SentAt, Is.Null);
    }
}
