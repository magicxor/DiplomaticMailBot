using DiplomaticMailBot.Repositories;

namespace DiplomaticMailBot.Services;

public class SeedService
{
    private readonly SlotTemplateRepository _slotTemplateRepository;

    public SeedService(SlotTemplateRepository slotTemplateRepository)
    {
        _slotTemplateRepository = slotTemplateRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _slotTemplateRepository.SeedAsync(cancellationToken);
    }
}
