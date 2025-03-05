using DiplomaticMailBot.Infra.Entities;

namespace DiplomaticMailBot.Infra.Repositories.Implementations.Extensions;

public static class RegisteredChatExtensions
{
    public static bool IsSameAs(this RegisteredChat sourceChat, RegisteredChat targetChat)
    {
        ArgumentNullException.ThrowIfNull(sourceChat);
        ArgumentNullException.ThrowIfNull(targetChat);

        return sourceChat.ChatId == targetChat.ChatId
               || sourceChat.ChatAlias.Equals(targetChat.ChatAlias, StringComparison.OrdinalIgnoreCase);
    }
}
