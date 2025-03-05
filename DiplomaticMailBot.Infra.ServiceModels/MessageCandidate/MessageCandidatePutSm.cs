namespace DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;

public sealed class MessageCandidatePutSm
{
    public required int SlotTemplateId { get; set; }
    public required DateOnly NextVoteSlotDate { get; set; }
    public required int MessageId { get; set; }
    public required string Preview { get; set; }
    public required long SubmitterId { get; set; }
    public required long AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required long SourceChatId { get; set; }
    public required string TargetChatAlias { get; set; }
}
