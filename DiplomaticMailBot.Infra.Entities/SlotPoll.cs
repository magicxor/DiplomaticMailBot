using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Infra.Entities;

[Table("SlotPolls")]
[Index(nameof(SlotInstanceId), Name = $"{nameof(SlotPoll)}_{nameof(SlotInstanceId)}_Unique_IX", IsUnique = true)]
public class SlotPoll
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
    [ForeignKey(nameof(SlotPoll.SlotInstance))]
    public int SlotInstanceId { get; set; }

    // FK models
    [Required]
    public virtual SlotInstance SlotInstance
    {
        get => _slotInstance
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SlotInstance));
        set => _slotInstance = value;
    }

    private SlotInstance? _slotInstance;
}
