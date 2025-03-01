using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.Tests.Integration.Constants;
using DiplomaticMailBot.Tests.Integration.Extensions;
using DiplomaticMailBot.Tests.Integration.Services;
using DiplomaticMailBot.Tests.Integration.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiplomaticMailBot.Tests.Integration.Tests;

[TestFixture]
[Parallelizable(scope: ParallelScope.Fixtures)]
public class SlotTemplateRepositoryTests
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
    public async Task GetSlotTemplateAsync_WhenCalled_ReturnsSlotTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed
        await using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.SlotTemplates.Add(new SlotTemplate
        {
            VoteStartAt = new TimeOnly(16, 00),
            VoteEndAt = new TimeOnly(18, 00),
            Number = 1,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        var slotTemplateRepository = new SlotTemplateRepository(NullLoggerFactory.Instance.CreateLogger<SlotTemplateRepository>(), dbContextFactory);
        var slotTemplate = await slotTemplateRepository.GetDefaultTemplateAsync(cancellationToken);

        // Assert
        AssertThrow.IsNotNull(slotTemplate);
        Assert.That(slotTemplate.VoteStartAt, Is.EqualTo(new TimeOnly(16, 00)));
        Assert.That(slotTemplate.VoteEndAt, Is.EqualTo(new TimeOnly(18, 00)));
        Assert.That(slotTemplate.Number, Is.EqualTo(1));
    }
}
