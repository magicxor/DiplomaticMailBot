using DiplomaticMailBot.Data.DbContexts;
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

    public async Task<SlotTemplateSm?> GetDefaultTemplateAsync(CancellationToken cancellationToken = default)
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
}
