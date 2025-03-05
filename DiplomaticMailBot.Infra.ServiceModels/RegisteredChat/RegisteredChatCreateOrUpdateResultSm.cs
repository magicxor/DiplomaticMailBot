namespace DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;

public sealed class RegisteredChatCreateOrUpdateResultSm
{
    public required bool IsCreated { get; set; }
    public required bool IsUpdated { get; set; }

    public required long ChatId { get; set; }

    public required string ChatTitle { get; set; }

    public required string ChatAlias { get; set; }
}
