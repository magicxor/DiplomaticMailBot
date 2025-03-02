namespace DiplomaticMailBot.Tests.Unit;

public sealed class UnitTestException : Exception
{
    public UnitTestException(string message)
        : base(message)
    {
    }
}
