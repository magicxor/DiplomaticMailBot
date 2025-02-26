using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.ServiceModels.MessageCandidate;
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
public class MessageCandidateRepositoryTests : IntegrationTestBase
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

        var outgoingRelation = new DiplomaticRelation
        {
            SourceChat = sourceChat,
            TargetChat = targetChat,
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
        };
        var incomingRelation = new DiplomaticRelation
        {
            SourceChat = targetChat,
            TargetChat = sourceChat,
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
        };
        await dbContext.DiplomaticRelations.AddRangeAsync(outgoingRelation, incomingRelation);

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
            TimeProvider);

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
            NextVoteSlotDate = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime),
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
            Date = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime),
            FromChat = sourceChat,
            ToChat = targetChat,
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
            CreatedAt = TimeProvider.GetUtcNow().UtcDateTime,
            SlotInstance = slotInstance,
        };
        dbContext.MessageCandidates.Add(candidate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new MessageCandidateRepository(
            NullLoggerFactory.Instance.CreateLogger<MessageCandidateRepository>(),
            dbContextFactory,
            TimeProvider);

        // Act
        var deletedCandidatesCountResult = await repository.WithdrawAsync(sourceChat.ChatId, candidate.MessageId, candidate.SubmitterId, cancellationToken);

        // Assert
        Assert.That(deletedCandidatesCountResult.Match(err => err.Code, deletedCount => deletedCount), Is.EqualTo(1));

        await using var verifyContext = dbContextFactory.CreateDbContext();
        var deletedCandidate = await verifyContext.MessageCandidates
            .FirstOrDefaultAsync(x => x.MessageId == candidate.MessageId, cancellationToken);

        Assert.That(deletedCandidate, Is.Null);
    }
}
