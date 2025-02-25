using DiplomaticMailBot.Common.Extensions;

namespace DiplomaticMailBot.Tests.Unit.Extensions;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class StringExtensionsTests
{
    [TestCase("", 0, "")]
    [TestCase("", 1, "")]
    [TestCase("a", 0, "")]
    [TestCase("a", 1, "a")]
    [TestCase("a", 2, "a")]
    [TestCase("a", 3, "a")]
    [TestCase("abc", 0, "")]
    [TestCase("abc", 1, "a")]
    [TestCase("abc", 2, "ab")]
    [TestCase("abc", 3, "abc")]
    [TestCase("abc", 4, "abc")]
    [TestCase("abc", 999, "abc")]
    public void TryLeft_WhenArgumentsValid_ShouldReturnExpectedResult(string source, int maxLength, string expectedResult)
    {
        var actualResult = source.TryLeft(maxLength);
        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }

    [TestCase("", -1)]
    [TestCase("a", -2)]
    public void TryLeft_WhenMaxLengthIsLessThanZero_ShouldThrowArgumentOutOfRangeException(string source, int maxLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => source.TryLeft(maxLength));
    }

    [TestCase("", 0, "")]
    [TestCase("", 1, "")]
    [TestCase("a", 0, "")]
    [TestCase("a", 1, "a")]
    [TestCase("a", 2, "a")]
    [TestCase("a", 3, "a")]
    [TestCase("abc", 0, "")]
    [TestCase("abc", 1, "c")]
    [TestCase("abc", 2, "bc")]
    [TestCase("abc", 3, "abc")]
    [TestCase("abc", 4, "abc")]
    [TestCase("abc", 999, "abc")]
    public void TryRight_WhenArgumentsValid_ShouldReturnExpectedResult(string source, int maxLength, string expectedResult)
    {
        var actualResult = source.TryRight(maxLength);
        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }

    [TestCase("", -1)]
    [TestCase("a", -2)]
    public void TryRight_WhenMaxLengthIsLessThanZero_ShouldThrowArgumentOutOfRangeException(string source, int maxLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => source.TryRight(maxLength));
    }
}
