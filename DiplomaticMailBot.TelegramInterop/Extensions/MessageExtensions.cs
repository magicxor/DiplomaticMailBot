﻿using Telegram.Bot.Types;

namespace DiplomaticMailBot.TelegramInterop.Extensions;

public static class MessageExtensions
{
    public static ReplyParameters ToReplyParameters(this Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ReplyParameters
        {
            ChatId = message.Chat.Id,
            MessageId = message.MessageId,
            AllowSendingWithoutReply = true,
        };
    }
}
