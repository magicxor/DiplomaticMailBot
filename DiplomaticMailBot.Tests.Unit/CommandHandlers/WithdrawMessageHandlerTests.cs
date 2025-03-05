using Moq;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using Telegram.Bot.Types;
using DiplomaticMailBot.Tests.Unit.Mocks;

namespace DiplomaticMailBot.Tests.Unit.CommandHandlers;

[TestFixture]
public class WithdrawMessageHandlerTests
{
    private MockTelegramBotClient _telegramBotClientMock;
    private Mock<IMessageCandidateRepository> _messageCandidateRepositoryMock;
    private WithdrawMessageHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _telegramBotClientMock = new MockTelegramBotClient();
        _messageCandidateRepositoryMock = new Mock<IMessageCandidateRepository>();

        _handler = new WithdrawMessageHandler(
            _telegramBotClientMock,
            _messageCandidateRepositoryMock.Object);
    }

    [Test]
    public void HandleWithdrawMessageAsync_WhenBotIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleWithdrawMessageAsync(null!, new Message()));
    }

    [Test]
    public void HandleWithdrawMessageAsync_WhenUserCommandIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleWithdrawMessageAsync(new User(), null!));
    }

    [Test]
    public async Task HandleWithdrawMessageAsync_WhenNotReplyToMessage_ShouldSendErrorMessage()
    {
        var userCommand = new Message { Chat = new Chat { Id = 123 }, Text = "/withdraw" };

        await _handler.HandleWithdrawMessageAsync(new User(), userCommand);

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleWithdrawMessageAsync_WhenReplyToMessage_ShouldProceed()
    {
        var userCommand = new Message { Chat = new Chat { Id = 123 }, Text = "/withdraw", ReplyToMessage = new Message { Id = 456 } };

        _messageCandidateRepositoryMock.Setup(repo => repo.WithdrawAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _handler.HandleWithdrawMessageAsync(new User(), userCommand);

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }
}
