namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface IMessageOutboxRepository
{
    Task SendPendingMailsAsync(
        ProcessOutboxRecordCallback processOutboxRecordCallback,
        CancellationToken cancellationToken = default);
}
