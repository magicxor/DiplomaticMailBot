using DiplomaticMailBot.Data.Extensions;
using DiplomaticMailBot.Data.Utils;
using DiplomaticMailBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiplomaticMailBot.Data.DbContexts;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MessageCandidate> MessageCandidates { get; set; } = null!;
    public DbSet<MessageOutbox> MessageOutbox { get; set; } = null!;
    public DbSet<SlotPoll> SlotPolls { get; set; } = null!;
    public DbSet<DiplomaticRelation> DiplomaticRelations { get; set; } = null!;
    public DbSet<RegisteredChat> RegisteredChats { get; set; } = null!;
    public DbSet<SlotInstance> SlotInstances { get; set; } = null!;
    public DbSet<SlotTemplate> SlotTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ContextConfigurationUtils.SetValueConverters(modelBuilder);
        modelBuilder.AddEfFunctions();

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RegisteredChat>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<DiplomaticRelation>()
            .HasQueryFilter(x => !x.SourceChat.IsDeleted && !x.TargetChat.IsDeleted);

        modelBuilder.Entity<SlotInstance>()
            .HasQueryFilter(x => !x.SourceChat.IsDeleted && !x.TargetChat.IsDeleted);

        modelBuilder.Entity<MessageCandidate>()
            .HasQueryFilter(x => !x.SlotInstance.SourceChat.IsDeleted && !x.SlotInstance.TargetChat.IsDeleted);

        modelBuilder.Entity<MessageOutbox>()
            .HasQueryFilter(x => !x.SlotInstance.SourceChat.IsDeleted && !x.SlotInstance.TargetChat.IsDeleted);

        modelBuilder.Entity<SlotPoll>()
            .HasQueryFilter(x => !x.SlotInstance.SourceChat.IsDeleted && !x.SlotInstance.TargetChat.IsDeleted);
    }
}
