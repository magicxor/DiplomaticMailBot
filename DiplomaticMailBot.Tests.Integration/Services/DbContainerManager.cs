using DiplomaticMailBot.Tests.Integration.Constants;
using DiplomaticMailBot.Tests.Integration.Models;
using DiplomaticMailBot.Tests.Integration.Utils;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace DiplomaticMailBot.Tests.Integration.Services;

public class DbContainerManager
{
    private IContainer? _container;

    public async Task<ContainerInfo> StartAsync()
    {
        TestLogUtils.WriteProgressMessage("Starting the DB container...");

        var containerName = "dipmailbot_integration_test_db_" + Guid.NewGuid().ToString("D");

        var containerBuilder = new PostgreSqlBuilder()
            .WithName(containerName)
            .WithImage("postgres:17-bookworm")
            .WithExposedPort(TestDefaults.DbPort)
            .WithPassword(TestDefaults.DbPassword)
            .WithAutoRemove(true)
            .WithCleanUp(true);

        _container = containerBuilder.Build();

        using (var cancellationTokenSource = new CancellationTokenSource(TestDefaults.DbStartTimeout))
        {
            var cancellationToken = cancellationTokenSource.Token;
            await _container.StartAsync(cancellationToken);
        }

        var containerHostPort = _container.GetMappedPublicPort(TestDefaults.DbPort);

        TestLogUtils.WriteProgressMessage($"The DB container started successfully ({containerName})");

        return new ContainerInfo(containerHostPort, _container.Hostname);
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        TestLogUtils.WriteProgressMessage($"The DB container is about to stop ({_container?.Name}).");

        if (_container != null)
        {
            _ = Task.Run(_container.DisposeAsync, ct);
        }

        return Task.CompletedTask;
    }
}
