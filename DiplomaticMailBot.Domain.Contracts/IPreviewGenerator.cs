using Telegram.Bot.Types;

namespace DiplomaticMailBot.Domain.Contracts;

public interface IPreviewGenerator
{
    string GetPollOptionPreview(int messageId, string authorName, string messagePreview, int authorNameMaxLength, int totalMaxLength);
    string GetMessagePreview(Message message, int maxLength);
    string GetMessageTypeAndCaption(string type, string? caption);
    string GetAuthorName(Message message, int maxLength);
    string GetChatDisplayString(string chatAlias, string chatTitle);
    string GetMessageLinkUrl(long chatId, int messageId);
    string GetMessageLinkMarkdown(long chatId, int messageId);
    string GetMessageLinkMarkdown(long chatId, int messageId, string caption);
    string GetMessageLinkHtml(long chatId, int messageId);
    string GetMessageLinkHtml(long chatId, int messageId, string caption);
}
