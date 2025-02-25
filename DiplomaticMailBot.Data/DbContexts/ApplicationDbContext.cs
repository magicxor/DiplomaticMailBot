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

    public DbSet<DiplomaticMailCandidate> DiplomaticMailCandidates { get; set; } = null!;
    public DbSet<DiplomaticMailOutbox> DiplomaticMailOutbox { get; set; } = null!;
    public DbSet<DiplomaticMailPoll> DiplomaticMailPolls { get; set; } = null!;
    public DbSet<DiplomaticRelation> DiplomaticRelations { get; set; } = null!;
    public DbSet<RegisteredChat> RegisteredChats { get; set; } = null!;
    public DbSet<SlotInstance> SlotInstances { get; set; } = null!;
    public DbSet<SlotTemplate> SlotTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ContextConfigurationUtils.SetValueConverters(builder);
        base.OnModelCreating(builder);

        builder.Entity<RegisteredChat>()
            .HasQueryFilter(x => x.IsDeleted == false);

        builder.Entity<DiplomaticRelation>()
            .HasQueryFilter(x => x.SourceChat!.IsDeleted == false && x.TargetChat!.IsDeleted == false);

        builder.Entity<SlotInstance>()
            .HasQueryFilter(x => x.FromChat!.IsDeleted == false && x.ToChat!.IsDeleted == false);

        builder.Entity<DiplomaticMailCandidate>()
            .HasQueryFilter(x => x.SlotInstance!.FromChat!.IsDeleted == false && x.SlotInstance!.ToChat!.IsDeleted == false);

        builder.Entity<DiplomaticMailOutbox>()
            .HasQueryFilter(x => x.SlotInstance!.FromChat!.IsDeleted == false && x.SlotInstance!.ToChat!.IsDeleted == false);

        builder.Entity<DiplomaticMailPoll>()
            .HasQueryFilter(x => x.SlotInstance!.FromChat!.IsDeleted == false && x.SlotInstance!.ToChat!.IsDeleted == false);
    }
}
