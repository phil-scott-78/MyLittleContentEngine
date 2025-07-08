using Markdig.Syntax;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions;

internal static class CodeBlockExtensions
{
    /// <summary>
    /// Parses a string in the format "key=value key2='value with spaces' key3=\"another value\"" into a dictionary
    /// </summary>
    /// <param name="codeBlock">The string to parse</param>
    /// <returns>A dictionary with the parsed keys and values</returns>
    public static Dictionary<string, string> GetArgumentPairs(this FencedCodeBlock codeBlock)
    {
        if (string.IsNullOrWhiteSpace(codeBlock.Arguments))
        {
            return [];
        }

        var input = codeBlock.Arguments;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentPosition = 0;

        while (currentPosition < input.Length)
        {
            // Skip leading whitespace
            while (currentPosition < input.Length && char.IsWhiteSpace(input[currentPosition]))
                currentPosition++;

            if (currentPosition >= input.Length)
                break;

            // Parse key
            int keyStart = currentPosition;
            while (currentPosition < input.Length && input[currentPosition] != '=')
            {
                if (char.IsWhiteSpace(input[currentPosition]))
                    break;
                currentPosition++;
            }

            // If we're at the end of the string or didn't find an equals sign, break
            if (currentPosition >= input.Length || input[currentPosition] != '=')
                break;

            string key = input.Substring(keyStart, currentPosition - keyStart).Trim();
            currentPosition++; // Skip the equals sign

            // Parse value
            string value;

            // Skip whitespace between equals sign and value
            while (currentPosition < input.Length && char.IsWhiteSpace(input[currentPosition]))
                currentPosition++;

            if (currentPosition >= input.Length)
                break;

            if (input[currentPosition] == '\'' || input[currentPosition] == '"')
            {
                // Quoted value
                char quoteChar = input[currentPosition];
                currentPosition++; // Skip the opening quote
                int valueStart = currentPosition;

                // Find the closing quote
                while (currentPosition < input.Length && input[currentPosition] != quoteChar)
                    currentPosition++;

                // If we're at the end of the string without finding the closing quote
                if (currentPosition >= input.Length)
                    value = input.Substring(valueStart);
                else
                    value = input.Substring(valueStart, currentPosition - valueStart);

                currentPosition++; // Skip the closing quote if found
            }
            else
            {
                // Unquoted value
                int valueStart = currentPosition;
                while (currentPosition < input.Length && !char.IsWhiteSpace(input[currentPosition]))
                    currentPosition++;

                value = input.Substring(valueStart, currentPosition - valueStart);
            }

            // Add to dictionary
            result[key] = value;
        }

        return result;
    }
}