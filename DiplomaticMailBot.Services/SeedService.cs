using DiplomaticMailBot.Infra.Repositories.Contracts;

namespace DiplomaticMailBot.Services;

public sealed class SeedService
{
    private readonly ISeedRepository _seedRepository;

    public SeedService(ISeedRepository seedRepository)
    {
        _seedRepository = seedRepository;
    }

    public async Task InitializeDbAsync(CancellationToken cancellationToken = default)
    {
        await _seedRepository.MigrateAsync(cancellationToken);
        await _seedRepository.SeedAsync(cancellationToken);
    }
}
