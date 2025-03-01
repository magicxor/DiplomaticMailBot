namespace DiplomaticMailBot.Common.Utils;

public static class SlotDateUtils
{
    public static DateOnly GetNearestVoteStartDate(DateTime utcNow, TimeOnly voteStartsAt)
    {
        if (!utcNow.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("utcNow must be in UTC", nameof(utcNow));
        }

        var today = DateOnly.FromDateTime(utcNow);
        var voteStartsAtDateTime = new DateTime(today, voteStartsAt, DateTimeKind.Utc);

        var tomorrow = today.AddDays(1);

        return utcNow < voteStartsAtDateTime
            ? today
            : tomorrow;
    }

    public static DateOnly GetNearestVoteEndDate(DateTime utcNow, TimeOnly voteEndsAt)
    {
        if (!utcNow.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("utcNow must be in UTC", nameof(utcNow));
        }

        var today = DateOnly.FromDateTime(utcNow);
        var voteEndsAtDateTime = new DateTime(today, voteEndsAt, DateTimeKind.Utc);
        if (voteEndsAtDateTime <= utcNow)
        {
            voteEndsAtDateTime = voteEndsAtDateTime.AddDays(1);
        }

        return DateOnly.FromDateTime(voteEndsAtDateTime);
    }

    public static bool IsVoteGoingOn(DateTime utcNow, TimeOnly voteStartsAt, TimeOnly voteEndsAt)
    {
        if (!utcNow.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("utcNow must be in UTC", nameof(utcNow));
        }

        var today = DateOnly.FromDateTime(utcNow);
        var voteStartsAtDateTime = new DateTime(today, voteStartsAt, DateTimeKind.Utc);
        var voteEndsAtDateTime = new DateTime(today, voteEndsAt, DateTimeKind.Utc);
        if (voteEndsAtDateTime <= voteStartsAtDateTime)
        {
            voteEndsAtDateTime = voteEndsAtDateTime.AddDays(1);
        }

        return voteStartsAtDateTime <= utcNow
               && voteEndsAtDateTime >= utcNow;
    }

    public static TimeSpan VoteStartsIn(DateTime utcNow, TimeOnly voteStartsAt)
    {
        if (!utcNow.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("utcNow must be in UTC", nameof(utcNow));
        }

        var nearestVoteStartDate = GetNearestVoteStartDate(utcNow, voteStartsAt);

        return new DateTime(nearestVoteStartDate, voteStartsAt, DateTimeKind.Utc) - utcNow;
    }

    public static TimeSpan VoteEndsIn(DateTime utcNow, TimeOnly voteEndsAt)
    {
        if (!utcNow.Kind.Equals(DateTimeKind.Utc))
        {
            throw new ArgumentException("utcNow must be in UTC", nameof(utcNow));
        }

        var nearestVoteEndDate = GetNearestVoteEndDate(utcNow, voteEndsAt);

        return new DateTime(nearestVoteEndDate, voteEndsAt, DateTimeKind.Utc) - utcNow;
    }
}
