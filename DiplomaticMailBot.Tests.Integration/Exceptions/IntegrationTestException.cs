﻿namespace DiplomaticMailBot.Tests.Integration.Exceptions;

public sealed class IntegrationTestException : Exception
{
    public IntegrationTestException(string message)
        : base(message)
    {
    }
}
