using DiplomaticMailBot.Common.Errors;
using DiplomaticMailBot.Common.Exceptions;
using LanguageExt;
using LanguageExt.Common;

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

    [Test]
    public void Append_WhenErrorIsManyExceptions_CallsAppend()
    {
        var oldException1 = new DomainException(1, "a");
        var oldException2 = new DomainException(2, "b");
        var oldExceptionList = default(Seq<ErrorException>)
            .Add(oldException1)
            .Add(oldException2);
        var oldExceptions = new ManyExceptions(oldExceptionList);

        var newException = new DomainException(101, "Domain error");

        var result = newException.Append(oldExceptions);

        Assert.That(result, Is.TypeOf<ManyExceptions>());

        if (result is ManyExceptions manyExceptions)
        {
            Assert.That(manyExceptions.Errors, Has.Count.EqualTo(3));
            Assert.That(manyExceptions.Errors, Contains.Item(newException));
            Assert.That(manyExceptions.Errors, Contains.Item(oldException1));
            Assert.That(manyExceptions.Errors, Contains.Item(oldException2));
        }
    }

    [Test]
    public void Append_WhenErrorIsDomainException_CallsAppend()
    {
        var oldException = new DomainException(1, "a");
        var newException = new DomainException(101, "Domain error");

        var result = newException.Append(oldException);

        Assert.That(result, Is.TypeOf<ManyExceptions>());

        if (result is ManyExceptions manyExceptions)
        {
            Assert.That(manyExceptions.Errors, Has.Count.EqualTo(2));
            Assert.That(manyExceptions.Errors, Contains.Item(newException));
            Assert.That(manyExceptions.Errors, Contains.Item(oldException));
        }
    }
}
