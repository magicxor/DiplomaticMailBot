using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Index(nameof(MessageId), nameof(SlotInstanceId), Name = $"{nameof(DiplomaticMailCandidate)}_Unique_IX", IsUnique = true)]
public class DiplomaticMailCandidate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required int MessageId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string Preview { get; set; }

    [Required]
    public required long SubmitterId { get; set; }

    [Required]
    public required long AuthorId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string AuthorName { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(DiplomaticMailCandidate.SlotInstance))]
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
