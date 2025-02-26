using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Table("SlotInstances")]
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
    [Column("SourceChatId")]
    public int FromChatId { get; set; }

    [Required]
    [ForeignKey(nameof(SlotInstance.ToChat))]
    [Column("TargetChatId")]
    public int ToChatId { get; set; }

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
    public virtual RegisteredChat FromChat
    {
        get => _fromChat
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(FromChat));
        set => _fromChat = value;
    }

    private RegisteredChat? _fromChat;

    [Required]
    public virtual RegisteredChat ToChat
    {
        get => _toChat
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ToChat));
        set => _toChat = value;
    }

    private RegisteredChat? _toChat;

    // Relations
    public virtual ICollection<MessageCandidate> DiplomaticMailCandidates { get; set; } = new List<MessageCandidate>();
}
