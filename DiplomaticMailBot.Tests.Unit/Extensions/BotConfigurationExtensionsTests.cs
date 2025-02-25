using System.Globalization;
using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Common.Extensions;

namespace DiplomaticMailBot.Tests.Unit.Extensions;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class BotConfigurationExtensionsTests
{
    [Test]
    public void GetCultureInfo_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
    {
        BotConfiguration? config = null;
        Assert.Throws<ArgumentNullException>(() => config!.GetCultureInfo());
    }

    [Test]
    public void GetCultureInfo_WhenCultureIsValid_ShouldReturnCultureInfo()
    {
        var config = new BotConfiguration { Culture = "en-US" };
        var result = config.GetCultureInfo();
        Assert.That(result, Is.EqualTo(CultureInfo.GetCultureInfo("en-US")));
    }
}
