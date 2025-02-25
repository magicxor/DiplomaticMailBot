using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Exceptions;

namespace DiplomaticMailBot.Tests.Unit.Exceptions;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class DomainExceptionTests
{
    [Test]
    public void Constructor_ShouldSetProperties()
    {
        var innerException = new DomainException(1, "Inner");
        var exception = new DomainException(42, "Test message", true, false, innerException);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Code, Is.EqualTo(42));
            Assert.That(exception.Message, Is.EqualTo("Test message"));
            Assert.That(exception.IsExceptional, Is.True);
            Assert.That(exception.IsExpected, Is.False);
            Assert.That(exception.Inner.IsSome, Is.True);
        });
    }

    [Test]
    public void Constructor_WithDefaults_ShouldSetDefaultValues()
    {
        var exception = new DomainException(42, "Test message");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Code, Is.EqualTo(42));
            Assert.That(exception.Message, Is.EqualTo("Test message"));
            Assert.That(exception.IsExceptional, Is.False);
            Assert.That(exception.IsExpected, Is.True);
            Assert.That(exception.Inner.IsNone, Is.True);
        });
    }

    [Test]
    public void ToError_ShouldReturnDomainError()
    {
        var exception = new DomainException(42, "Test message", true, false);
        var error = exception.ToError();

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.TypeOf<DomainError>());
            Assert.That(error.Code, Is.EqualTo(42));
            Assert.That(error.Message, Is.EqualTo("Test message"));
            Assert.That(error.IsExceptional, Is.True);
            Assert.That(error.IsExpected, Is.False);
        });
    }
}
