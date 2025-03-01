using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Data.Utils;
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
            .UseNpgsql(_connectionString, ContextConfiguration.NpgsqlOptionsAction)
            .Options;
        return new ApplicationDbContext(dbContextOptions);
    }
}
