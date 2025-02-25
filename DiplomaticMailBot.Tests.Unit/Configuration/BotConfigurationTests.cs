using System.ComponentModel.DataAnnotations;
using DiplomaticMailBot.Common.Configuration;

namespace DiplomaticMailBot.Tests.Unit.Configuration;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class BotConfigurationTests
{
    [Test]
    public void BotConfiguration_WhenCreated_ShouldHaveDefaultValues()
    {
        var config = new BotConfiguration();
        Assert.Multiple(() =>
        {
            Assert.That(config.TelegramBotApiKey, Is.EqualTo(string.Empty));
            Assert.That(config.Culture, Is.EqualTo("en-US"));
        });
    }

    [Test]
    public void BotConfiguration_WhenTelegramBotApiKeyIsNull_ShouldFailValidation()
    {
        var config = new BotConfiguration { TelegramBotApiKey = null! };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, context, results, true);

        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].ErrorMessage, Does.Contain("TelegramBotApiKey"));
        });
    }

    [Test]
    public void BotConfiguration_WhenTelegramBotApiKeyIsTooShort_ShouldFailValidation()
    {
        var config = new BotConfiguration { TelegramBotApiKey = "123:456" };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, context, results, true);

        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].ErrorMessage, Does.Contain("TelegramBotApiKey"));
        });
    }

    [Test]
    public void BotConfiguration_WhenTelegramBotApiKeyDoesNotContainColon_ShouldFailValidation()
    {
        var config = new BotConfiguration { TelegramBotApiKey = "12345678" };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, context, results, true);

        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.False);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].ErrorMessage, Does.Contain("TelegramBotApiKey"));
        });
    }

    [Test]
    public void BotConfiguration_WhenTelegramBotApiKeyIsValid_ShouldPassValidation()
    {
        var config = new BotConfiguration { TelegramBotApiKey = "1234:5678" };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, context, results, true);

        Assert.Multiple(() =>
        {
            Assert.That(isValid, Is.True);
            Assert.That(results, Is.Empty);
        });
    }
}
