using DiplomaticMailBot.Common.Extensions;

namespace DiplomaticMailBot.Tests.Unit.Extensions;

[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
public sealed class StringExtensionsTests
{
    [Test]
    public void TryLeft_WhenNull_ShouldReturnNull()
    {
        const string? source = null;
        var result = source.TryLeft(1);
        Assert.That(result, Is.Null);
    }

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
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = source.TryLeft(maxLength));
    }

    [Test]
    public void TryRight_WhenNull_ShouldReturnNull()
    {
        const string? source = null;
        var result = source.TryRight(1);
        Assert.That(result, Is.Null);
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
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = source.TryRight(maxLength));
    }

    [TestCase(null, null, true)]
    [TestCase(null, "", false)]
    [TestCase("", null, false)]
    [TestCase("", "", true)]
    [TestCase("test", "TEST", true)]
    [TestCase("test", "test", true)]
    [TestCase("test", "other", false)]
    public void EqualsIgnoreCase_ShouldReturnExpectedResult(string? source, string? target, bool expectedResult)
    {
        var actualResult = source.EqualsIgnoreCase(target);
        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }

    [Test]
    public void GetNonEmpty_WhenAllValuesEmpty_ShouldReturnEmptyCollection()
    {
        var result = StringExtensions.FilterNonEmpty(null, string.Empty, " ", "\t", "\n");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetNonEmpty_WhenSomeValuesNonEmpty_ShouldReturnNonEmptyValues()
    {
        var result = StringExtensions.FilterNonEmpty(null, "test1", string.Empty, " ", "test2", "\t", "\n");
        Assert.That(result, Is.EqualTo(new[] { "test1", "test2" }));
    }

    [Test]
    public void IsNotNullOrEmpty_WhenNull_ShouldReturnFalse()
    {
        const string? value = null;
        Assert.That(value.IsNotNullOrEmpty(), Is.False);
    }

    [Test]
    public void IsNotNullOrEmpty_WhenEmpty_ShouldReturnFalse()
    {
        Assert.That(string.Empty.IsNotNullOrEmpty(), Is.False);
    }

    [Test]
    public void IsNotNullOrEmpty_WhenNonEmpty_ShouldReturnTrue()
    {
        Assert.That("test".IsNotNullOrEmpty(), Is.True);
    }

    [Test]
    public void CutToLastClosingLinkTag_WhenNull_ShouldReturnNull()
    {
        const string? value = null;
        var result = value!.CutToLastClosingLinkTag();
        Assert.That(result, Is.EqualTo(null));
    }

    [Test]
    public void CutToLastClosingLinkTag_WhenEmpty_ShouldReturnEmpty()
    {
        var result = string.Empty.CutToLastClosingLinkTag();
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void CutToLastClosingLinkTag_WhenNoClosingTag_ShouldReturnEmpty()
    {
        var result = "Hello World".CutToLastClosingLinkTag();
        Assert.That(result, Is.EqualTo("Hello World"));
    }

    [Test]
    public void CutToLastClosingLinkTag_WhenHasClosingTag_ShouldCutToLastTag()
    {
        const string input = "Hello <a>World</a> extra text";
        const string expected = "Hello <a>World</a>";
        var result = input.CutToLastClosingLinkTag();
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CutToLastClosingLinkTag_WhenMultipleTags_ShouldCutToLastTag()
    {
        const string input = "<a>First</a> middle <a>Last</a> extra";
        const string expected = "<a>First</a> middle <a>Last</a>";
        var result = input.CutToLastClosingLinkTag();
        Assert.That(result, Is.EqualTo(expected));
    }
}
