using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Entities;

[Table("RegisteredChats")]
[Index(nameof(ChatId), Name = $"{nameof(RegisteredChat)}_{nameof(ChatId)}_Unique_IX", IsUnique = true)]
[Index(nameof(ChatAlias), Name = $"{nameof(RegisteredChat)}_{nameof(ChatAlias)}_Unique_IX", IsUnique = true)]
[Index(nameof(IsDeleted))]
public class RegisteredChat
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required long ChatId { get; set; }

    [Required]
    [MaxLength(128)]
    public required string ChatTitle { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(128)]
    public required string ChatAlias { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; }

    // Relations
    [InverseProperty(nameof(DiplomaticRelation.SourceChat))]
    public virtual ICollection<DiplomaticRelation> OutgoingDiplomaticRelations { get; set; } = new List<DiplomaticRelation>();

    [InverseProperty(nameof(DiplomaticRelation.TargetChat))]
    public virtual ICollection<DiplomaticRelation> IncomingDiplomaticRelations { get; set; } = new List<DiplomaticRelation>();

    [InverseProperty(nameof(SlotInstance.FromChat))]
    public virtual ICollection<SlotInstance> SlotInstancesSource { get; set; } = new List<SlotInstance>();

    [InverseProperty(nameof(SlotInstance.ToChat))]
    public virtual ICollection<SlotInstance> SlotInstancesTarget { get; set; } = new List<SlotInstance>();
}
