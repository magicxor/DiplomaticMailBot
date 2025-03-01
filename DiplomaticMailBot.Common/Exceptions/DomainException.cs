using DiplomaticMailBot.Common.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace DiplomaticMailBot.Common.Exceptions;

public sealed class DomainException : ErrorException
{
    public DomainException(int code, string message, bool isExceptional = false, bool isExpected = true, ErrorException? inner = null)
        : base(code)
    {
        Code = code;
        Message = message;
        IsExceptional = isExceptional;
        IsExpected = isExpected;
        Inner = inner;
    }

    public override Error ToError()
    {
        return new DomainError(Code, Message, IsExceptional, IsExpected);
    }

    public override ErrorException Append(ErrorException error)
    {
        if (error is ManyExceptions manyExceptions)
        {
            return new ManyExceptions(new Seq<ErrorException>([this, ..manyExceptions.Errors]));
        }
        else
        {
            return new ManyExceptions(new Seq<ErrorException>([this, error]));
        }
    }

    public override int Code { get; }
    public override string Message { get; }
    public override bool IsExceptional { get; }
    public override bool IsExpected { get; }
    public override Option<ErrorException> Inner { get; }
}
