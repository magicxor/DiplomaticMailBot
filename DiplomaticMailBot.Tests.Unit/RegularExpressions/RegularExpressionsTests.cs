using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.Services;

namespace DiplomaticMailBot.Tests.Unit.RegularExpressions;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class RegularExpressionsTests
{
    [TestCase($"{BotCommands.BreakOffRelations}@TestBot myalias", true, "TestBot", "myalias")]
    [TestCase($"{BotCommands.BreakOffRelations} myalias", true, null, "myalias")]
    [TestCase($"{BotCommands.BreakOffRelations}", false, null, null)]
    [TestCase($"{BotCommands.BreakOffRelations} ", false, null, null)]
    [TestCase($"{BotCommands.BreakOffRelations}@", false, null, null)]
    [TestCase($"{BotCommands.BreakOffRelations}@TestBot", false, null, null)]
    [TestCase("breakoff myalias", false, null, null)]
    [TestCase("", false, null, null)]
    public void BreakOffRelationsHandlerRegexTests(string input, bool expectedMatch, string? expectedBotName, string? expectedAlias)
    {
        // Arrange
        var regex = BreakOffRelationsHandler.BreakOffRelationsRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
                Assert.That(match.Groups["alias"].Value, Is.EqualTo(expectedAlias));
            });
        }
    }

    [TestCase($"{BotCommands.EstablishRelations}@TestBot myalias", true, "TestBot", "myalias")]
    [TestCase($"{BotCommands.EstablishRelations} myalias", true, null, "myalias")]
    [TestCase($"{BotCommands.EstablishRelations}", false, null, null)]
    [TestCase($"{BotCommands.EstablishRelations} ", false, null, null)]
    [TestCase($"{BotCommands.EstablishRelations}@", false, null, null)]
    [TestCase($"{BotCommands.EstablishRelations}@TestBot", false, null, null)]
    [TestCase("establish myalias", false, null, null)]
    [TestCase("", false, null, null)]
    public void EstablishRelationsHandlerRegexTests(string input, bool expectedMatch, string? expectedBotName, string? expectedAlias)
    {
        // Arrange
        var regex = EstablishRelationsHandler.EstablishRelationsRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
                Assert.That(match.Groups["alias"].Value, Is.EqualTo(expectedAlias));
            });
        }
    }

    [TestCase($"{BotCommands.PutMessage}@TestBot myalias", true, "TestBot", "myalias")]
    [TestCase($"{BotCommands.PutMessage} myalias", true, null, "myalias")]
    [TestCase($"{BotCommands.PutMessage}", false, null, null)]
    [TestCase($"{BotCommands.PutMessage} ", false, null, null)]
    [TestCase($"{BotCommands.PutMessage}@", false, null, null)]
    [TestCase($"{BotCommands.PutMessage}@TestBot", false, null, null)]
    [TestCase("put myalias", false, null, null)]
    [TestCase("", false, null, null)]
    public void PutMessageHandlerRegexTests(string input, bool expectedMatch, string? expectedBotName, string? expectedAlias)
    {
        // Arrange
        var regex = PutMessageHandler.PutMessageRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
                Assert.That(match.Groups["alias"].Value, Is.EqualTo(expectedAlias));
            });
        }
    }

    [TestCase($"{BotCommands.RegisterChat}@TestBot myalias", true, "TestBot", "myalias")]
    [TestCase($"{BotCommands.RegisterChat} myalias", true, null, "myalias")]
    [TestCase($"{BotCommands.RegisterChat}", false, null, null)]
    [TestCase($"{BotCommands.RegisterChat} ", false, null, null)]
    [TestCase($"{BotCommands.RegisterChat}@", false, null, null)]
    [TestCase($"{BotCommands.RegisterChat}@TestBot", false, null, null)]
    [TestCase("reg myalias", false, null, null)]
    [TestCase("", false, null, null)]
    public void RegisterChatHandlerRegexTests(string input, bool expectedMatch, string? expectedBotName, string? expectedAlias)
    {
        // Arrange
        var regex = RegisterChatHandler.RegisterChatRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
                Assert.That(match.Groups["alias"].Value, Is.EqualTo(expectedAlias));
            });
        }
    }

    [TestCase($"{BotCommands.DeregisterChat}@TestBot myalias", true, "TestBot", "myalias")]
    [TestCase($"{BotCommands.DeregisterChat} myalias", true, null, "myalias")]
    [TestCase($"{BotCommands.DeregisterChat}", false, null, null)]
    [TestCase($"{BotCommands.DeregisterChat} ", false, null, null)]
    [TestCase($"{BotCommands.DeregisterChat}@", false, null, null)]
    [TestCase($"{BotCommands.DeregisterChat}@TestBot", false, null, null)]
    [TestCase("deregister myalias", false, null, null)]
    [TestCase("", false, null, null)]
    public void DeregisterChatHandlerRegexTests(string input, bool expectedMatch, string? expectedBotName, string? expectedAlias)
    {
        // Arrange
        var regex = RegisterChatHandler.DeregisterChatRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
                Assert.That(match.Groups["alias"].Value, Is.EqualTo(expectedAlias));
            });
        }
    }

    [TestCase("/start@TestBot", true, "start", "TestBot")]
    [TestCase("/start", true, "start", null)]
    [TestCase("/help@TestBot", true, "help", "TestBot")]
    [TestCase("/help", true, "help", null)]
    [TestCase("/help ", true, "help", null)]
    [TestCase("help", false, null, null)]
    [TestCase("", false, null, null)]
    public void TelegramBotServiceCommandRegexTests(string input, bool expectedMatch, string? expectedCommand, string? expectedBotName)
    {
        // Arrange
        var regex = TelegramBotService.CommandRegex();

        // Act
        var match = regex.Match(input);

        // Assert
        Assert.That(match.Success, Is.EqualTo(expectedMatch));
        if (expectedMatch)
        {
            Assert.Multiple(() =>
            {
                Assert.That(match.Groups["command"].Value, Is.EqualTo(expectedCommand));
                Assert.That(match.Groups["botname"].Value, Is.EqualTo(expectedBotName ?? string.Empty));
            });
        }
    }
}
