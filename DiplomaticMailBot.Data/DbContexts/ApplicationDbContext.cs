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

    public DbSet<MessageCandidate> DiplomaticMailCandidates { get; set; } = null!;
    public DbSet<MessageOutbox> DiplomaticMailOutbox { get; set; } = null!;
    public DbSet<SlotPoll> DiplomaticMailPolls { get; set; } = null!;
    public DbSet<DiplomaticRelation> DiplomaticRelations { get; set; } = null!;
    public DbSet<RegisteredChat> RegisteredChats { get; set; } = null!;
    public DbSet<SlotInstance> SlotInstances { get; set; } = null!;
    public DbSet<SlotTemplate> SlotTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ContextConfigurationUtils.SetValueConverters(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RegisteredChat>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<DiplomaticRelation>()
            .HasQueryFilter(x => !x.SourceChat!.IsDeleted && !x.TargetChat!.IsDeleted);

        modelBuilder.Entity<SlotInstance>()
            .HasQueryFilter(x => !x.FromChat!.IsDeleted && !x.ToChat!.IsDeleted);

        modelBuilder.Entity<MessageCandidate>()
            .HasQueryFilter(x => !x.SlotInstance!.FromChat!.IsDeleted && !x.SlotInstance!.ToChat!.IsDeleted);

        modelBuilder.Entity<MessageOutbox>()
            .HasQueryFilter(x => !x.SlotInstance!.FromChat!.IsDeleted && !x.SlotInstance!.ToChat!.IsDeleted);

        modelBuilder.Entity<SlotPoll>()
            .HasQueryFilter(x => !x.SlotInstance!.FromChat!.IsDeleted && !x.SlotInstance!.ToChat!.IsDeleted);
    }
}
