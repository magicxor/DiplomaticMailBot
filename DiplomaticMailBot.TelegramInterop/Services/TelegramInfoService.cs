using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.TelegramInterop.Services;

public sealed class TelegramInfoService
{
    private readonly ILogger<TelegramInfoService> _logger;
    private readonly ITelegramBotClient _telegramBotClient;

    public TelegramInfoService(
        ILogger<TelegramInfoService> logger,
        ITelegramBotClient telegramBotClient)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
    }

    public async Task<bool> IsSentByChatAdminAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Checking if message {MessageId} is sent by chat admin", message.MessageId);

        var userInfo = await _telegramBotClient.GetChatMember(message.Chat.Id, message.From?.Id ?? 0, cancellationToken);
        return userInfo.IsAdmin;
    }
}
