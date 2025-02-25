using System.Globalization;
using DiplomaticMailBot.Common.Extensions;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Domain;

public sealed class PreviewGenerator
{
    public string GetPollOptionPreview(int messageId, string authorName, string messagePreview, int authorNameMaxLength, int totalMaxLength)
    {
        return $"[{messageId}] ({authorName.TryLeft(authorNameMaxLength)}): {messagePreview}"
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace('\n', ' ')
            .TryLeft(totalMaxLength);
    }

    public string GetMessagePreview(Message message, int maxLength)
    {
        var fullText = message switch
        {
            not null when !string.IsNullOrWhiteSpace(message.Text) => message.Text,
            { Animation: not null } => GetMessageTypeAndCaption("Анимация", message.Caption),
            { Audio: not null } => GetMessageTypeAndCaption("Аудио", message.Caption),
            { Document: not null } => GetMessageTypeAndCaption("Документ", message.Caption),
            { Photo: not null } => GetMessageTypeAndCaption("Фото", message.Caption),
            { Sticker: not null } => GetMessageTypeAndCaption("Стикер", message.Caption),
            { Video: not null } => GetMessageTypeAndCaption("Видео", message.Caption),
            { VideoNote: not null } => GetMessageTypeAndCaption("Видеосообщение", message.Caption),
            { Voice: not null } => GetMessageTypeAndCaption("Голосовое сообщение", message.Caption),
            { Location: not null } => GetMessageTypeAndCaption("Местоположение", message.Caption),
            _ => "Не удалось определить тип сообщения",
        };

        return fullText.TryLeft(maxLength);
    }

    public string GetMessageTypeAndCaption(string type, string? caption)
    {
        return string.IsNullOrWhiteSpace(caption)
            ? type
            : $"({type}) {caption}".TrimEnd();
    }

    public string GetAuthorName(Message message, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(message);

        var authorName = message.From?.Username
                         ?? string.Join(' ', StringExtensions.GetNonEmpty(message.From?.FirstName, message.From?.LastName));

        return authorName.TryLeft(maxLength);
    }

    public string GetChatDisplayString(string chatAlias, string chatTitle)
    {
        return $"{chatAlias} ({chatTitle})";
    }

    public string GetMessageLinkUrl(long chatId, int messageId)
    {
        var chatIdString = chatId.ToString(CultureInfo.InvariantCulture)[4..];
        return $"https://t.me/c/{chatIdString}/{messageId}";
    }

    public string GetMessageLinkMarkdown(long chatId, int messageId)
    {
        var caption = messageId.ToString(CultureInfo.InvariantCulture);
        var url = GetMessageLinkUrl(chatId, messageId);
        return $"[{caption.EscapeSpecialTelegramMdCharacters()}]({url})";
    }

    public string GetMessageLinkMarkdown(long chatId, int messageId, string caption)
    {
        var url = GetMessageLinkUrl(chatId, messageId);
        return $"[{caption.EscapeSpecialTelegramMdCharacters()}]({url})";
    }

    public string GetMessageLinkHtml(long chatId, int messageId)
    {
        var caption = messageId.ToString(CultureInfo.InvariantCulture);
        var url = GetMessageLinkUrl(chatId, messageId);
        return $"""<a href="{url}">{caption}</a>""";
    }

    public string GetMessageLinkHtml(long chatId, int messageId, string caption)
    {
        var url = GetMessageLinkUrl(chatId, messageId);
        return $"""<a href="{url}">{caption}</a>""";
    }
}
