using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Table("SlotInstances")]
[Index(nameof(Status))]
[Index(nameof(Date), nameof(TemplateId), nameof(SourceChatId), nameof(TargetChatId), Name = $"{nameof(SlotInstance)}_Unique_IX", IsUnique = true)]
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
    [ForeignKey(nameof(SlotInstance.SourceChat))]
    [Column("SourceChatId")]
    public int SourceChatId { get; set; }

    [Required]
    [ForeignKey(nameof(SlotInstance.TargetChat))]
    [Column("TargetChatId")]
    public int TargetChatId { get; set; }

    // FK models
    [Required]
    public virtual SlotTemplate Template
    {
        get => _template
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SlotTemplate));
        set => _template = value;
    }

    private SlotTemplate? _template;

    [Required]
    public virtual RegisteredChat SourceChat
    {
        get => _sourceChat
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(SourceChat));
        set => _sourceChat = value;
    }

    private RegisteredChat? _sourceChat;

    [Required]
    public virtual RegisteredChat TargetChat
    {
        get => _targetChat
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(TargetChat));
        set => _targetChat = value;
    }

    private RegisteredChat? _targetChat;

    // Relations
    public virtual ICollection<MessageCandidate> DiplomaticMailCandidates { get; set; } = new List<MessageCandidate>();
}
