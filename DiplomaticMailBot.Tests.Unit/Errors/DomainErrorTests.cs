using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Exceptions;

namespace DiplomaticMailBot.Tests.Unit.Errors;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class DomainErrorTests
{
    [Test]
    public void Constructor_ShouldSetProperties()
    {
        var error = new DomainError(42, "Test message", true, false);

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo(42));
            Assert.That(error.Message, Is.EqualTo("Test message"));
            Assert.That(error.IsExceptional, Is.True);
            Assert.That(error.IsExpected, Is.False);
        });
    }

    [Test]
    public void Constructor_WithDefaults_ShouldSetDefaultValues()
    {
        var error = new DomainError(42, "Test message");

        Assert.Multiple(() =>
        {
            Assert.That(error.Code, Is.EqualTo(42));
            Assert.That(error.Message, Is.EqualTo("Test message"));
            Assert.That(error.IsExceptional, Is.False);
            Assert.That(error.IsExpected, Is.True);
        });
    }

    [Test]
    public void Is_WhenTypeDomainException_AndIsExceptionalTrue_ShouldReturnTrue()
    {
        var error = new DomainError(42, "Test message", true);
        Assert.That(error.Is<DomainException>(), Is.True);
    }

    [Test]
    public void Is_WhenTypeDomainException_AndIsExceptionalFalse_ShouldReturnFalse()
    {
        var error = new DomainError(42, "Test message", false);
        Assert.That(error.Is<DomainException>(), Is.False);
    }

    [Test]
    public void Is_WhenTypeNotDomainException_ShouldReturnFalse()
    {
        var error = new DomainError(42, "Test message", true);
        Assert.That(error.Is<ArgumentException>(), Is.False);
    }

    [Test]
    public void ToErrorException_ShouldReturnDomainException()
    {
        var error = new DomainError(42, "Test message", true, false);
        var exception = error.ToErrorException();

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.TypeOf<DomainException>());
            Assert.That(exception.Code, Is.EqualTo(42));
            Assert.That(exception.Message, Is.EqualTo("Test message"));
            Assert.That(exception.IsExceptional, Is.True);
            Assert.That(exception.IsExpected, Is.False);
        });
    }
}
