using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Domain.Contracts;

public interface IPollOptionParser
{
    Either<int, Error> GetMessageId(string pollOptionText);
}
