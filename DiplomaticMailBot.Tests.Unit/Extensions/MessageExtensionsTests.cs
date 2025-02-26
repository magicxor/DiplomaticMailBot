using Telegram.Bot.Types;
using DiplomaticMailBot.TelegramInterop.Extensions;

namespace DiplomaticMailBot.Tests.Unit.Extensions;

[TestFixture]
public sealed class MessageExtensionsTests
{
    [Test]
    public void ToReplyParameters_WithValidMessage_ReturnsCorrectParameters()
    {
        // Arrange
        var message = new Message
        {
            Id = 123,
            Chat = new Chat
            {
                Id = 456,
            },
        };

        // Act
        var result = message.ToReplyParameters();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.MessageId, Is.EqualTo(123));
            Assert.That(result.ChatId?.Identifier, Is.EqualTo(456));
            Assert.That(result.AllowSendingWithoutReply, Is.True);
        });
    }

    [Test]
    public void ToReplyParameters_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        Message message = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.ToReplyParameters());
    }
}
