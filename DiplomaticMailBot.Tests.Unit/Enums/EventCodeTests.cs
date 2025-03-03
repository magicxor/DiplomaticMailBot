using System.Globalization;
using DiplomaticMailBot.Common.Enums;

namespace DiplomaticMailBot.Tests.Unit.Enums;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class EventCodeTests
{
    [Test]
    public void EventCode_ShouldNotHaveDuplicateValues()
    {
        // Arrange
        var enumType = typeof(EventCode);
        var enumValues = Enum.GetValues<EventCode>().Cast<int>().ToList();
        var enumNames = Enum.GetNames<EventCode>().ToList();

        // Act
        var duplicateValues = enumValues
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToList();

        // Assert
        if (duplicateValues.Count != 0)
        {
            var duplicatesInfo = string.Join(Environment.NewLine, duplicateValues.Select(d =>
            {
                var namesWithValue = enumNames
                    .Where(name => (int)Enum.Parse(enumType, name) == d.Value)
                    .ToList();

                return $"Value {d.Value.ToString(CultureInfo.InvariantCulture)} appears {d.Count.ToString(CultureInfo.InvariantCulture)} times in: {string.Join(", ", namesWithValue)}";
            }));

            Assert.Fail($"Found duplicate values in EventCode enum:{Environment.NewLine}{duplicatesInfo}");
        }

        Assert.That(duplicateValues, Is.Empty, "EventCode enum should not have duplicate values");
    }

    [Test]
    public void EventCode_ShouldHaveCorrectStartingValue()
    {
        // Arrange
        const int expectedStartingValue = Defaults.StartingEventCode;

        // Act
        var firstNonZeroValue = Enum.GetValues<EventCode>()
            .Cast<int>()
            .Where(v => v != 0) // Skip None = 0
            .Min();

        // Assert
        Assert.That(firstNonZeroValue, Is.EqualTo(expectedStartingValue + 1), "The first non-zero EventCode value should be StartingEventCode + 1");
    }

    [Test]
    public void EventCode_ShouldHaveConsecutiveValues()
    {
        // Arrange
        const int startingValue = Defaults.StartingEventCode;

        var nonZeroValues = Enum.GetValues<EventCode>()
            .Cast<int>()
            .Where(v => v != 0) // Skip None = 0
            .Order()
            .ToList();

        // Act
        var expectedValues = Enumerable.Range(1, nonZeroValues.Count)
            .Select(i => startingValue + i)
            .ToList();

        var missingOrExtraValues = nonZeroValues
            .Except(expectedValues)
            .Concat(expectedValues.Except(nonZeroValues))
            .ToList();

        // Assert
        if (missingOrExtraValues.Count != 0)
        {
            var missingValues = expectedValues.Except(nonZeroValues).ToList();
            var extraValues = nonZeroValues.Except(expectedValues).ToList();

            var message = string.Empty;

            if (missingValues.Count != 0)
            {
                message += $"Missing values: {string.Join(", ", missingValues)}{Environment.NewLine}";
            }

            if (extraValues.Count != 0)
            {
                message += $"Extra values: {string.Join(", ", extraValues)}{Environment.NewLine}";
            }

            Assert.Fail($"EventCode enum should have consecutive values:{Environment.NewLine}{message}");
        }

        Assert.That(missingOrExtraValues, Is.Empty, "EventCode enum should have consecutive values");
    }
}
