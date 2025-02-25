using DiplomaticMailBot.Tests.Integration.Services;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Tests.Integration.Extensions;

public static class RespawnableContextManagerExtensions
{
    public static async Task StopIfNotNullAsync<T>(this RespawnableContextManager<T>? respawnableContextManager)
        where T : DbContext
    {
        if (respawnableContextManager != null)
        {
            await respawnableContextManager.StopAsync();
            respawnableContextManager.Dispose();
        }
    }
}
