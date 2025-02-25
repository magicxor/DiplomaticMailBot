using DiplomaticMailBot.Data.DbContexts;
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
public class SeedRepositoryTests
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
    public async Task SeedAsync_WhenNoTemplates_CreatesDefaultTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        var repository = new SeedRepository(
            NullLoggerFactory.Instance.CreateLogger<SeedRepository>(),
            dbContextFactory);

        // Act
        await repository.SeedAsync(cancellationToken);

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
    public async Task SeedAsync_WhenTemplatesExist_DoesNotCreateTemplate(CancellationToken cancellationToken)
    {
        // Arrange
        var dbConnectionString = await _contextManager!.CreateRespawnedDbConnectionStringAsync();
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);

        // Seed existing template
        await using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.SlotTemplates.Add(new DiplomaticMailBot.Entities.SlotTemplate
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
        await repository.SeedAsync(cancellationToken);

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
}
