using System.Data;
using System.Data.Common;
using DiplomaticMailBot.Tests.Integration.Exceptions;
using DiplomaticMailBot.Tests.Integration.Utils;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace DiplomaticMailBot.Tests.Integration.Services;

public sealed class RespawnableContextManager<TContext> : IDisposable
    where TContext : DbContext
{
    private readonly string _dbConnectionString;
    private readonly Func<string, TContext> _dbContextCreator;

    private RespawnerOptions RespawnerOptions { get; init; } = new()
    {
        DbAdapter = DbAdapter.Postgres,
        SchemasToInclude = ["public"],
        TablesToIgnore = [new Table("public", "__EFMigrationsHistory")],
    };

    private DbConnection? _dbConnection;
    private Respawner? _respawner;

    public RespawnableContextManager(string dbConnectionString,
        Func<string, TContext> dbContextCreator)
    {
        _dbConnectionString = dbConnectionString;
        _dbContextCreator = dbContextCreator;
    }

    public async Task StartAsync()
    {
        await using var dbContext = _dbContextCreator(_dbConnectionString);
        await dbContext.Database.MigrateAsync();

        TestLogUtils.WriteProgressMessage($"Migrated DB {typeof(TContext).Name} {_dbConnection?.ConnectionString} successfully");

        _dbConnection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, RespawnerOptions);
    }

    private async Task ResetDatabaseAsync()
    {
        TestLogUtils.WriteProgressMessage($"Running {nameof(ResetDatabaseAsync)} on {typeof(TContext).Name} {_dbConnection?.ConnectionString}...");

        if (_respawner != null && _dbConnection != null)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
        else if (_respawner == null)
        {
            throw new TestConfigException($"{nameof(_respawner)} == null");
        }

        TestLogUtils.WriteProgressMessage($"Successfully performed {nameof(ResetDatabaseAsync)} on {typeof(TContext).Name} {_dbConnection?.ConnectionString}");
    }

    [MustDisposeResource]
    public async Task<TContext> CreateRespawnedDbContextAsync()
    {
        await ResetDatabaseAsync();
        return _dbContextCreator(_dbConnectionString);
    }

    public async Task<string> CreateRespawnedDbConnectionStringAsync()
    {
        await ResetDatabaseAsync();
        return _dbConnectionString;
    }

    public async Task StopAsync()
    {
        TestLogUtils.WriteProgressMessage($"Running {nameof(StopAsync)} on {typeof(TContext).Name} {_dbConnection?.ConnectionString}");

        _respawner = null;

        if (_dbConnection != null)
        {
            if (_dbConnection.State == ConnectionState.Open)
            {
                await _dbConnection.CloseAsync();
            }

            await _dbConnection.DisposeAsync();
        }

        await using var dbContext = _dbContextCreator(_dbConnectionString);
        await dbContext.Database.EnsureDeletedAsync();

        TestLogUtils.WriteProgressMessage($"Successfully performed {nameof(StopAsync)} on {typeof(TContext).Name}");
    }

    public void Dispose()
    {
        _dbConnection?.Dispose();
    }
}
