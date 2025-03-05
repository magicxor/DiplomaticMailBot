using Moq;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using Telegram.Bot.Types;
using Microsoft.Extensions.Logging;
using DiplomaticMailBot.Infra.Telegram.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.RegisteredChat;
using DiplomaticMailBot.Tests.Unit.Mocks;

namespace DiplomaticMailBot.Tests.Unit.CommandHandlers;

[TestFixture]
public class RegisterChatHandlerTests
{
    private Mock<ILogger<RegisterChatHandler>> _loggerMock;
    private MockTelegramBotClient _telegramBotClientMock;
    private Mock<ITelegramInfoService> _telegramInfoServiceMock;
    private Mock<IRegisteredChatRepository> _registeredChatRepositoryMock;
    private Mock<IPreviewGenerator> _previewGeneratorMock;
    private RegisterChatHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RegisterChatHandler>>();
        _telegramBotClientMock = new MockTelegramBotClient();
        _telegramInfoServiceMock = new Mock<ITelegramInfoService>();
        _registeredChatRepositoryMock = new Mock<IRegisteredChatRepository>();
        _previewGeneratorMock = new Mock<IPreviewGenerator>();

        _handler = new RegisterChatHandler(
            _loggerMock.Object,
            _telegramBotClientMock,
            _telegramInfoServiceMock.Object,
            _registeredChatRepositoryMock.Object,
            _previewGeneratorMock.Object);
    }

    [Test]
    public void HandleRegisterChatAsync_WhenBotIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleRegisterChatAsync(null!, new Message()));
    }

    [Test]
    public void HandleRegisterChatAsync_WhenUserCommandIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleRegisterChatAsync(new User(), null!));
    }

    [Test]
    public async Task HandleRegisterChatAsync_WhenCommandSentByAdmin_ShouldProceed()
    {
        _telegramInfoServiceMock.Setup(s => s.IsSentByChatAdminAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _registeredChatRepositoryMock.Setup(r => r.CreateOrUpdateAsync(It.IsAny<RegisteredChatCreateOrUpdateRequestSm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisteredChatCreateOrUpdateResultSm
            {
                IsCreated = true,
                ChatAlias = "testAlias",
                IsUpdated = false,
                ChatId = 123,
                ChatTitle = "test",
            });

        await _handler.HandleRegisterChatAsync(new User(), new Message { Chat = new Chat { Id = 123, Title = "Test Chat" }, Text = "/register test" });

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleListChatsAsync_ShouldSendRegisteredChatsList()
    {
        _registeredChatRepositoryMock.Setup(r => r.ListRegisteredChatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new RegisteredChatSm
                {
                    ChatAlias = "testAlias",
                    ChatTitle = "Test Chat",
                    Id = 123,
                    ChatId = 456,
                    CreatedAt = null,
                },
            ]);

        await _handler.HandleListChatsAsync(new User(), new Message { Chat = new Chat { Id = 123 } });

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }
}
