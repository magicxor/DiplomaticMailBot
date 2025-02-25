using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Index(nameof(Status))]
[Index(nameof(Date), nameof(TemplateId), nameof(FromChatId), nameof(ToChatId), Name = $"{nameof(SlotInstance)}_Unique_IX", IsUnique = true)]
public class SlotInstance
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public required string Status { get; set; }

    [Required]
    public required DateOnly Date { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(SlotInstance.Template))]
    public int TemplateId { get; set; }

    [Required]
    [ForeignKey(nameof(SlotInstance.FromChat))]
    public int FromChatId { get; set; }

    [Required]
    [ForeignKey(nameof(SlotInstance.ToChat))]
    public int ToChatId { get; set; }

    // FK models
    [Required]
    public virtual SlotTemplate? Template { get; set; }

    [Required]
    public virtual RegisteredChat? FromChat { get; set; }

    [Required]
    public virtual RegisteredChat? ToChat { get; set; }

    // Relations
    public virtual ICollection<DiplomaticMailCandidate> DiplomaticMailCandidates { get; set; } = new List<DiplomaticMailCandidate>();
}
