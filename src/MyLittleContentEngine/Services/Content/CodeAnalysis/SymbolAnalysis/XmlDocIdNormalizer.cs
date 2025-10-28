using System.Text;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;

/// <summary>
/// Normalizes XML Documentation IDs to ensure consistent lookup regardless of namespace qualification in parameters.
/// </summary>
/// <remarks>
/// XML Doc IDs can have different representations for the same member:
/// - M:Type.Method(Namespace.ParamType) vs M:Type.Method(ParamType)
/// This normalizer strips namespace prefixes from parameter types while keeping the member name fully qualified.
/// </remarks>
internal static class XmlDocIdNormalizer
{
    /// <summary>
    /// Normalizes an XML Doc ID by stripping namespace prefixes from parameter types.
    /// </summary>
    /// <param name="xmlDocId">The XML Doc ID to normalize</param>
    /// <returns>The normalized XML Doc ID</returns>
    /// <example>
    /// Input: M:Type.Method(System.String,System.Int32)
    /// Output: M:Type.Method(String,Int32)
    /// </example>
    public static string Normalize(string xmlDocId)
    {
        if (string.IsNullOrEmpty(xmlDocId))
        {
            return xmlDocId;
        }

        // Find the parameter list (if any)
        var paramStartIndex = xmlDocId.IndexOf('(');
        if (paramStartIndex == -1)
        {
            // No parameters, return as-is
            return xmlDocId;
        }

        var paramEndIndex = xmlDocId.LastIndexOf(')');
        if (paramEndIndex == -1 || paramEndIndex < paramStartIndex)
        {
            // Malformed, return as-is
            return xmlDocId;
        }

        // Keep the prefix and member name (everything before the parameter list)
        var prefix = xmlDocId.Substring(0, paramStartIndex + 1);
        var suffix = xmlDocId.Substring(paramEndIndex);
        var parameterList = xmlDocId.Substring(paramStartIndex + 1, paramEndIndex - paramStartIndex - 1);

        // Normalize the parameter list
        var normalizedParams = NormalizeParameterList(parameterList);

        return prefix + normalizedParams + suffix;
    }

    /// <summary>
    /// Normalizes a parameter list by stripping namespace prefixes from all type references.
    /// </summary>
    private static string NormalizeParameterList(string parameterList)
    {
        if (string.IsNullOrEmpty(parameterList))
        {
            return parameterList;
        }

        var result = new StringBuilder(parameterList.Length);
        var currentToken = new StringBuilder();

        for (int i = 0; i < parameterList.Length; i++)
        {
            char c = parameterList[i];

            // Check for delimiters that separate type names
            if (IsDelimiter(c))
            {
                // Process accumulated token
                if (currentToken.Length > 0)
                {
                    result.Append(StripNamespace(currentToken.ToString()));
                    currentToken.Clear();
                }
                result.Append(c);
            }
            else
            {
                currentToken.Append(c);
            }
        }

        // Process final token
        if (currentToken.Length > 0)
        {
            result.Append(StripNamespace(currentToken.ToString()));
        }

        return result.ToString();
    }

    /// <summary>
    /// Checks if a character is a delimiter in XML Doc ID parameter lists.
    /// </summary>
    private static bool IsDelimiter(char c)
    {
        return c switch
        {
            ',' => true,  // Separates parameters or generic arguments
            '{' => true,  // Opens generic type arguments
            '}' => true,  // Closes generic type arguments
            '[' => true,  // Array brackets or dimensions
            ']' => true,  // Array brackets or dimensions
            '(' => true,  // Method pointer types
            ')' => true,  // Method pointer types
            '*' => true,  // Pointer types
            '&' => true,  // Reference types
            '@' => true,  // Ref/out parameters
            _ => false
        };
    }

    /// <summary>
    /// Strips the namespace prefix from a type name.
    /// </summary>
    /// <param name="typeName">The type name, possibly with namespace prefix</param>
    /// <returns>The type name without namespace prefix</returns>
    /// <example>
    /// System.String -> String
    /// MyNamespace.MyType -> MyType
    /// ``0 -> ``0 (generic parameter, unchanged)
    /// </example>
    private static string StripNamespace(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return typeName;
        }

        // Don't strip generic parameter references (e.g., ``0, ``1)
        // These are generic method type parameters
        if (typeName.StartsWith("``"))
        {
            return typeName;
        }

        // Don't strip generic type parameter references (e.g., `0, `1)
        // These are generic type parameters
        if (typeName.StartsWith("`") && typeName.Length > 1 && char.IsDigit(typeName[1]))
        {
            return typeName;
        }

        // Find the last dot and take everything after it
        var lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex == -1)
        {
            // No namespace, return as-is
            return typeName;
        }

        return typeName.Substring(lastDotIndex + 1);
    }
}
