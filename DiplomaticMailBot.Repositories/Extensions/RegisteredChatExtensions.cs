using DiplomaticMailBot.Entities;

namespace DiplomaticMailBot.Repositories.Extensions;

public static class RegisteredChatExtensions
{
    public static bool IsSameAs(this RegisteredChat sourceChat, RegisteredChat targetChat)
    {
        return sourceChat.ChatId == targetChat.ChatId
               || sourceChat.ChatAlias.Equals(targetChat.ChatAlias, StringComparison.OrdinalIgnoreCase);
    }
}
