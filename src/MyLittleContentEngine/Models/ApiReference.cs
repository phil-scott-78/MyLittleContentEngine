namespace MyLittleContentEngine.Models;

/// <summary>
/// Base class for API reference items
/// </summary>
public abstract record ApiReferenceItem
{
    /// <summary>
    /// XML Documentation ID
    /// </summary>
    public required string XmlDocId { get; init; }
    
    /// <summary>
    /// Display name of the item
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Full name including namespace
    /// </summary>
    public required string FullName { get; init; }
    
    /// <summary>
    /// Full name including namespace, with minimal formatting
    /// </summary>
    public required string MinimalFullName { get; init; }
    
    /// <summary>
    /// XML documentation summary
    /// </summary>
    public string? Summary { get; init; }
    
    /// <summary>
    /// XML documentation remarks
    /// </summary>
    public string? Remarks { get; init; }
    
    /// <summary>
    /// Declaration syntax as a string
    /// </summary>
    public required string Declaration { get; init; }
    
    /// <summary>
    /// Microsoft-style identifier (e.g., namespace.typename.membername)
    /// </summary>
    public required string MicrosoftStyleId { get; init; }
}

/// <summary>
/// Represents a namespace in the API reference
/// </summary>
public record ApiNamespace : ApiReferenceItem
{
    /// <summary>
    /// Types contained in this namespace
    /// </summary>
    public required IReadOnlyList<ApiType> Types { get; init; } = Array.Empty<ApiType>();
}

/// <summary>
/// Represents a type (class, interface, struct, etc.) in the API reference
/// </summary>
public record ApiType : ApiReferenceItem
{
    /// <summary>
    /// The namespace this type belongs to
    /// </summary>
    public required string Namespace { get; init; }
    
    /// <summary>
    /// The kind of type (class, interface, struct, enum, etc.)
    /// </summary>
    public required string TypeKind { get; init; }
    
    /// <summary>
    /// Base type name if any  
    /// </summary>
    public string? BaseType { get; init; }
    
    /// <summary>
    /// Implemented interfaces
    /// </summary>
    public required IReadOnlyList<string> Interfaces { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Members of this type
    /// </summary>
    public required IReadOnlyList<ApiMember> Members { get; init; } = Array.Empty<ApiMember>();
}

/// <summary>
/// Represents a member (method, property, field, etc.) in the API reference
/// </summary>
public record ApiMember : ApiReferenceItem
{
    /// <summary>
    /// The type this member belongs to
    /// </summary>
    public required string ContainingType { get; init; }
    
    /// <summary>
    /// The namespace of the containing type
    /// </summary>
    public required string Namespace { get; init; }
    
    /// <summary>
    /// The kind of member (method, property, field, etc.)
    /// </summary>
    public required string MemberKind { get; init; }
    
    /// <summary>
    /// Return type for methods and properties, or field type for fields
    /// </summary>
    public string? ReturnType { get; init; }
    
    /// <summary>
    /// Return type for methods and properties, or field type for fields
    /// </summary>
    public string? ReturnTypeDisplayName { get; init; }
    
    /// <summary>
    /// Parameters for methods
    /// </summary>
    public required IReadOnlyList<ApiParameter> Parameters { get; init; } = Array.Empty<ApiParameter>();
}

/// <summary>
/// Represents a parameter in a method signature
/// </summary>
public record ApiParameter
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Parameter type
    /// </summary>
    public required string Type { get; init; }
    
    /// <summary>
    /// Parameter type's minimal display name
    /// </summary>
    public required string TypeDisplayName { get; init; }
    
    /// <summary>
    /// Whether the parameter has a default value
    /// </summary>
    public bool HasDefaultValue { get; init; }
    
    /// <summary>
    /// The default value if any
    /// </summary>
    public string? DefaultValue { get; init; }
    
    /// <summary>
    /// Parameter documentation
    /// </summary>
    public string? Summary { get; init; }
}