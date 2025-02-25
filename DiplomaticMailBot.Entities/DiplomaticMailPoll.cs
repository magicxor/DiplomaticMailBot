using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Index(nameof(SlotInstanceId), Name = $"{nameof(DiplomaticMailPoll)}_{nameof(SlotInstanceId)}_Unique_IX", IsUnique = true)]
public class DiplomaticMailPoll
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public required string Status { get; set; }

    [Required]
    public required int MessageId { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(DiplomaticMailPoll.SlotInstance))]
    public int SlotInstanceId { get; set; }

    // FK models
    [Required]
    public virtual SlotInstance? SlotInstance { get; set; }
}
