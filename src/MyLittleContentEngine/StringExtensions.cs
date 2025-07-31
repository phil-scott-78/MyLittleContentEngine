using System.Globalization;
using System.Text;

namespace MyLittleContentEngine;

internal static class StringExtensions
{
    /// <summary>
    /// Splits on NewLines, first normalizing line endings.
    /// </summary>
    /// <param name="s">The string to split.</param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string[] SplitNewLines(this string s, StringSplitOptions options = StringSplitOptions.None)
    {
        return s.ReplaceLineEndings(Environment.NewLine).Split(Environment.NewLine, options);
    }

    /// <summary>
    /// Converts a string to a URL-friendly slug with a single-pass algorithm.
    /// </summary>
    /// <param name="input">The string to slugify</param>
    /// <param name="maxLength">Maximum length of the resulting slug (default: 80)</param>
    /// <returns>A URL-friendly slug</returns>
    public static string Slugify(this string input, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize the string (decompose Unicode characters)
        var normalizedString = input.Normalize(NormalizationForm.FormD);

        var slugBuilder = new StringBuilder(input.Length);
        var lastWasHyphen = false;

        foreach (var c in normalizedString)
        {
            // Skip non-spacing marks (diacritics)
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            // Convert to lowercase and process
            var lowerChar = char.ToLowerInvariant(c);

            if (lowerChar is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                // Valid alphanumeric character
                slugBuilder.Append(lowerChar);
                lastWasHyphen = false;
            }
            else if (char.IsWhiteSpace(c) || c == '-' || c == '_' || c == '.')
            {
                // Convert whitespace, hyphens, underscores, and periods to hyphens
                // But only add a hyphen if the last character wasn't already a hyphen
                if (!lastWasHyphen && slugBuilder.Length > 0)
                {
                    slugBuilder.Append('-');
                    lastWasHyphen = true;
                }
            }
            // All other characters are ignored
        }

        // Trim hyphens from the end if needed
        var length = slugBuilder.Length;
        if (length > 0 && slugBuilder[length - 1] == '-')
            slugBuilder.Remove(length - 1, 1);

        // Enforce maximum length
        if (slugBuilder.Length > maxLength)
        {
            slugBuilder.Length = maxLength;

            // Ensure we don't end with a hyphen after truncating
            if (slugBuilder[^1] == '-')
                slugBuilder.Remove(slugBuilder.Length - 1, 1);
        }

        return slugBuilder.ToString();
    }

    private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Short conjunctions (3 letters or fewer)
        "and", "as", "but", "for", "if", "nor", "or", "so", "yet",

        // Articles
        "a", "an", "the",

        // Short prepositions (3 letters or fewer)
        "at", "by", "in", "of", "off", "on", "per", "to", "up", "via"
    };

    public static string ToApaTitleCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var span = input.AsSpan();
        var result = new StringBuilder(input.Length);

        var isFirstWord = true;
        var afterPunctuation = false;

        var i = 0;
        while (i < span.Length)
        {
            // Skip whitespace and add it to result
            var whitespaceStart = i;
            while (i < span.Length && char.IsWhiteSpace(span[i]))
                i++;

            if (whitespaceStart < i)
            {
                result.Append(span.Slice(whitespaceStart, i - whitespaceStart));
            }

            if (i >= span.Length)
            {
                break;
            }

            // Find the end of the current word
            var wordStart = i;
            while (i < span.Length && !char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            var wordSpan = span.Slice(wordStart, i - wordStart);
            var processedWord = ProcessWord(wordSpan, isFirstWord, afterPunctuation);
            result.Append(processedWord);

            // Check if this word ends with punctuation that requires next word capitalization
            afterPunctuation = EndsWithPunctuation(wordSpan);
            isFirstWord = false;
        }

        return result.ToString();
    }

    private static bool EndsWithPunctuation(ReadOnlySpan<char> word)
    {
        if (word.Length == 0)
            return false;

        var lastChar = word[^1];
        return lastChar is ':' or '.' or '!' or '?' or '—';
    }

    private static string ProcessWord(ReadOnlySpan<char> word, bool isFirstWord, bool afterPunctuation)
    {
        if (word.Length == 0)
            return string.Empty;

        // Handle hyphenated words
        var hyphenIndex = word.IndexOf('-');
        if (hyphenIndex >= 0)
        {
            var firstPart = word[..hyphenIndex];
            var secondPart = word[(hyphenIndex + 1)..];

            var processedFirst = ProcessSingleWord(firstPart, isFirstWord, afterPunctuation);
            var processedSecond = ProcessSingleWord(secondPart, true, false); // Second part of hyphenated word is capitalized

            return processedFirst + "-" + processedSecond;
        }

        return ProcessSingleWord(word, isFirstWord, afterPunctuation);
    }

    private static string ProcessSingleWord(ReadOnlySpan<char> word, bool isFirstWord, bool afterPunctuation)
    {
        if (word.Length == 0)
            return string.Empty;

        // Extract the core word without leading/trailing punctuation
        var coreWord = ExtractCoreWord(word, out var prefix, out var suffix);

        if (coreWord.Length == 0)
            return word.ToString();

        var shouldCapitalize = isFirstWord ||
                               afterPunctuation ||
                               coreWord.Length >= 4 ||
                               !IsMinorWord(coreWord);

        var processedCore = shouldCapitalize ?
            CapitalizeWord(coreWord) :
            coreWord.ToString().ToLower();

        return prefix + processedCore + suffix;
    }

    private static ReadOnlySpan<char> ExtractCoreWord(ReadOnlySpan<char> word, out string prefix, out string suffix)
    {
        var start = 0;
        var end = word.Length - 1;

        // Find start of core word (skip leading punctuation)
        while (start < word.Length && !char.IsLetter(word[start]))
        {
            start++;
        }

        // Find end of core word (skip trailing punctuation)
        while (end >= start && !char.IsLetter(word[end]))
        {
            end--;
        }

        prefix = start > 0 ? word[..start].ToString() : string.Empty;
        suffix = end < word.Length - 1 ? word[(end + 1)..].ToString() : string.Empty;

        return start <= end 
            ? word.Slice(start, end - start + 1) 
            : ReadOnlySpan<char>.Empty;
    }

    private static bool IsMinorWord(ReadOnlySpan<char> word)
    {
        if (word.Length > 3)
            return false;

        // Create a string for HashSet lookup - only for short words
        Span<char> buffer = stackalloc char[word.Length];
        for (var i = 0; i < word.Length; i++)
        {
            buffer[i] = char.ToLower(word[i]);
        }

        return MinorWords.Contains(buffer.ToString());
    }

    private static string CapitalizeWord(ReadOnlySpan<char> word)
    {
        if (word.Length == 0)
            throw new ArgumentException($"{nameof(word)} cannot be empty", nameof(word));

        return string.Create(word.Length, word, (span, src) =>
        {
            span[0] = char.ToUpper(src[0]);
            src[1..].CopyTo(span[1..]);
        });
    }
}