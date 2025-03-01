using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiplomaticMailBot.Entities;

[Table("SlotTemplates")]
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

    public virtual ICollection<RegisteredChat> Chats { get; set; } = new List<RegisteredChat>();
}
