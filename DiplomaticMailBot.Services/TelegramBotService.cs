using System.Text.RegularExpressions;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Services.CommandHandlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiplomaticMailBot.Services;

public sealed partial class TelegramBotService
{
    private static readonly ReceiverOptions ReceiverOptions = new()
    {
        AllowedUpdates = [UpdateType.Message, UpdateType.MyChatMember],
    };

    private readonly ILogger<TelegramBotService> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly RegisterChatHandler _registerChatHandler;
    private readonly EstablishRelationsHandler _establishRelationsHandler;
    private readonly BreakOffRelationsHandler _breakOffRelationsHandler;
    private readonly PutMessageHandler _putMessageHandler;
    private readonly WithdrawMessageHandler _withdrawMessageHandler;

    private User? _me;

    public TelegramBotService(
        ILogger<TelegramBotService> logger,
        ITelegramBotClient telegramBotClient,
        RegisterChatHandler registerChatHandler,
        EstablishRelationsHandler establishRelationsHandler,
        BreakOffRelationsHandler breakOffRelationsHandler,
        PutMessageHandler putMessageHandler,
        WithdrawMessageHandler withdrawMessageHandler)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _registerChatHandler = registerChatHandler;
        _establishRelationsHandler = establishRelationsHandler;
        _breakOffRelationsHandler = breakOffRelationsHandler;
        _putMessageHandler = putMessageHandler;
        _withdrawMessageHandler = withdrawMessageHandler;
    }

    private async Task HandleMyChatMemberUpdateAsync(
        User me,
        Update update,
        CancellationToken cancellationToken = default)
    {
        var myChatMember = update.MyChatMember;
        if (myChatMember is null)
        {
            _logger.LogTrace("Ignoring update without my_chat_member");
            return;
        }

        var chat = myChatMember.Chat;

        _logger.LogDebug("Handling my_chat_member update for chat {ChatId} ({ChatType}, {ChatTitle})", chat.Id, chat.Type, chat.Title);

        if (update.MyChatMember?.NewChatMember.Status is ChatMemberStatus.Kicked or ChatMemberStatus.Left or ChatMemberStatus.Restricted)
        {
            await _registerChatHandler.HandleDeregisterExitedChatAsync(me, chat, cancellationToken);
        }
    }

    private async Task HandleMessageUpdateAsync(
        User me,
        Update update,
        CancellationToken cancellationToken = default)
    {
        var message = update.Message;
        if (message is null)
        {
            _logger.LogTrace("Ignoring update without message");
            return;
        }

        var messageText = message.Text;
        if (string.IsNullOrWhiteSpace(messageText))
        {
            _logger.LogTrace("Ignoring update with empty message text");
            return;
        }

        var match = CommandRegex().Match(messageText);
        if (!match.Success)
        {
            return;
        }

        var commandBotUsername = match.Groups["botname"].Value;

        if (!string.IsNullOrWhiteSpace(commandBotUsername)
            && !string.IsNullOrWhiteSpace(me.Username)
            && !commandBotUsername.Equals(me.Username, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _logger.LogDebug("Handling command {MessageText}", messageText);

        var handlerTask = messageText switch
        {
            _ when messageText.StartsWith(BotCommands.ListChats, StringComparison.Ordinal) => _registerChatHandler.HandleListChatsAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.RegisterChat, StringComparison.Ordinal) => _registerChatHandler.HandleRegisterChatAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.DeregisterChat, StringComparison.Ordinal) => _registerChatHandler.HandleDeregisterChatAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.EstablishRelations, StringComparison.Ordinal) => _establishRelationsHandler.HandleEstablishRelationsAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.BreakOffRelations, StringComparison.Ordinal) => _breakOffRelationsHandler.HandleBreakOffRelationsAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.PutMessage, StringComparison.Ordinal) => _putMessageHandler.HandlePutMessageAsync(me, message, cancellationToken),
            _ when messageText.StartsWith(BotCommands.WithdrawMessage, StringComparison.Ordinal) => _withdrawMessageHandler.HandleWithdrawMessageAsync(me, message, cancellationToken),
            _ => Task.CompletedTask,
        };
        await handlerTask;
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var me = _me;
            if (me is null)
            {
                _logger.LogWarning("Bot identity is not known; ignoring update");
                return;
            }

            switch (update.Type)
            {
                case UpdateType.MyChatMember:
                    await HandleMyChatMemberUpdateAsync(me, update, cancellationToken);
                    break;
                case UpdateType.Message:
                    await HandleMessageUpdateAsync(me, update, cancellationToken);
                    break;
                default:
                    _logger.LogDebug("Ignoring update with unknown type: {UpdateType}", update.Type);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling update");
        }
    }

    private Task HandlePollingErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            _logger.LogError(exception,
                "Telegram API Error. ErrorCode={ErrorCode}, RetryAfter={RetryAfter}, MigrateToChatId={MigrateToChatId}",
                apiRequestException.ErrorCode,
                apiRequestException.Parameters?.RetryAfter,
                apiRequestException.Parameters?.MigrateToChatId);
        }
        else
        {
            _logger.LogError(exception, "Telegram API Error");
        }

        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Telegram bot");

        _me = await _telegramBotClient.GetMe(cancellationToken);

        _logger.LogInformation("Bot identity: {BotId} ({BotUsername})", _me?.Id, _me?.Username);

        _telegramBotClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: ReceiverOptions,
            cancellationToken: cancellationToken
        );
    }

    [GeneratedRegex("^/(?<command>[A-Za-z0-9_]+)(?:@(?<botname>[A-Za-z0-9_]+))?.*$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, 500)]
    private static partial Regex CommandRegex();
}
