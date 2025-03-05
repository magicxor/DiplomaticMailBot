using DiplomaticMailBot.Infra.Telegram.Implementations.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Logging;
using DiplomaticMailBot.Tests.Unit.Mocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiplomaticMailBot.Tests.Unit.Services;

[TestFixture]
public class TelegramInfoServiceTests
{
    private ILogger<TelegramInfoService> _loggerMock;
    private MockTelegramBotClient _telegramBotClientMock;
    private TelegramInfoService _telegramInfoService;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = NullLoggerFactory.Instance.CreateLogger<TelegramInfoService>();
        _telegramBotClientMock = new MockTelegramBotClient();

        _telegramInfoService = new TelegramInfoService(
            _loggerMock,
            _telegramBotClientMock);
    }

    [Test]
    public void IsSentByChatAdminAsync_WhenMessageIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _telegramInfoService.IsSentByChatAdminAsync(null!));
    }

    [Test]
    public async Task IsSentByChatAdminAsync_WhenCalled_ShouldReturnTrueIfAdmin()
    {
        var message = new Message { Chat = new Chat { Id = 123 }, From = new User { Id = 456 } };

        _telegramBotClientMock.Options.ChatMemberStatus = ChatMemberStatus.Administrator;

        var result = await _telegramInfoService.IsSentByChatAdminAsync(message);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsSentByChatAdminAsync_WhenCalled_ShouldReturnFalseIfNotAdmin()
    {
        var message = new Message { Chat = new Chat { Id = 123 }, From = new User { Id = 456 } };

        _telegramBotClientMock.Options.ChatMemberStatus = ChatMemberStatus.Member;

        var result = await _telegramInfoService.IsSentByChatAdminAsync(message);

        Assert.That(result, Is.False);
    }
}
