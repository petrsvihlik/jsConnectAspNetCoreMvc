using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace jsConnect.Vanilla;

/// <summary>
/// Username sanitization rules that mirror common Vanilla-side validation.
/// </summary>
/// <remarks>
/// These rules existed in the legacy v2 implementation. They are not part of the jsConnect protocol —
/// Vanilla maps users by their unique ID under v3 — but they can be useful when generating display names.
/// </remarks>
public static partial class UsernameSanitizer
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>
    /// Removes all whitespace characters.
    /// </summary>
    public static string RemoveWhitespace(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return WhitespaceRegex().Replace(input, string.Empty);
    }

    /// <summary>
    /// Strips diacritics (combining marks) from the input, e.g. "Švihlík" → "Svihlik".
    /// </summary>
    public static string RemoveAccents(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var decomposed = input.Normalize(NormalizationForm.FormD);
        var chars = decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Composes the standard sanitization pipeline.
    /// </summary>
    public static string Sanitize(string input, bool allowWhitespace = false, bool allowAccents = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = input;
        if (!allowWhitespace) result = RemoveWhitespace(result);
        if (!allowAccents) result = RemoveAccents(result);
        return result;
    }
}
