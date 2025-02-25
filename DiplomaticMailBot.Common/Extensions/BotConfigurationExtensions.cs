using System.Globalization;
using DiplomaticMailBot.Common.Configuration;

namespace DiplomaticMailBot.Common.Extensions;

public static class BotConfigurationExtensions
{
    public static CultureInfo GetCultureInfo(this BotConfiguration botConfiguration)
    {
        ArgumentNullException.ThrowIfNull(botConfiguration);

        return CultureInfo.GetCultureInfo(botConfiguration.Culture);
    }
}
