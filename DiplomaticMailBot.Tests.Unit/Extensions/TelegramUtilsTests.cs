using DiplomaticMailBot.Domain.Implementations;
using Telegram.Bot.Types.Enums;

namespace DiplomaticMailBot.Tests.Unit.Extensions;

[TestFixture]
public class TelegramUtilsTests
{
    [TestCase("*bold*", @"\*bold\*")]
    [TestCase("_italic_", @"\_italic\_")]
    [TestCase("`code`", @"\`code\`")]
    [TestCase("[text_link](https://github.com/)", @"\[text\_link](https://github.com/)")]
    public void Test_escape_markdown(string testStr, string expected)
    {
        Assert.That(testStr.EscapeMarkdown(), Is.EqualTo(expected));
    }

    [TestCase(@"a_b*c[d]e", @"a\_b\*c\[d\]e")]
    [TestCase(@"(fg) ", @"\(fg\) ")]
    [TestCase(@"h~I`>JK#L+MN", @"h\~I\`\>JK\#L\+MN")]
    [TestCase(@"-O=|p{qr}s.t!\ ", @"\-O\=\|p\{qr\}s\.t\!\\ ")]
    [TestCase(@"\u", @"\\u")]
    public void Test_escape_markdown_v2(string testStr, string expected)
    {
        Assert.That(testStr.EscapeMarkdown(parseMode: ParseMode.MarkdownV2), Is.EqualTo(expected));
    }

    [TestCase(@"mono/pre:", "mono/pre:")]
    [TestCase(@"`abc`", @"\`abc\`")]
    [TestCase(@"\int", @"\\int")]
    [TestCase(@"(`\some \` stuff)", @"(\`\\some \\\` stuff)")]
    public void Test_escape_markdown_v2_monospaced(string testStr, string expected)
    {
        var escaped = testStr.EscapeMarkdown(parseMode: ParseMode.MarkdownV2,
            entityType: MessageEntityType.Pre);

        Assert.That(escaped, Is.EqualTo(expected));

        escaped = testStr.EscapeMarkdown(parseMode: ParseMode.MarkdownV2, entityType: MessageEntityType.Code);

        Assert.That(escaped, Is.EqualTo(expected));
    }

    [Test]
    public void Test_escape_markdown_v2_text_link()
    {
        const string test = """https://url.containing/funny)cha)\ra\)cter\s""";
        const string expected = """https://url.containing/funny\)cha\)\\ra\\\)cter\\s""";

        var escaped = test.EscapeMarkdown(parseMode: ParseMode.MarkdownV2, entityType: MessageEntityType.TextLink);

        Assert.That(escaped, Is.EqualTo(expected));
    }

    [Test]
    public void Test_markdown_invalid_version()
    {
        Assert.Throws<ArgumentException>(
            () => "abc".EscapeMarkdown(parseMode: ParseMode.Html));

        Assert.Throws<ArgumentException>(
            () => TelegramUtils.MentionMarkdown(1, "abc", parseMode: ParseMode.Html));
    }

    [Test]
    public void Test_create_deep_linked_url()
    {
        const string username = "JamesTheMock";
        var payload = "hello";

        var expected = $"https://t.me/{username}?start={payload}";
        var actual = TelegramUtils.CreateDeepLinkedUrl(username, payload);
        Assert.That(actual, Is.EqualTo(expected));

        expected = $"https://t.me/{username}?startgroup={payload}";
        actual = TelegramUtils.CreateDeepLinkedUrl(username, payload, group: true);
        Assert.That(actual, Is.EqualTo(expected));

        payload = string.Empty;
        expected = $"https://t.me/{username}";
        Assert.That(TelegramUtils.CreateDeepLinkedUrl(username), Is.EqualTo(expected));
        Assert.That(TelegramUtils.CreateDeepLinkedUrl(username, payload), Is.EqualTo(expected));

        payload = null!;
        expected = $"https://t.me/{username}";
        Assert.That(TelegramUtils.CreateDeepLinkedUrl(username, payload), Is.EqualTo(expected));

        Assert.Throws<ArgumentException>(()
            => TelegramUtils.CreateDeepLinkedUrl(username, "text with spaces"));

        Assert.Throws<ArgumentException>(()
            => TelegramUtils.CreateDeepLinkedUrl(username, new string('0', 65)));

        Assert.Throws<ArgumentException>(()
            => TelegramUtils.CreateDeepLinkedUrl(null!, payload: null));

        Assert.Throws<ArgumentException>(()
            => TelegramUtils.CreateDeepLinkedUrl("abc", payload: null));
    }

    [Test]
    public void Test_mention_html()
    {
        const string expected = """<a href="tg://user?id=1">the name</a>""";

        var mention = TelegramUtils.MentionHtml(1, "the name");

        Assert.That(mention, Is.EqualTo(expected));
    }

    [TestCase(@"the name", @"[the name](tg://user?id=1)")]
    [TestCase(@"under_score", @"[under_score](tg://user?id=1)")]
    [TestCase(@"starred*text", @"[starred*text](tg://user?id=1)")]
    [TestCase(@"`backtick`", @"[`backtick`](tg://user?id=1)")]
    [TestCase(@"[square brackets", @"[[square brackets](tg://user?id=1)")]
    public void Test_mention_markdown(string testStr, string expected)
    {
        var mention = TelegramUtils.MentionMarkdown(1, testStr);

        Assert.That(mention, Is.EqualTo(expected));
    }

    [Test]
    public void Test_mention_markdown_2()
    {
        const string expected = """[the\_name](tg://user?id=1)""";

        var mention = TelegramUtils.MentionMarkdown(1, @"the_name", parseMode: ParseMode.MarkdownV2);

        Assert.That(mention, Is.EqualTo(expected));
    }

    [Test]
    public void EscapeSpecialTelegramMdCharacters_WhenNull_ShouldThrowArgumentNullException()
    {
        const string? value = null;
        Assert.Throws<ArgumentNullException>(() => _ = value!.EscapeMarkdown(ParseMode.MarkdownV2));
    }

    [Test]
    public void EscapeSpecialTelegramMdCharacters_WhenContainsSpecialCharacters_ShouldEscapeThem()
    {
        const string input = "Hello_World*[Test]~`>#+-=|{}.!";
        const string expected = @"Hello\_World\*\[Test\]\~\`\>\#\+\-\=\|\{\}\.\!";
        var result = input.EscapeMarkdown(ParseMode.MarkdownV2);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void EscapeSpecialTelegramHtmlCharacters_WhenNull_ShouldThrowArgumentNullException()
    {
        const string? value = null;
        Assert.Throws<ArgumentNullException>(() => _ = value!.EscapeHtml());
    }

    [Test]
    public void EscapeSpecialTelegramHtmlCharacters_WhenContainsSpecialCharacters_ShouldEscapeThem()
    {
        const string input = "Hello<World>&Test";
        const string expected = "Hello&lt;World&gt;&amp;Test";
        var result = input.EscapeHtml();
        Assert.That(result, Is.EqualTo(expected));
    }
}
