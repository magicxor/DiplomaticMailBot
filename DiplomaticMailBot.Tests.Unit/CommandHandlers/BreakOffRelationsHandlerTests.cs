using Moq;
using DiplomaticMailBot.Services.CommandHandlers;
using DiplomaticMailBot.Infra.Repositories.Contracts;
using Telegram.Bot.Types;
using Telegram.Bot;
using DiplomaticMailBot.Domain.Contracts;
using DiplomaticMailBot.Infra.ServiceModels.DiplomaticRelation;
using DiplomaticMailBot.Infra.Telegram.Contracts;

namespace DiplomaticMailBot.Tests.Unit.CommandHandlers;

[TestFixture]
public class BreakOffRelationsHandlerTests
{
    private Mock<ITelegramBotClient> _telegramBotClientMock;
    private Mock<ITelegramInfoService> _telegramInfoServiceMock;
    private Mock<IDiplomaticRelationRepository> _diplomaticRelationRepositoryMock;
    private Mock<IPreviewGenerator> _previewGeneratorMock;
    private BreakOffRelationsHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _telegramBotClientMock = new Mock<ITelegramBotClient>();
        _telegramInfoServiceMock = new Mock<ITelegramInfoService>();
        _diplomaticRelationRepositoryMock = new Mock<IDiplomaticRelationRepository>();
        _previewGeneratorMock = new Mock<IPreviewGenerator>();

        _handler = new BreakOffRelationsHandler(
            _telegramBotClientMock.Object,
            _telegramInfoServiceMock.Object,
            _diplomaticRelationRepositoryMock.Object,
            _previewGeneratorMock.Object);
    }

    [Test]
    public void HandleBreakOffRelationsAsync_WhenBotIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleBreakOffRelationsAsync(null!, new Message()));
    }

    [Test]
    public void HandleBreakOffRelationsAsync_WhenUserCommandIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleBreakOffRelationsAsync(new User(), null!));
    }

    [Test]
    public async Task HandleBreakOffRelationsAsync_WhenCommandNotSentByAdmin_ShouldNotProceed()
    {
        _telegramInfoServiceMock.Setup(s => s.IsSentByChatAdminAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.HandleBreakOffRelationsAsync(new User(), new Message { Chat = new Chat { Id = 123 }, Text = "/breakoff test" });

        _diplomaticRelationRepositoryMock.Verify(r => r.BreakOffRelationsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleBreakOffRelationsAsync_WhenCommandSentByAdmin_ShouldProceed()
    {
        _telegramInfoServiceMock.Setup(s => s.IsSentByChatAdminAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _diplomaticRelationRepositoryMock.Setup(r => r.BreakOffRelationsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiplomaticRelationsInfoSm
            {
                IsOutgoingRelationPresent = false,
                IsIncomingRelationPresent = false,
                SourceChatId = 0,
                SourceChatAlias = string.Empty,
                SourceChatTitle = string.Empty,
                TargetChatId = 0,
                TargetChatAlias = string.Empty,
                TargetChatTitle = string.Empty,
            });

        await _handler.HandleBreakOffRelationsAsync(new User(), new Message { Chat = new Chat { Id = 123 }, Text = "/breakoff test" });

        _diplomaticRelationRepositoryMock.Verify(r => r.BreakOffRelationsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
