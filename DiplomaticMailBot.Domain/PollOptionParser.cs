using System.Globalization;
using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Extensions;
using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Domain;

public sealed class PollOptionParser
{
    public Either<int, Error> GetMessageId(string pollOptionText)
    {
        if (string.IsNullOrWhiteSpace(pollOptionText))
        {
            return new DomainError(EventCode.MessageIdNotFound.ToInt(), "Message ID not found");
        }

        var openingBracketIndex = pollOptionText.IndexOf('[', StringComparison.Ordinal);

        if (openingBracketIndex == -1)
        {
            return new DomainError(EventCode.OpeningBracketNotFound.ToInt(), "Opening bracket not found");
        }

        var closingBracketIndex = pollOptionText.IndexOf(']', StringComparison.Ordinal);

        if (closingBracketIndex == -1)
        {
            return new DomainError(EventCode.ClosingBracketNotFound.ToInt(), "Closing bracket not found");
        }

        var bracketsContentLength = closingBracketIndex - openingBracketIndex - 1;

        if (bracketsContentLength <= 0)
        {
            return new DomainError(EventCode.MessageIdNotFound.ToInt(), "Message ID not found");
        }

        var bracketsContent = pollOptionText.Substring(openingBracketIndex + 1, bracketsContentLength);

        if (string.IsNullOrWhiteSpace(bracketsContent))
        {
            return new DomainError(EventCode.MessageIdNotFound.ToInt(), "Message ID not found");
        }

        if (!int.TryParse(bracketsContent, CultureInfo.InvariantCulture, out var messageId))
        {
            return new DomainError(EventCode.MessageIdNotFound.ToInt(), "Message ID not found");
        }
        else
        {
            return messageId;
        }
    }
}
