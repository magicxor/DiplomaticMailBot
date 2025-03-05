using Telegram.Bot.Types;

namespace DiplomaticMailBot.Infra.Telegram.Contracts;

public interface ITelegramInfoService
{
    Task<bool> IsSentByChatAdminAsync(Message message, CancellationToken cancellationToken = default);
}
