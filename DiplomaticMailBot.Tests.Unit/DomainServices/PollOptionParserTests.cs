using DiplomaticMailBot.Common.Enums;
using DiplomaticMailBot.Common.Extensions;
using DiplomaticMailBot.Domain;

namespace DiplomaticMailBot.Tests.Unit.DomainServices;

[TestFixture]
public sealed class PollOptionParserTests
{
    private PollOptionParser _parser;

    [SetUp]
    public void Setup()
    {
        _parser = new PollOptionParser();
    }

    [Test]
    public void GetMessageId_WithValidInput_ReturnsMessageId()
    {
        // Arrange
        var pollOptionText = "[123] Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(123));
    }

    [Test]
    public void GetMessageId_WithEmptyInput_ReturnsError()
    {
        // Arrange
        var pollOptionText = string.Empty;

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.MessageIdNotFound.ToInt()));
        });
    }

    [Test]
    public void GetMessageId_WithoutOpeningBracket_ReturnsError()
    {
        // Arrange
        var pollOptionText = "123] Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.OpeningBracketNotFound.ToInt()));
        });
    }

    [Test]
    public void GetMessageId_WithoutClosingBracket_ReturnsError()
    {
        // Arrange
        var pollOptionText = "[123 Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.ClosingBracketNotFound.ToInt()));
        });
    }

    [Test]
    public void GetMessageId_WithEmptyBrackets_ReturnsError()
    {
        // Arrange
        var pollOptionText = "[] Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.MessageIdNotFound.ToInt()));
        });
    }

    [Test]
    public void GetMessageId_WithSpaceInBrackets_ReturnsError()
    {
        // Arrange
        var pollOptionText = "[ ] Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.MessageIdNotFound.ToInt()));
        });
    }

    [Test]
    public void GetMessageId_WithNonNumericContent_ReturnsError()
    {
        // Arrange
        var pollOptionText = "[abc] Test message";

        // Act
        var result = _parser.GetMessageId(pollOptionText);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Match(err => err.Code, success => success), Is.EqualTo(EventCode.MessageIdNotFound.ToInt()));
        });
    }
}
