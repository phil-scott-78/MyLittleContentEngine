namespace MyLittleContentEngine.Services.Content.Roslyn;

internal static class TextFormatter
{
    public static string NormalizeIndents(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return code;

        var lines = code.TrimEnd().Split('\n');

        // Find the first non-empty line
        var firstNonEmptyIndex = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
        if (firstNonEmptyIndex < 0)
            return string.Empty; // All lines are empty or whitespace

        // Calculate the common indent from the first non-empty line
        var commonIndent = lines[firstNonEmptyIndex].Length - lines[firstNonEmptyIndex].TrimStart(' ').Length;

        // Process each line from the first non-empty line
        for (var i = firstNonEmptyIndex; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                var currentIndent = Math.Min(commonIndent, lines[i].Length - lines[i].TrimStart(' ').Length);
                lines[i] = lines[i][currentIndent..];
            }
        }

        // Join only the lines from the first non-empty one
        return string.Join('\n', lines.Skip(firstNonEmptyIndex));
    }
}