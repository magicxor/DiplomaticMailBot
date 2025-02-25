using Microsoft.Extensions.Time.Testing;

namespace DiplomaticMailBot.Tests.Integration.Tests;

public abstract class IntegrationTestBase
{
    protected FakeTimeProvider TimeProvider { get; }

    protected IntegrationTestBase()
    {
        TimeProvider = new FakeTimeProvider();
        TimeProvider.SetUtcNow(new DateTimeOffset(2025, 2, 25, 16, 40, 39, TimeSpan.Zero)); // Setting a fixed time for all tests
    }
}
