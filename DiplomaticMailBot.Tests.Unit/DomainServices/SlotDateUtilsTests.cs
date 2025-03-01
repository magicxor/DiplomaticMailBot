using DiplomaticMailBot.Common.Utils;
using Microsoft.Extensions.Time.Testing;

namespace DiplomaticMailBot.Tests.Unit.DomainServices;

[TestFixture]
public sealed class SlotDateUtilsTests
{
    private const string LocalTimeZoneId = "Etc/GMT-11";

    private FakeTimeProvider _timeProvider;

    [SetUp]
    public void Setup()
    {
        _timeProvider = new FakeTimeProvider();
    }

    [Test]
    public void GetNextAvailableSlotDate_WhenCurrentTimeBeforeVoteStart_ReturnsToday()
    {
        // Arrange
        var currentTime = new DateTime(2025, 02, 23, 10, 00, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 00); // 11:00

        // Act
        var result = SlotDateUtils.GetNearestVoteStartDate(utcNow, voteStartsAt);

        // Assert
        Assert.That(result, Is.EqualTo(DateOnly.FromDateTime(currentTime)));
    }

    [Test]
    public void GetNextAvailableSlotDate_WhenCurrentTimeAfterVoteStart_ReturnsTomorrow()
    {
        // Arrange
        var currentTime = new DateTime(2025, 02, 23, 11, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 00); // 11:00

        // Act
        var result = SlotDateUtils.GetNearestVoteStartDate(utcNow, voteStartsAt);

        // Assert
        Assert.That(result, Is.EqualTo(DateOnly.FromDateTime(currentTime.AddDays(1))));
    }

