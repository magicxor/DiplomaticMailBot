using System.ComponentModel.DataAnnotations;

namespace DiplomaticMailBot.Entities;

public class SlotTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required TimeOnly VoteStartAt { get; set; }

    [Required]
    public required TimeOnly VoteEndAt { get; set; }

    [Required]
    public required int Number { get; set; }

    // Relations
    public virtual ICollection<SlotInstance> SlotInstances { get; set; } = new List<SlotInstance>();
}
