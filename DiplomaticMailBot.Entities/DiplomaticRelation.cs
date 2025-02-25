using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Index(nameof(SourceChatId), nameof(TargetChatId), Name = $"{nameof(DiplomaticRelation)}_Unique_IX", IsUnique = true)]
public class DiplomaticRelation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    // FK id
    [ForeignKey(nameof(DiplomaticRelation.SourceChat))]
    public int SourceChatId { get; set; }

    [ForeignKey(nameof(DiplomaticRelation.TargetChat))]
    public int TargetChatId { get; set; }

    // FK models
    [Required]
    public virtual RegisteredChat? SourceChat { get; set; }

    [Required]
    public virtual RegisteredChat? TargetChat { get; set; }
}
