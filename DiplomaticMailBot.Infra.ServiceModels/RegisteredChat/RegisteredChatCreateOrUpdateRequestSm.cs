namespace DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;

public sealed class RegisteredChatCreateOrUpdateRequestSm
{
    public required long ChatId { get; set; }

    public required string ChatTitle { get; set; }

    public required string ChatAlias { get; set; }
}
