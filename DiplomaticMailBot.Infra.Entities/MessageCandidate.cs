using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiplomaticMailBot.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Infra.Entities;

[Table("MessageCandidates")]
[Index(nameof(MessageId), nameof(SlotInstanceId), Name = $"{nameof(MessageCandidate)}_Unique_IX", IsUnique = true)]
public class MessageCandidate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required int MessageId { get; set; }

    [Required]
    [MaxLength(Defaults.DbMessagePreviewMaxLength)]
    public required string Preview { get; set; }

    [Required]
    public required long SubmitterId { get; set; }

    [Required]
    public required long AuthorId { get; set; }

    [Required]
    [MaxLength(Defaults.DbAuthorNameMaxLength)]
    public required string AuthorName { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(MessageCandidate.SlotInstance))]
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
