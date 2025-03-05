namespace DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;

public class MessageCandidateSm
{
    public required int MessageId { get; set; }
    public required string AuthorName { get; set; }
    public required string Preview { get; set; }
}
