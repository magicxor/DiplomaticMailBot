using System.Diagnostics.CodeAnalysis;

namespace DiplomaticMailBot.Common.Extensions;

/// <summary>
/// Extension methods for string.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if the string is null or empty.
    /// </summary>
    /// <param name="src">source string</param>
    /// <returns>True if the string is null or empty, false otherwise.</returns>
    public static bool IsNotNullOrEmpty(this string? src)
    {
        return !string.IsNullOrEmpty(src);
    }

    /// <summary>
    /// Returns the leftmost maxLength characters from the string.
    /// </summary>
    /// <param name="src">source string</param>
    /// <param name="maxLength">maximum length of the string</param>
    /// <returns>Leftmost maxLength characters from the string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is less than 0.</exception>
    [return: NotNullIfNotNull(nameof(src))]
    public static string? TryLeft(this string? src, int maxLength)
    {
        if (src is null)
        {
            return null;
        }

        return maxLength switch
        {
            0 => string.Empty,
            < 0 => throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be greater than 0"),
            _ => src.Length <= maxLength ? src : src[..maxLength],
        };
    }

    /// <summary>
    /// Returns the rightmost maxLength characters from the string.
    /// </summary>
    /// <param name="src">source string</param>
    /// <param name="maxLength">maximum length of the string</param>
    /// <returns>Rightmost maxLength characters from the string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is less than 0.</exception>
    [return: NotNullIfNotNull(nameof(src))]
    public static string? TryRight(this string? src, int maxLength)
    {
        if (src is null)
        {
            return null;
        }

        return maxLength switch
        {
            0 => string.Empty,
            < 0 => throw new ArgumentOutOfRangeException(nameof(maxLength), $"{nameof(maxLength)} must be greater than 0"),
            _ => src.Length <= maxLength ? src : src[^maxLength..],
        };
    }

    /// <summary>
    /// Compares two strings ignoring case.
    /// </summary>
    /// <param name="src">source string</param>
    /// <param name="target">target string</param>
    /// <returns>True if the strings are equal, false otherwise.</returns>
    public static bool EqualsIgnoreCase(this string? src, string? target)
    {
        return string.Equals(src, target, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyCollection<string> GetNonEmpty(params string?[] values)
    {
        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();
    }

    public static string EscapeSpecialTelegramMdCharacters(this string src)
    {
        ArgumentNullException.ThrowIfNull(src);

        string[] chars = ["_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!"];

        foreach (var c in chars)
        {
            src = src.Replace(c, @"\" + c, StringComparison.Ordinal);
        }

        return src;
    }

    public static string EscapeSpecialTelegramHtmlCharacters(this string src)
    {
        ArgumentNullException.ThrowIfNull(src);

        return src
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("&", "&amp;", StringComparison.Ordinal);
    }

    public static string CutToLastClosingLinkTag(this string src)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return src;
        }

        const string closingTag = "</a>";
        if (!src.EndsWith(closingTag, StringComparison.Ordinal))
        {
            var lastIndexOfBracket = src.LastIndexOf(closingTag, StringComparison.Ordinal);
            src = src[..(lastIndexOfBracket + closingTag.Length)];
        }

        return src;
    }
}
