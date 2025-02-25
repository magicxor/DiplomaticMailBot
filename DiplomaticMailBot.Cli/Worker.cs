using DiplomaticMailBot.Services;

namespace DiplomaticMailBot.Cli;

public sealed class Worker : BackgroundService
{
    private readonly TimeSpan _scheduledProcessingPeriod = TimeSpan.FromSeconds(30);

    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var telegramBotService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
        var scheduledProcessingService = scope.ServiceProvider.GetRequiredService<ScheduledProcessingService>();
        var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();

        await seedService.SeedAsync(stoppingToken);
        await telegramBotService.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {Time}", timeProvider.GetUtcNow().DateTime);

            await scheduledProcessingService.ExecuteAsync(stoppingToken);

            await Task.Delay(_scheduledProcessingPeriod, stoppingToken);
        }
    }
}
