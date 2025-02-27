using DiplomaticMailBot.Domain;
using Telegram.Bot.Types;

namespace DiplomaticMailBot.Tests.Unit.DomainServices;

[TestFixture]
public sealed class PreviewGeneratorTests
{
    private PreviewGenerator _previewGenerator;

    [SetUp]
    public void Setup()
    {
        _previewGenerator = new PreviewGenerator();
    }

    [Test]
    public void GetMessagePreview_WithTextMessage_ReturnsText()
    {
        // Arrange
        var message = CreateMessage(text: "Hello, world!");

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Hello, world!"));
    }

    [Test]
    public void GetMessagePreview_WithPhoto_ReturnsPhotoPreview()
    {
        // Arrange
        var message = new Message
        {
            Photo = [new PhotoSize()],
            Caption = "Beautiful sunset",
        };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("(Фото) Beautiful sunset"));
    }

    [Test]
    public void GetMessagePreview_WithPhotoNoCaption_ReturnsPhotoTypeOnly()
    {
        // Arrange
        var message = new Message
        {
            Photo = [new PhotoSize()],
        };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Фото"));
    }

    [Test]
    public void GetMessagePreview_WithUnknownType_ReturnsDefaultMessage()
    {
        // Arrange
        var message = new Message();

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Не удалось определить тип сообщения"));
    }

    [Test]
    public void GetMessageTypeAndCaption_WithCaption_ReturnsCombinedString()
    {
        // Act
        var result = _previewGenerator.GetMessageTypeAndCaption("Фото", "Beautiful sunset");

        // Assert
        Assert.That(result, Is.EqualTo("(Фото) Beautiful sunset"));
    }

    [Test]
    public void GetMessageTypeAndCaption_WithoutCaption_ReturnsTypeOnly()
    {
        // Act
        var result = _previewGenerator.GetMessageTypeAndCaption("Фото", null);

        // Assert
        Assert.That(result, Is.EqualTo("Фото"));
    }

    [Test]
    public void GetAuthorName_WithUsername_ReturnsUsername()
    {
        // Arrange
        var message = CreateMessage(username: "testuser");

        // Act
        var result = _previewGenerator.GetAuthorName(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("testuser"));
    }

    [Test]
    public void GetAuthorName_WithoutUsername_ReturnsFullName()
    {
        // Arrange
        var message = CreateMessage(firstName: "John", lastName: "Doe");

        // Act
        var result = _previewGenerator.GetAuthorName(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void GetChatDisplayString_WithValidInput_ReturnsFormattedString()
    {
        // Act
        var result = _previewGenerator.GetChatDisplayString("general", "General Chat");

        // Assert
        Assert.That(result, Is.EqualTo("general (General Chat)"));
    }

    [Test]
    public void GetMessagePreview_WithAnimation_ReturnsAnimationPreview()
    {
        // Arrange
        var message = new Message { Animation = new Animation(), Caption = "Funny GIF" };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("(Анимация) Funny GIF"));
    }

    [Test]
    public void GetMessagePreview_WithAudio_ReturnsAudioPreview()
    {
        // Arrange
        var message = new Message { Audio = new Audio(), Caption = "Nice song" };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("(Аудио) Nice song"));
    }

    [Test]
    public void GetMessagePreview_WithDocument_ReturnsDocumentPreview()
    {
        // Arrange
        var message = new Message { Document = new Document(), Caption = "Important file" };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("(Документ) Important file"));
    }

    [Test]
    public void GetMessagePreview_WithSticker_ReturnsStickerPreview()
    {
        // Arrange
        var message = new Message { Sticker = new Sticker() };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Стикер"));
    }

    [Test]
    public void GetMessagePreview_WithVideo_ReturnsVideoPreview()
    {
        // Arrange
        var message = new Message { Video = new Video(), Caption = "Cool video" };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("(Видео) Cool video"));
    }

    [Test]
    public void GetMessagePreview_WithVideoNote_ReturnsVideoNotePreview()
    {
        // Arrange
        var message = new Message { VideoNote = new VideoNote() };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Видеосообщение"));
    }

    [Test]
    public void GetMessagePreview_WithVoice_ReturnsVoicePreview()
    {
        // Arrange
        var message = new Message { Voice = new Voice() };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Голосовое сообщение"));
    }

    [Test]
    public void GetMessagePreview_WithLocation_ReturnsLocationPreview()
    {
        // Arrange
        var message = new Message { Location = new Location() };

        // Act
        var result = _previewGenerator.GetMessagePreview(message, 50);

        // Assert
        Assert.That(result, Is.EqualTo("Местоположение"));
    }

    [Test]
    public void GetPollOptionPreview_WithValidInput_ReturnsFormattedString()
    {
        // Act
        var result = _previewGenerator.GetPollOptionPreview(123, "user", "test message", 10, 50);

        // Assert
        Assert.That(result, Is.EqualTo("[123] (user): test message"));
    }

    [Test]
    public void GetPollOptionPreview_WithLongAuthorName_TruncatesAuthorName()
    {
        // Act
        var result = _previewGenerator.GetPollOptionPreview(123, "verylongusername", "test message", 5, 50);

        // Assert
        Assert.That(result, Is.EqualTo("[123] (veryl): test message"));
    }

    [Test]
    public void GetPollOptionPreview_WithTotalLengthLimit_TruncatesEntireString()
    {
        // Act
        var result = _previewGenerator.GetPollOptionPreview(123, "user", "very long test message that should be truncated", 10, 20);

        // Assert
        Assert.That(result, Is.EqualTo("[123] (user): very l"));
    }

    [Test]
    public void GetMessageLinkUrl_WithValidInput_ReturnsFormattedUrl()
    {
        // Act
        var result = _previewGenerator.GetMessageLinkUrl(-1001234567890, 123);

        // Assert
        Assert.That(result, Is.EqualTo("https://t.me/c/1234567890/123"));
    }

    [Test]
    public void GetMessageLinkMarkdown_WithMessageIdOnly_ReturnsFormattedMarkdown()
    {
        // Act
        var result = _previewGenerator.GetMessageLinkMarkdown(-1001234567890, 123);

        // Assert
        Assert.That(result, Is.EqualTo("[123](https://t.me/c/1234567890/123)"));
    }

    [Test]
    public void GetMessageLinkMarkdown_WithCustomCaption_ReturnsFormattedMarkdown()
    {
        // Act
        var result = _previewGenerator.GetMessageLinkMarkdown(-1001234567890, 123, "Click here");

        // Assert
        Assert.That(result, Is.EqualTo("[Click here](https://t.me/c/1234567890/123)"));
    }

    [Test]
    public void GetMessageLinkHtml_WithMessageIdOnly_ReturnsFormattedHtml()
    {
        // Act
        var result = _previewGenerator.GetMessageLinkHtml(-1001234567890, 123);

        // Assert
        Assert.That(result, Is.EqualTo("""<a href="https://t.me/c/1234567890/123">123</a>"""));
    }

    [Test]
    public void GetMessageLinkHtml_WithCustomCaption_ReturnsFormattedHtml()
    {
        // Act
        var result = _previewGenerator.GetMessageLinkHtml(-1001234567890, 123, "Click here");

        // Assert
        Assert.That(result, Is.EqualTo("""<a href="https://t.me/c/1234567890/123">Click here</a>"""));
    }

    private static Message CreateMessage(
        string? username = null,
        string? text = null,
        string? firstName = null,
        string? lastName = null)
    {
        var message = new Message { Text = text };

        if (username != null || firstName != null || lastName != null)
        {
            message.From = new User
            {
                Username = username,
                FirstName = firstName ?? string.Empty,
                LastName = lastName,
            };
        }

        return message;
    }
}
