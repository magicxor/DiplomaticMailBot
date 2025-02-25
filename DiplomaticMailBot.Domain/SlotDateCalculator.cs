namespace DiplomaticMailBot.Domain;

public sealed class SlotDateCalculator
{
    private readonly TimeProvider _timeProvider;

    public SlotDateCalculator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateOnly GetNextAvailableSlotDate(TimeOnly voteStartsAt)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dateNow = DateOnly.FromDateTime(utcNow);
        var voteStartsAtDateTime = new DateTime(dateNow, voteStartsAt);

        var dateTomorrow = dateNow.AddDays(1);

        return utcNow < voteStartsAtDateTime
            ? dateNow
            : dateTomorrow;
    }

    public bool IsVotingGoingOn(TimeOnly voteStartsAt, TimeOnly voteEndsAt)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var dateNow = DateOnly.FromDateTime(utcNow);
        var voteStartsAtDateTime = new DateTime(dateNow, voteStartsAt);
        var voteEndsAtDateTime = new DateTime(dateNow, voteEndsAt);
        if (voteEndsAtDateTime <= voteStartsAtDateTime)
        {
            voteEndsAtDateTime = voteEndsAtDateTime.AddDays(1);
        }

        return voteStartsAtDateTime <= utcNow
               && voteEndsAtDateTime >= utcNow;
    }
}
