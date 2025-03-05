using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Infra.Database.DbContexts;
using DiplomaticMailBot.Infra.Database.Utils;
using DiplomaticMailBot.Tests.Integration.Utils;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Tests.Integration.Services;

public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
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
            .LogTo((eventId, logLevel) => eventId.Id == Defaults.EfExecutedDbCommandEventId || logLevel >= LogLevel.Information,
                eventData =>
                {
                    TestLogUtils.WriteProgressMessage(eventData.ToString());
                    TestLogUtils.WriteConsoleMessage(eventData.ToString());
                })
            .Options;
        return new ApplicationDbContext(dbContextOptions);
    }
}
