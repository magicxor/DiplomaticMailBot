namespace DiplomaticMailBot.Tests.Integration.Exceptions;

public class TestConfigException : Exception
{
    public TestConfigException(string message)
        : base(message)
    {
    }
}
