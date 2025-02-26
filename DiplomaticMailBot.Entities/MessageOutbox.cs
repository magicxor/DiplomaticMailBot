using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Table("MessageOutbox")]
[Index(nameof(Status), Name = $"{nameof(MessageOutbox)}_{nameof(Status)}_IX")]
[Index(nameof(SlotInstanceId), Name = $"{nameof(MessageOutbox)}_{nameof(SlotInstanceId)}_IX", IsUnique = true)]
public class MessageOutbox
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public required string Status { get; set; }

    [MaxLength(2048)]
    public string? StatusDetails { get; set; }

    [Required]
    public int Attempts { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(MessageOutbox.SlotInstance))]
    public int SlotInstanceId { get; set; }

    [Required]
    [ForeignKey(nameof(MessageOutbox.MessageCandidate))]
    public int DiplomaticMailCandidateId { get; set; }

    // FK models
    [Required]
    public virtual SlotInstance SlotInstance
    {
        get => _slotInstance
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SlotInstance));
        set => _slotInstance = value;
    }

    private SlotInstance? _slotInstance;

    [Required]
    public virtual MessageCandidate MessageCandidate
    {
        get => _diplomaticMailCandidate
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(MessageCandidate));
        set => _diplomaticMailCandidate = value;
    }

    private MessageCandidate? _diplomaticMailCandidate;
}
