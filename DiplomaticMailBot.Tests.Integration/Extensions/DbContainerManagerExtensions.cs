using DiplomaticMailBot.Tests.Integration.Services;

namespace DiplomaticMailBot.Tests.Integration.Extensions;

public static class DbContainerManagerExtensions
{
    public static async Task StopIfNotNullAsync(this DbContainerManager? containerManager)
    {
        if (containerManager != null)
        {
            await containerManager.StopAsync();
        }
    }
}
