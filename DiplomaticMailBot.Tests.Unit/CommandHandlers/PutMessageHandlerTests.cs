using Moq;
using DiplomaticMailBot.Common.Configuration;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using Telegram.Bot.Types;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.MessageCandidate;
using DiplomaticMailBot.Infra.ServiceModels.SlotTemplate;
using DiplomaticMailBot.Tests.Common;
using DiplomaticMailBot.Tests.Unit.Mocks;
using Microsoft.Extensions.Options;

namespace DiplomaticMailBot.Tests.Unit.CommandHandlers;

[TestFixture]
public class PutMessageHandlerTests
{
    private IOptions<BotConfiguration> _optionsMock;
    private Mock<TimeProvider> _timeProviderMock;
    private MockTelegramBotClient _telegramBotClientMock;
    private Mock<IMessageCandidateRepository> _messageCandidateRepositoryMock;
    private Mock<IPreviewGenerator> _previewGeneratorMock;
    private Mock<IRegisteredChatRepository> _registeredChatRepositoryMock;
    private PutMessageHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _optionsMock = Options.Create(new BotConfiguration
        {
            TelegramBotApiKey = "abc:def",
            Culture = "en-US",
        });

        _timeProviderMock = new Mock<TimeProvider>();
        _telegramBotClientMock = new MockTelegramBotClient();

        _messageCandidateRepositoryMock = new Mock<IMessageCandidateRepository>();
        _previewGeneratorMock = new Mock<IPreviewGenerator>();
        _registeredChatRepositoryMock = new Mock<IRegisteredChatRepository>();

        _handler = new PutMessageHandler(
            _optionsMock,
            _timeProviderMock.Object,
            _telegramBotClientMock,
            _messageCandidateRepositoryMock.Object,
            _previewGeneratorMock.Object,
            _registeredChatRepositoryMock.Object);
    }

    [Test]
    public void HandlePutMessageAsync_WhenBotIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandlePutMessageAsync(null!, new Message()));
    }

    [Test]
    public void HandlePutMessageAsync_WhenUserCommandIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandlePutMessageAsync(new User(), null!));
    }

    [Test]
    public async Task HandlePutMessageAsync_WhenNotReplyToMessage_ShouldSendErrorMessage()
    {
        var userCommand = new Message { Chat = new Chat { Id = 123 }, Text = "/put test" };

        await _handler.HandlePutMessageAsync(new User(), userCommand);

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandlePutMessageAsync_WhenReplyToMessage_ShouldProceed()
    {
        var currentTime = new DateTime(2025, 02, 23, 10, 00, 0, DateTimeKind.Utc);
        var timeProvider = FakeTimeProviderFactory.Create(currentTime);

        var userCommand = new Message { Chat = new Chat { Id = 123 }, Text = "/put test", ReplyToMessage = new Message { Id = 456 } };

        _registeredChatRepositoryMock.Setup(repo => repo.GetChatSlotTemplateByTelegramChatIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SlotTemplateSm
            {
                Id = 1,
                VoteStartAt = TimeOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddHours(-1),
                VoteEndAt = TimeOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddHours(1),
                Number = 0,
            });

        _messageCandidateRepositoryMock.Setup(repo => repo.PutAsync(It.IsAny<MessageCandidatePutSm>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.HandlePutMessageAsync(new User(), userCommand);

        _messageCandidateRepositoryMock.Verify(repo => repo.PutAsync(It.IsAny<MessageCandidatePutSm>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(_telegramBotClientMock.SendMessageCallCount, Is.EqualTo(1));
    }
}
