namespace DiplomaticMailBot.Tests.Integration.Exceptions;

public sealed class TestConfigException : Exception
{
    public TestConfigException(string message)
        : base(message)
    {
    }
}
