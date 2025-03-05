namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface ISeedRepository
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task SeedDefaultSlotTemplateAsync(CancellationToken cancellationToken = default);
    Task SeedChatSlotTemplatesAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}
