using System.Text;
using Microsoft.AspNetCore.Components;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Utility class for breaking long words (especially .NET identifiers) with word break opportunities.
/// </summary>
public static class WordBreaker
{
    internal static readonly char[] BreakCharacters = { '.', '+', ',', '<', '>', '[', ']', '&', '*', '`' };

    /// <summary>
    /// Inserts word break opportunities (&lt;wbr /&gt;) after specific characters in long words.
    /// Particularly useful for .NET identifiers and technical terms.
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>Text with word break opportunities inserted</returns>
    public static string InsertWordBreaks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder(text.Length * 2);
        
        for (int i = 0; i < text.Length; i++)
        {
            var currentChar = text[i];
            result.Append(currentChar);
            
            // Insert <wbr /> after break characters, but not at the end of the string
            if (i < text.Length - 1 && Array.IndexOf(BreakCharacters, currentChar) >= 0)
            {
                // Skip break for closing characters of pairs when they're followed by opening of next pair
                if (currentChar == '>' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    // Don't add break after > when followed by [
                    continue;
                }
                
                result.Append("<wbr />");
            }
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Creates a MarkupString with word break opportunities inserted.
    /// Use this method in Razor components to safely render HTML with word breaks.
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>A MarkupString with word break opportunities</returns>
    public static MarkupString CreateMarkupStringWithWordBreaks(string text)
    {
        var processedText = InsertWordBreaks(text);
        return new MarkupString(processedText);
    }
    
    /// <summary>
    /// Checks if a word should have word breaks inserted based on its length and content.
    /// </summary>
    /// <param name="word">The word to check</param>
    /// <param name="minLength">Minimum length before considering word breaks (default: 15)</param>
    /// <returns>True if the word should have breaks inserted</returns>
    public static bool ShouldInsertWordBreaks(string word, int minLength = 15)
    {
        if (string.IsNullOrEmpty(word) || word.Length < minLength)
            return false;

        // Check if the word contains any of our break characters
        return word.IndexOfAny(BreakCharacters) >= 0;
    }
}