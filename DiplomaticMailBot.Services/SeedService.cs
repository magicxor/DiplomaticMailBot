using DiplomaticMailBot.Repositories;

namespace DiplomaticMailBot.Services;

public sealed class SeedService
{
    private readonly SeedRepository _seedRepository;

    public SeedService(SeedRepository seedRepository)
    {
        _seedRepository = seedRepository;
    }

    public async Task InitializeDbAsync(CancellationToken cancellationToken = default)
    {
        await _seedRepository.MigrateAsync(cancellationToken);
        await _seedRepository.SeedAsync(cancellationToken);
    }
}
