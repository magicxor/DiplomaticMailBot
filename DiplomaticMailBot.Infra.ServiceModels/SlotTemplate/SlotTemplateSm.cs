namespace DiplomaticMailBot.Infra.ServiceModels.SlotTemplate;

public sealed class SlotTemplateSm
{
    public required int Id { get; set; }
    public required TimeOnly VoteStartAt { get; set; }
    public required TimeOnly VoteEndAt { get; set; }
    public required int Number { get; set; }
}
