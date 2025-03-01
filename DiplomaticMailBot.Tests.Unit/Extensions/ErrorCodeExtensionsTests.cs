using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Common.Enums;
using Microsoft.Extensions.Logging;

namespace DiplomaticMailBot.Tests.Unit;

[TestFixture]
public class ErrorCodeExtensionsTests
{
    [Test]
    public void ToInt_GivenValidEventCode_ReturnsCorrectIntegerValue()
    {
        // Arrange
        // Using a cast value for EventCode since we don't know the actual enum constants.
        // You can replace these with actual named constants if available.
        EventCode code = (EventCode)1;
        int expected = 1;

        // Act
        int actual = code.ToInt();

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ToEventId_GivenValidEventCode_ReturnsEventIdWithCorrectId()
    {
        // Arrange
        EventCode code = (EventCode)2;
        int expected = 2;

        // Act
        EventId eventId = code.ToEventId();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(eventId.Id, Is.EqualTo(expected));

            // The Name property is expected to be null unless set otherwise
            Assert.That(eventId.Name, Is.Null);
        });
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(100)]
    public void ExtensionMethods_WorkForVariousIntegerValues(int intValue)
    {
        // Arrange
        EventCode code = (EventCode)intValue;

        // Act & Assert
        // Test ToInt extension
        Assert.That(code.ToInt(), Is.EqualTo(intValue));

        // Test ToEventId extension
        EventId eventId = code.ToEventId();
        Assert.Multiple(() =>
        {
            Assert.That(eventId.Id, Is.EqualTo(intValue));
            Assert.That(eventId.Name, Is.Null);
        });
    }
}
