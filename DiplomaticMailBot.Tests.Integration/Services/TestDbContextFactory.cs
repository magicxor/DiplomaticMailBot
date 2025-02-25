using DiplomaticMailBot.Data.DbContexts;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Tests.Integration.Services;

public class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly string _connectionString;

    public TestDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    [MustDisposeResource]
    public ApplicationDbContext CreateDbContext()
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString, sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;
        return new ApplicationDbContext(dbContextOptions);
    }
}
