namespace DiplomaticMailBot.ServiceModels.DiplomaticRelation;

public sealed class DiplomaticRelationsInfoSm
{
    public required bool IsOutgoingRelationPresent { get; set; }
    public required bool IsIncomingRelationPresent { get; set; }

    public required long SourceChatId { get; set; }
    public required string SourceChatAlias { get; set; }
    public required string SourceChatTitle { get; set; }

    public required long TargetChatId { get; set; }
    public required string TargetChatAlias { get; set; }
    public required string TargetChatTitle { get; set; }
}
