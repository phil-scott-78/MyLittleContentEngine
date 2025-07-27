using System.Text;
using System.Text.RegularExpressions;

namespace MyLittleContentEngine.Services.Content.MarkdigExtensions.CodeHighlighting;

internal static partial class GbnfHighlighter
{
    public static string Highlight(string gbnfText)
    {
        // Tokenize the GBNF text first
        var tokens = TokenizeGbnf(gbnfText);

        // Convert tokens to HTML
        var html = new StringBuilder();
        html.Append("<pre><code>");

        foreach (var token in tokens)
        {
            var escapedText = EscapeHtml(token.Text);
            switch (token.Type)
            {
                case TokenType.RuleName:
                    html.Append($"<span class=\"hljs-variable\">{escapedText}</span>");
                    break;
                case TokenType.Comment:
                    html.Append($"<span class=\"hljs-comment\">{escapedText}</span>");
                    break;
                case TokenType.StringLiteral:
                    html.Append($"<span class=\"hljs-string\">{escapedText}</span>");
                    break;
                case TokenType.CharRange:
                    html.Append($"<span class=\"hljs-regexp\">{escapedText}</span>");
                    break;
                case TokenType.Operator:
                    html.Append($"<span class=\"hljs-operator\">{escapedText}</span>");
                    break;
                case TokenType.Identifier:
                    html.Append($"<span class=\"hljs-name\">{escapedText}</span>");
                    break;
                case TokenType.Whitespace:
                default:
                    html.Append(escapedText);
                    break;
            }
        }

        html.Append("</code></pre>");
        return html.ToString();
    }

    private static List<Token> TokenizeGbnf(string text)
    {
        var tokens = new List<Token>();
        var currentPosition = 0;

        // Keep track of rule names to identify references later
        var ruleNames = new HashSet<string>();

        // First pass: identify rule names
        var ruleNameMatches = IdentityRuleRegex().Matches(text);
        foreach (Match match in ruleNameMatches)
        {
            ruleNames.Add(match.Groups[1].Value);
        }

        while (currentPosition < text.Length)
        {
            // Try to match a rule name declaration
            var ruleNameMatch = RuleNameDeclarationRegex().Match(text[currentPosition..]);
            if (ruleNameMatch is { Success: true, Index: 0 })
            {
                var ruleName = ruleNameMatch.Groups[1].Value;
                tokens.Add(new Token { Type = TokenType.RuleName, Text = ruleName });
                currentPosition += ruleName.Length;
                continue;
            }

            // Try to match a comment
            var commentMatch = CommentRegex().Match(text[currentPosition..]);
            if (commentMatch is { Success: true, Index: 0 })
            {
                var comment = commentMatch.Groups[1].Value;
                tokens.Add(new Token { Type = TokenType.Comment, Text = comment });
                currentPosition += comment.Length;
                continue;
            }

            // Try to match a string literal
            var stringMatch = StringLiteralRegex().Match(text[currentPosition..]);
            if (stringMatch is { Success: true, Index: 0 })
            {
                var stringLiteral = stringMatch.Value;
                tokens.Add(new Token { Type = TokenType.StringLiteral, Text = stringLiteral });
                currentPosition += stringLiteral.Length;
                continue;
            }

            // Try to match a character range
            var rangeMatch = CharacterRangeRegex().Match(text[currentPosition..]);
            if (rangeMatch is { Success: true, Index: 0 })
            {
                var charRange = rangeMatch.Value;
                tokens.Add(new Token { Type = TokenType.CharRange, Text = charRange });
                currentPosition += charRange.Length;
                continue;
            }

            // Try to match an operator
            var operatorMatch = OperatorRegex().Match(text[currentPosition..]);
            if (operatorMatch is { Success: true, Index: 0 })
            {
                var op = operatorMatch.Value;
                tokens.Add(new Token { Type = TokenType.Operator, Text = op });
                currentPosition += op.Length;
                continue;
            }

            // Try to match an identifier (reference to a rule)
            var identifierMatch = IdentifierRegex().Match(text[currentPosition..]);
            if (identifierMatch is { Success: true, Index: 0 })
            {
                var identifier = identifierMatch.Value;
                // Check if this is a known rule name (but not a rule declaration which we already handled)
                if (ruleNames.Contains(identifier))
                {
                    tokens.Add(new Token { Type = TokenType.Identifier, Text = identifier });
                    currentPosition += identifier.Length;
                    continue;
                }
            }

            // Match whitespace or a single character
            var whitespaceMatch = WhitespaceRegex().Match(text[currentPosition..]);
            if (whitespaceMatch.Success)
            {
                var whitespace = whitespaceMatch.Value;
                tokens.Add(new Token { Type = TokenType.Whitespace, Text = whitespace });
                currentPosition += whitespace.Length;
            }
            else
            {
                // Fallback in case of an unexpected pattern
                tokens.Add(new Token { Type = TokenType.Whitespace, Text = text[currentPosition].ToString() });
                currentPosition++;
            }
        }

        return tokens;
    }

    private static string EscapeHtml(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }

    [GeneratedRegex(@"^([a-zA-Z][a-zA-Z0-9_-]*)\s*::=", RegexOptions.Multiline)]
    private static partial Regex IdentityRuleRegex();

    [GeneratedRegex(@"^([a-zA-Z][a-zA-Z0-9_-]*)\s*(?=::=)", RegexOptions.Singleline)]
    private static partial Regex RuleNameDeclarationRegex();

    [GeneratedRegex(@"^(//.*?)(?=\r?\n|$)", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"^""([^""\\]*(\\.[^""\\]*)*)""", RegexOptions.Singleline)]
    private static partial Regex StringLiteralRegex();

    [GeneratedRegex(@"^\[([^\]\\]*(\\.[^\]\\]*)*)\]", RegexOptions.Singleline)]
    private static partial Regex CharacterRangeRegex();

    [GeneratedRegex(@"^(::=|\||\*|\+|\?|\(|\)|{|}|,)", RegexOptions.Singleline)]
    private static partial Regex OperatorRegex();

    [GeneratedRegex(@"^([a-zA-Z][a-zA-Z0-9_-]*)", RegexOptions.Singleline)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^(\s+|.)", RegexOptions.Singleline)]
    private static partial Regex WhitespaceRegex();
}

internal enum TokenType
{
    RuleName,
    Comment,
    StringLiteral,
    CharRange,
    Operator,
    Identifier,
    Whitespace
}

internal class Token
{
    public required TokenType Type { get; init; }
    public required string Text { get; init; }
}