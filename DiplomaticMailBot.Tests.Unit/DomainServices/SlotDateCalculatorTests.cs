using DiplomaticMailBot.Common.Utils;
using Microsoft.Extensions.Time.Testing;

namespace DiplomaticMailBot.Tests.Unit.DomainServices;

[TestFixture]
public sealed class SlotDateCalculatorTests
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
    public void IsVotingGoingOn_WhenCurrentTimeWithinVotingPeriod_ReturnsTrue()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 11, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0);   // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsVotingGoingOn_WhenCurrentTimeBeforeVotingPeriod_ReturnsFalse()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 10, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0);   // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsVotingGoingOn_WhenCurrentTimeAfterVotingPeriod_ReturnsFalse()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 12, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(11, 0); // 11:00
        var voteEndsAt = new TimeOnly(12, 0);   // 12:00

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsVotingGoingOn_WhenVoteEndsCrossesOverToNextDay_HandlesCorrectly()
    {
        // Arrange
        var currentTime = new DateTime(2025, 2, 23, 23, 30, 0, DateTimeKind.Utc);
        _timeProvider.SetUtcNow(currentTime);
        _timeProvider.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var voteStartsAt = new TimeOnly(23, 0); // 23:00
        var voteEndsAt = new TimeOnly(1, 0);    // 01:00 next day

        // Act
        var result = SlotDateUtils.IsVoteGoingOn(utcNow, voteStartsAt, voteEndsAt);

        // Assert
        Assert.That(result, Is.True);
    }
}
