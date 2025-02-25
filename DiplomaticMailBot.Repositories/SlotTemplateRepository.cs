using DiplomaticMailBot.Data.DbContexts;
using DiplomaticMailBot.Entities;
using DiplomaticMailBot.ServiceModels.SlotTemplate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Repositories;

public sealed class SlotTemplateRepository
{
    private readonly ILogger<SlotTemplateRepository> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContextFactory;

    public SlotTemplateRepository(
        ILogger<SlotTemplateRepository> logger,
        IDbContextFactory<ApplicationDbContext> applicationDbContextFactory)
    {
        _logger = logger;
        _applicationDbContextFactory = applicationDbContextFactory;
    }

    public async Task<SlotTemplateSm?> GetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Getting default slot template");

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        var slotTemplate = await applicationDbContext.SlotTemplates.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (slotTemplate is null)
        {
            _logger.LogWarning("Default slot template not found");
            return null;
        }

        _logger.LogTrace("Default slot template found: voteStartAt={VoteStartAt}, voteEndAt={VoteEndAt}, number={Number}",
            slotTemplate.VoteStartAt,
            slotTemplate.VoteEndAt,
            slotTemplate.Number);

        var result = new SlotTemplateSm
        {
            Id = slotTemplate.Id,
            VoteStartAt = slotTemplate.VoteStartAt,
            VoteEndAt = slotTemplate.VoteEndAt,
            Number = slotTemplate.Number,
        };

        return result;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Seeding default slot template");

        var applicationDbContext = await _applicationDbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await applicationDbContext.SlotTemplates.AnyAsync(cancellationToken: cancellationToken))
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
}
