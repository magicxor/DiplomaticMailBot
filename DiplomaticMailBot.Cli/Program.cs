using System.Diagnostics.CodeAnalysis;
using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Data.Utils;
using DiplomaticMailBot.Domain;
using DiplomaticMailBot.Repositories;
using DiplomaticMailBot.Services;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.TelegramInterop.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using Telegram.Bot;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DiplomaticMailBot.Cli;

public static class Program
{
    private static readonly LoggingConfiguration LoggingConfiguration = new XmlLoggingConfiguration("nlog.config");

    private static readonly Action<ILogger, string, Exception?> LogMessage =
        LoggerMessage.Define<string>(LogLevel.Debug, EventCode.DatabaseQuery.ToEventId(), "DB query: {Message}");

    [SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Entry point")]
    public static void Main(string[] args)
    {
        // NLog: set up the logger first to catch all errors
        LogManager.Configuration = LoggingConfiguration;
        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config
                        .AddEnvironmentVariables("DIPMAILBOT_")
                        .AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(LoggingConfiguration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptionsWithValidateOnStart<BotConfiguration>()
                        .Bind(hostContext.Configuration.GetSection("Bot"))
                        .ValidateDataAnnotations();

                    services
                        /* Infrastructure */
                        .AddScoped<TimeProvider>(_ => TimeProvider.System)
                        .AddScoped<ITelegramBotClient>(s =>
                        {
                            var botConfig = s.GetRequiredService<IOptions<BotConfiguration>>();
                            return new TelegramBotClient(botConfig.Value.TelegramBotApiKey);
                        })
                        .AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
                        {
                            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
                            options
                                .UseNpgsql(hostContext.Configuration.GetConnectionString("DefaultConnection"), ContextConfiguration.NpgsqlOptionsAction)
                                .LogTo(msg => LogMessage(logger, msg, null));
                        })
                        .AddScoped<TelegramInfoService>()
                        /* Repositories */
                        .AddScoped<RegisteredChatRepository>()
                        .AddScoped<DiplomaticRelationRepository>()
                        .AddScoped<MessageOutboxRepository>()
                        .AddScoped<PollRepository>()
                        .AddScoped<MessageCandidateRepository>()
                        .AddScoped<SlotTemplateRepository>()
                        .AddScoped<SeedRepository>()
                        /* Domain */
                        .AddScoped<PreviewGenerator>()
                        .AddScoped<PollOptionParser>()
                        /* Services */
                        .AddScoped<RegisterChatHandler>()
                        .AddScoped<BreakOffRelationsHandler>()
                        .AddScoped<EstablishRelationsHandler>()
                        .AddScoped<PutMessageHandler>()
                        .AddScoped<WithdrawMessageHandler>()
                        .AddScoped<TelegramBotService>()
                        .AddScoped<ScheduledProcessingService>()
                        .AddScoped<SeedService>()
                        /* Presentation */
                        .AddHostedService<Worker>();
                })
                .Build();

            host.Run();
        }
        catch (Exception ex)
        {
            // NLog: catch setup errors
            LogManager.GetCurrentClassLogger().Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }
}
