using System.ComponentModel.DataAnnotations;

namespace DiplomaticMailBot.Common.Configuration;

public sealed class BotConfiguration
{
    [Required]
    [MinLength(8)]
    [RegularExpression(@".*:.*")]
    public string TelegramBotApiKey { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string Culture { get; set; } = "en-US";
}
