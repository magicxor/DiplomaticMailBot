using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class SeedRepository
{
    private readonly ILogger<SeedRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;

    public SeedRepository(
        ILogger<SeedRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        await applicationDbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Migrations applied");
    }

    public async Task SeedDefaultSlotTemplateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Seeding default slot template");

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await applicationDbContext.SlotTemplates.TagWithCallSite().AnyAsync(cancellationToken: cancellationToken))
        {
            _logger.LogDebug("Default slot template already seeded");
            return;
        }

        var slotTemplate = new SlotTemplate
        {
            VoteStartAt = new TimeOnly(14, 00),
            VoteEndAt = new TimeOnly(16, 00),
            Number = 1,
        };
        applicationDbContext.SlotTemplates.Add(slotTemplate);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default slot template seeded: voteStartAt={VoteStartAt}, voteEndAt={VoteEndAt}, number={Number}",
            slotTemplate.VoteStartAt,
            slotTemplate.VoteEndAt,
            slotTemplate.Number);
    }

    public async Task SeedChatSlotTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Seeding chat slot templates");

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);
        var defaultSlotTemplate = await applicationDbContext.SlotTemplates
            .TagWithCallSite()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultSlotTemplate == null)
        {
            _logger.LogWarning("Default slot template not found");
            return;
        }

        var rowsUpdated = await applicationDbContext.RegisteredChats
            .TagWithCallSite()
            .Where(x => x.SlotTemplateId == null)
            .ExecuteUpdateAsync(calls =>
                calls.SetProperty(
                    chat => chat.SlotTemplateId,
                    defaultSlotTemplate.Id),
                cancellationToken);

        _logger.LogInformation("Updated {RowsUpdated} chats that had no slot template", rowsUpdated);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDefaultSlotTemplateAsync(cancellationToken);
        await SeedChatSlotTemplatesAsync(cancellationToken);
    }
}