    [Test]
    public void GetNextAvailableSlotDate_WhenCurrentTimeAfterVoteStart2_ReturnsTomorrow()
    {
        // Arrange
        var currentTime = new DateTime(2025, 02, 23, 23, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(01, 00); // 11:00

        // Act
        var result = SlotDateUtils.GetNearestVoteStartDate(utcNow, voteStartsAt);

        // Assert
        Assert.That(result, Is.EqualTo(DateOnly.FromDateTime(currentTime.AddDays(1))));
    }

    [Test]
    public void IsVoteGoingOn_WhenCurrentTimeWithinVotingPeriod_ReturnsTrue()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 11, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0); // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsVoteGoingOn_WhenCurrentTimeBeforeVotingPeriod_ReturnsFalse()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 10, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0); // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsVoteGoingOn_WhenCurrentTimeAfterVotingPeriod_ReturnsFalse()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 12, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0); // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsVoteGoingOn_WhenVoteEndsCrossesOverToNextDay_HandlesCorrectly()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 23, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(23, 0); // 23:00
        var voteEndsAt = new TimeOnly(1, 0); // 01:00 next day

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetNearestVoteStartDate_WithNonUtc_ThrowsArgumentException()
    {
        // Arrange
        var localTime = new DateTime(2025, 03, 01, 12, 0, 0, DateTimeKind.Local);
        var voteStartsAt = new TimeOnly(11, 00);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SlotDateUtils.GetNearestVoteStartDate(localTime, voteStartsAt));
        Assert.That(ex.Message, Does.Contain("utcNow must be in UTC"));
    }

    [Test]
    public void GetNearestVoteEndDate_BeforeVoteEnds_ReturnsToday()
    {
        // Arrange: current time is before vote end time
        var currentTime = new DateTime(2025, 03, 01, 10, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteEndsAt = new TimeOnly(11, 00);

        // Act
        var result = SlotDateUtils.GetNearestVoteEndDate(utcNow, voteEndsAt);

        // Assert
        Assert.That(result, Is.EqualTo(DateOnly.FromDateTime(currentTime)));
    }

    [Test]
    public void GetNearestVoteEndDate_AfterVoteEnds_ReturnsTomorrow()
    {
        // Arrange: current time is after vote end time
        var currentTime = new DateTime(2025, 03, 01, 12, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteEndsAt = new TimeOnly(11, 00);

        // Act
        var result = SlotDateUtils.GetNearestVoteEndDate(utcNow, voteEndsAt);

        // Assert: should be tomorrow
        Assert.That(result, Is.EqualTo(DateOnly.FromDateTime(currentTime.AddDays(1))));
    }

    [Test]
    public void GetNearestVoteEndDate_WithNonUtc_ThrowsArgumentException()
    {
        // Arrange
        var localTime = new DateTime(2025, 03, 01, 12, 0, 0, DateTimeKind.Local);
        var voteEndsAt = new TimeOnly(11, 00);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SlotDateUtils.GetNearestVoteEndDate(localTime, voteEndsAt));
        Assert.That(ex.Message, Does.Contain("utcNow must be in UTC"));
    }

    [Test]
    public void VoteStartsIn_ReturnsCorrectTimeSpan_ForFutureVoteStartsAt()
    {
        // Arrange: current time is before vote start
        var currentTime = new DateTime(2025, 03, 01, 8, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(9, 0);

        // Act
        var timespan = SlotDateUtils.VoteStartsIn(utcNow, voteStartsAt);

        // Assert: should be 1 hour difference
        Assert.That(timespan, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    [Test]
    public void VoteStartsIn_ReturnsCorrectTimeSpan_ForPastVoteStartsAt()
    {
        // Arrange: current time is after today's vote start, so nearest vote start will be tomorrow
        var currentTime = new DateTime(2025, 03, 01, 10, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(9, 0); // already passed
        var expectedDateTime = new DateTime(2025, 03, 02, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var timespan = SlotDateUtils.VoteStartsIn(utcNow, voteStartsAt);

        // Assert: difference should equal tomorrow's vote start minus current time
        Assert.That(timespan, Is.EqualTo(expectedDateTime - currentTime));
    }

    [Test]
    public void VoteStartsIn_WithNonUtc_ThrowsArgumentException()
    {
        // Arrange
        var localTime = new DateTime(2025, 03, 01, 8, 0, 0, DateTimeKind.Local);
        var voteStartsAt = new TimeOnly(9, 0);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SlotDateUtils.VoteStartsIn(localTime, voteStartsAt));
        Assert.That(ex.Message, Does.Contain("utcNow must be in UTC"));
    }

    [Test]
    public void VoteEndsIn_ReturnsCorrectTimeSpan_ForFutureVoteEndsAt()
    {
        // Arrange: current time is before vote end
        var currentTime = new DateTime(2025, 03, 01, 8, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteEndsAt = new TimeOnly(9, 0);

        // Act
        var timespan = SlotDateUtils.VoteEndsIn(utcNow, voteEndsAt);

        // Assert: should be 1 hour difference
        Assert.That(timespan, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    [Test]
    public void VoteEndsIn_ReturnsCorrectTimeSpan_WhenVoteEndsNextDay()
    {
        // Arrange: current time is after today's vote end, so vote end is tomorrow
        var currentTime = new DateTime(2025, 03, 01, 10, 0, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteEndsAt = new TimeOnly(9, 0); // vote end today already passed
        var expectedDateTime = new DateTime(2025, 03, 02, 9, 0, 0, DateTimeKind.Utc);

        // Act
        var timespan = SlotDateUtils.VoteEndsIn(utcNow, voteEndsAt);

        // Assert: difference should equal tomorrow's vote end minus current time
        Assert.That(timespan, Is.EqualTo(expectedDateTime - currentTime));
    }

    [Test]
    public void VoteEndsIn_WithNonUtc_ThrowsArgumentException()
    {
        // Arrange
        var localTime = new DateTime(2025, 03, 01, 8, 0, 0, DateTimeKind.Local);
        var voteEndsAt = new TimeOnly(9, 0);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SlotDateUtils.VoteEndsIn(localTime, voteEndsAt));
        Assert.That(ex.Message, Does.Contain("utcNow must be in UTC"));
    }
}
