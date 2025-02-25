namespace DiplomaticMailBot.Tests.Integration.Exceptions;

public class IntegrationTestException : Exception
{
    public IntegrationTestException(string message)
        : base(message)
    {
    }
}
