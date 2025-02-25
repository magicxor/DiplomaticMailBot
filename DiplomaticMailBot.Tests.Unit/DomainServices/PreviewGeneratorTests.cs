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
