using DiplomaticMailBot.Tests.Integration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DiplomaticMailBot.Tests.Integration.Tests;

[TestFixture]
public sealed class EfMigrationsTests
{
    /* see
     https://www.meziantou.net/detect-missing-migrations-in-entity-framework-core.htm
     https://github.com/dotnet/efcore/issues/26348#issuecomment-1535156915
     */
    [Test]
    public async Task EnsureMigrationsAreUpToDate()
    {
        var dbConnectionString = string.Empty;
        var dbContextFactory = new TestDbContextFactory(dbConnectionString);
        await using var dbContext = dbContextFactory.CreateDbContext();

        // Get required services from the dbcontext
        var migrationModelDiffer = dbContext.GetService<IMigrationsModelDiffer>();
        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var modelRuntimeInitializer = dbContext.GetService<IModelRuntimeInitializer>();
        var designTimeModel = dbContext.GetService<IDesignTimeModel>();

        // Get current model
        var model = designTimeModel.Model;

        // Get the snapshot model and finalize it
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;
        if (snapshotModel is IMutableModel mutableModel)
        {
            // Forces post-processing on the model such that it is ready for use by the runtime
            snapshotModel = mutableModel.FinalizeModel();
        }

        if (snapshotModel is not null)
        {
            // Validates and initializes the given model with runtime dependencies
            snapshotModel = modelRuntimeInitializer.Initialize(snapshotModel);
        }

        // Compute differences
        var modelDifferences = migrationModelDiffer.GetDifferences(
            source: snapshotModel?.GetRelationalModel(),
            target: model.GetRelationalModel());

        // The differences should be empty if the migrations are up-to-date
        Assert.That(modelDifferences, Is.Empty);
    }
}
