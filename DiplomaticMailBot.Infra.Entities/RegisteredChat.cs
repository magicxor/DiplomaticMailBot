using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DiplomaticMailBot.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Infra.Entities;

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
    [MaxLength(Defaults.DbChatTitleMaxLength)]
    public required string ChatTitle { get; set; }

    [Required]
    [MaxLength(Defaults.DbChatAliasMaxLength)]
    public required string ChatAlias { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; }

    // FK id
    [ForeignKey(nameof(RegisteredChat.SlotTemplate))]
    public int? SlotTemplateId { get; set; }

    // FK models
    public virtual SlotTemplate? SlotTemplate { get; set; }

    // Relations
    [InverseProperty(nameof(DiplomaticRelation.SourceChat))]
    public virtual ICollection<DiplomaticRelation> OutgoingDiplomaticRelations { get; set; } = new List<DiplomaticRelation>();

    [InverseProperty(nameof(DiplomaticRelation.TargetChat))]
    public virtual ICollection<DiplomaticRelation> IncomingDiplomaticRelations { get; set; } = new List<DiplomaticRelation>();

    [InverseProperty(nameof(SlotInstance.SourceChat))]
    public virtual ICollection<SlotInstance> SlotInstancesSource { get; set; } = new List<SlotInstance>();

    [InverseProperty(nameof(SlotInstance.TargetChat))]
    public virtual ICollection<SlotInstance> SlotInstancesTarget { get; set; } = new List<SlotInstance>();
}
