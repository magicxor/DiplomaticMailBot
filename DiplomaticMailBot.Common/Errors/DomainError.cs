using DiplomaticMailBot.Common.Exceptions;
using LanguageExt.Common;

namespace DiplomaticMailBot.Common.Errors;

public sealed record DomainError : Error
{
    public override bool Is<TException>()
    {
        return typeof(TException) == typeof(DomainException) && IsExceptional;
    }

    public override ErrorException ToErrorException()
    {
        return new DomainException(Code, Message, IsExceptional, IsExpected);
    }

    public DomainError(int code, string message, bool isExceptional = false, bool isExpected = true)
    {
        Code = code;
        Message = message;
        IsExceptional = isExceptional;
        IsExpected = isExpected;
    }

    public override int Code { get; }
    public override string Message { get; }
    public override bool IsExceptional { get; }
    public override bool IsExpected { get; }
}
