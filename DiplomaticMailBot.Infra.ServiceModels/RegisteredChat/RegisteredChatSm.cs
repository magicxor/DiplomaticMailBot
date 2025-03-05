namespace DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;

public sealed class RegisteredChatSm
{
    public required int Id { get; set; }

    public required long ChatId { get; set; }

    public required string ChatTitle { get; set; }

    public required string ChatAlias { get; set; }

    public required DateTime? CreatedAt { get; set; }
}
