using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MyLittleContentEngine.Services.Content.Roslyn;

internal static class CodeFragmentExtractor
{
    public static async Task<string> ExtractCodeFragmentAsync(Document document, TextSpan originalSpan, SourceText sourceText, bool bodyOnly)
    {
        if (!bodyOnly)
        {
            return sourceText.GetSubText(originalSpan).ToString();
        }
        var syntaxTree = await document.GetSyntaxTreeAsync() ?? throw new NullReferenceException();
        var syntaxRoot = await syntaxTree.GetRootAsync();
        var nodeAtSpan = syntaxRoot.FindNode(originalSpan);
        return nodeAtSpan switch
        {
            MethodDeclarationSyntax methodNode => ExtractMethodBodyContent(methodNode, sourceText, originalSpan),
            ClassDeclarationSyntax classNode => ExtractClassBodyContent(classNode, sourceText, originalSpan),
            _ => sourceText.GetSubText(originalSpan).ToString()
        };
    }

    private static string ExtractMethodBodyContent(MethodDeclarationSyntax methodNode, SourceText sourceText, TextSpan originalSpan)
    {
        if (methodNode.ExpressionBody != null)
        {
            return sourceText.GetSubText(methodNode.ExpressionBody.Span).ToString();
        }
        if (methodNode.Body == null)
        {
            return sourceText.GetSubText(originalSpan).ToString();
        }
        var bodySpan = TextSpan.FromBounds(
            methodNode.Body.OpenBraceToken.Span.End,
            methodNode.Body.CloseBraceToken.SpanStart);
        return sourceText.GetSubText(bodySpan).ToString();
    }

    private static string ExtractClassBodyContent(ClassDeclarationSyntax classNode, SourceText sourceText, TextSpan originalSpan)
    {
        if (classNode.OpenBraceToken.Span.End >= classNode.CloseBraceToken.SpanStart)
        {
            return sourceText.GetSubText(originalSpan).ToString();
        }
        var bodySpan = TextSpan.FromBounds(
            classNode.OpenBraceToken.Span.End,
            classNode.CloseBraceToken.SpanStart);
        return sourceText.GetSubText(bodySpan).ToString();
    }
}
