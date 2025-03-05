namespace DiplomaticMailBot.Infra.Repositories.Contracts;

public interface IPollRepository
{
    Task SendVoteApproachingRemindersAsync(
        SendVoteApproachingReminderCallback sendReminderCallback,
        CancellationToken cancellationToken = default);

    Task OpenPendingPollsAsync(
        SendChosenCandidateInfoMessageCallback sendMessageCallback,
        SendPollCallback sendPollCallback,
        CancellationToken cancellationToken = default);

    Task CloseExpiredPollsAsync(
        StopPollCallback stopPollCallback,
        CancellationToken cancellationToken = default);
}
