using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Table("DiplomaticRelations")]
[Index(nameof(SourceChatId), nameof(TargetChatId), Name = $"{nameof(DiplomaticRelation)}_Unique_IX", IsUnique = true)]
public class DiplomaticRelation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    // FK id
    [Required]
    [ForeignKey(nameof(DiplomaticRelation.SourceChat))]
    public int SourceChatId { get; set; }

    [Required]
    [ForeignKey(nameof(DiplomaticRelation.TargetChat))]
    public int TargetChatId { get; set; }

    // FK models
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
}
