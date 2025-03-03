using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Data.Utils;
using DiplomaticMailBot.Tests.Integration.Constants;
using DiplomaticMailBot.Tests.Integration.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Tests.Integration.Utils;

public static class TestDbUtils
{
    public static string GetExactConnectionString(string host, ushort port, string db, string password)
    {
        return $"Host={host};Port={port};Database={db};Username=postgres;Password={password}";
    }

    private static string GetRandomizedConnectionString(string host, ushort port, string db, string password = TestDefaults.DbPassword)
    {
        return GetExactConnectionString(host, port, db + Guid.NewGuid().ToString("D"), password);
    }

    [MustDisposeResource]
    private static ApplicationDbContext CreateApplicationDbContext(string connectionString)
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, ContextConfiguration.NpgsqlOptionsAction)
            .LogTo((eventId, logLevel) => eventId.Id == Defaults.EfExecutedDbCommandEventId || logLevel >= LogLevel.Information,
                eventData =>
                {
                    TestLogUtils.WriteProgressMessage(eventData.ToString());
                    TestLogUtils.WriteConsoleMessage(eventData.ToString());
                })
            .Options;
        return new ApplicationDbContext(dbContextOptions);
    }

    private static string CreateNewRandomConnectionString()
    {
        return GetRandomizedConnectionString(GlobalSetUp.DbHost, GlobalSetUp.DbPort, TestDefaults.DbName);
    }

    /// <summary>
    /// Creates an empty <see cref="ApplicationDbContext"/> database with a random name.
    /// </summary>
    public static async Task<RespawnableContextManager<ApplicationDbContext>> CreateNewRandomDbContextManagerAsync()
    {
        var connectionString = CreateNewRandomConnectionString();
        var dbContext = new RespawnableContextManager<ApplicationDbContext>(connectionString, CreateApplicationDbContext);
        await dbContext.StartAsync();
        return dbContext;
    }
}
