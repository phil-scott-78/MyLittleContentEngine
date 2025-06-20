﻿using System.Collections.Immutable;
using System.Web;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.Roslyn;
using MyLittleContentEngine.Services.Infrastructure;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Content service for generating API reference documentation from .NET assemblies using Roslyn
/// </summary>
public class ApiReferenceContentService : IContentService, IDisposable
{
    private readonly IRoslynExampleCoordinator _roslynCoordinator;
    private readonly ILogger<ApiReferenceContentService> _logger;
    private readonly ApiReferenceContentOptions _options;
    private readonly LazyAndForgetful<ApiReferenceData> _apiDataCache;
    private bool _disposed;

    public ApiReferenceContentService(
        ApiReferenceContentOptions options,
        IRoslynExampleCoordinator roslynCoordinator,
        ILogger<ApiReferenceContentService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _roslynCoordinator = roslynCoordinator ?? throw new ArgumentNullException(nameof(roslynCoordinator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiDataCache = new LazyAndForgetful<ApiReferenceData>(async () => await BuildApiReferenceDataAsync());
    }

    /// <inheritdoc />
    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var apiData = await _apiDataCache.Value;
        var pages = new List<PageToGenerate>
        {
            // Add root API index page
            new(
                Url: $"/{_options.BasePageUrl}/",
                OutputFile: $"{_options.BasePageUrl}/index.html",
                Metadata: new Models.Metadata
                {
                    Title = "API Reference",
                    Description = "API Reference Documentation",
                    Order = 0
                })
        };

        // Add namespace pages
        foreach (var ns in apiData.Namespaces)
        {
            pages.Add(new PageToGenerate(
                Url: ns.Url,
                OutputFile: $"{_options.BasePageUrl}/namespace/{HttpUtility.UrlEncode(ns.XmlDocId)}/index.html",
                Metadata: new Models.Metadata
                {
                    Title = $"{ns.Name} Namespace",
                    Description = $"Types in the {ns.Name} namespace",
                    Order = 1
                }));
        }

        // Add type pages
        foreach (var type in apiData.Types)
        {
            pages.Add(new PageToGenerate(
                Url: type.Url,
                OutputFile: $"{_options.BasePageUrl}/type/{HttpUtility.UrlEncode(type.XmlDocId)}/index.html",
                Metadata: new Models.Metadata
                {
                    Title = $"{type.Name} {type.TypeKind}",
                    Description = type.Summary ?? $"{type.TypeKind} {type.FullName}",
                    Order = 2
                }));
        }

        // Member pages are no longer generated - members are included inline in type pages

        _logger.LogInformation("Generated {Count} API reference pages", pages.Count);
        return pages.ToImmutableList();
    }

    /// <inheritdoc />
    public Task<ImmutableList<PageToGenerate>> GetTocEntriesToGenerateAsync()
    {
        // Only expose the root /api/ entry in the table of contents
        var rootApiPage = new PageToGenerate(
            Url: $"/{_options.BasePageUrl}/",
            OutputFile: $"{_options.BasePageUrl}/index.html",
            Metadata: new Models.Metadata
            {
                Title = "API Reference",
                Description = "API Reference Documentation",
                Order = int.MaxValue // Put it at the end of the TOC
            });

        return Task.FromResult(ImmutableList.Create(rootApiPage));
    }

    /// <inheritdoc />
    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
    {
        // No content to copy for API reference
        return Task.FromResult(ImmutableList<ContentToCopy>.Empty);
    }

    /// <inheritdoc />
    public async Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        var apiData = await _apiDataCache.Value;
        var crossRefs = new List<CrossReference>();

        // Add namespace cross-references
        foreach (var ns in apiData.Namespaces)
        {
            crossRefs.Add(new CrossReference
            {
                Uid = ns.XmlDocId,
                Title = ns.Name,
                Url = ns.Url
            });
        }

        // Add type cross-references
        foreach (var type in apiData.Types)
        {
            crossRefs.Add(new CrossReference
            {
                Uid = type.XmlDocId,
                Title = type.FullName,
                Url = type.Url
            });
        }

        // Member cross-references are no longer generated - members are included inline in type pages

        _logger.LogInformation("Generated {Count} API cross-references", crossRefs.Count);
        return crossRefs.ToImmutableList();
    }

    /// <summary>
    /// Gets all API namespaces for display purposes
    /// </summary>
    public async Task<ImmutableList<ApiNamespace>> GetNamespacesAsync()
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Namespaces.ToImmutableList();
    }

    /// <summary>
    /// Gets all API types for display purposes
    /// </summary>
    public async Task<ImmutableList<ApiType>> GetTypesAsync()
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Types.ToImmutableList();
    }

    /// <summary>
    /// Gets all API members for display purposes
    /// </summary>
    public async Task<ImmutableList<ApiMember>> GetMembersAsync()
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Members.ToImmutableList();
    }

    /// <summary>
    /// Gets a specific namespace by name
    /// </summary>
    public async Task<ApiNamespace?> GetNamespaceByNameAsync(string name)
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific namespace by XML Doc ID
    /// </summary>
    public async Task<ApiNamespace?> GetNamespaceByXmlDocIdAsync(string xmlDocId)
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.XmlDocId, xmlDocId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific type by namespace and name
    /// </summary>
    public async Task<ApiType?> GetTypeByNameAsync(string namespaceName, string typeName)
    {
        var apiData = await _apiDataCache.Value;

        // Find the namespace first, then get the type from it (which has populated members)
        var ns = apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.Name, namespaceName, StringComparison.OrdinalIgnoreCase));

        return ns?.Types.FirstOrDefault(t =>
            string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific type by XML Doc ID
    /// </summary>
    public async Task<ApiType?> GetTypeByXmlDocIdAsync(string xmlDocId)
    {
        var apiData = await _apiDataCache.Value;
        return apiData.Types.FirstOrDefault(t =>
            string.Equals(t.XmlDocId, xmlDocId, StringComparison.OrdinalIgnoreCase));
    }

    // Member retrieval methods removed - members are now accessed through their containing types

    private async Task<ApiReferenceData> BuildApiReferenceDataAsync()
    {
        _logger.LogInformation("Building API reference data from Roslyn workspace");

        // Get all symbols from RoslynExampleCoordinator
        var symbolData = await _roslynCoordinator.GetAllSymbolsAsync();

        var typeSymbols = new List<INamedTypeSymbol>();
        var memberSymbols = new List<ISymbol>();

        // Extract symbols from cache, filtering for public members only
        foreach (var kvp in symbolData)
        {
            var symbol = kvp.Value.Symbol;

            // Skip non-public symbols
            if (symbol.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                typeSymbols.Add(typeSymbol);
            }
            else if (symbol is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
            {
                // Only include public members
                if (symbol.ContainingType?.DeclaredAccessibility == Accessibility.Public)
                {
                    memberSymbols.Add(symbol);
                }
            }
        }

        // Group types by namespace and filter based on IncludeNamespace option
        var namespaceGroups = typeSymbols
            .GroupBy(t => t.ContainingNamespace?.ToDisplayString() ?? string.Empty)
            .Where(g => ShouldIncludeNamespace(g.Key))
            .OrderBy(g => g.Key)
            .ToList();

        var namespaces = new List<ApiNamespace>();
        var types = new List<ApiType>();
        var members = new List<ApiMember>();

        // Build namespace and type data
        foreach (var nsGroup in namespaceGroups)
        {
            var namespaceName = nsGroup.Key;
            if (string.IsNullOrEmpty(namespaceName)) continue;

            var nsTypes = new List<ApiType>();

            foreach (var typeSymbol in nsGroup.OrderBy(t => t.Name))
            {
                // Add members for this type
                var typeMembers = CreateApiMembers(typeSymbol, memberSymbols);
                members.AddRange(typeMembers);

                var apiType = CreateApiType(typeSymbol, typeMembers);
                nsTypes.Add(apiType);
                types.Add(apiType);
            }

            var xmlDocId = $"N:{namespaceName}";
            var apiNamespace = new ApiNamespace
            {
                XmlDocId = xmlDocId,
                Name = namespaceName,
                FullName = namespaceName,
                MinimalFullName = namespaceName,
                Declaration = $"namespace {namespaceName}",
                Url = $"/{_options.BasePageUrl}/namespace/{HttpUtility.UrlEncode(xmlDocId)}/",
                Types = nsTypes,
                Summary = $"The {namespaceName} namespace"
            };

            namespaces.Add(apiNamespace);
        }

        _logger.LogInformation("Built API data: {NamespaceCount} namespaces, {TypeCount} types, {MemberCount} members",
            namespaces.Count, types.Count, members.Count);

        return new ApiReferenceData(namespaces, types, members);
    }

    private ApiType CreateApiType(INamedTypeSymbol typeSymbol, List<ApiMember> typeMembers)
    {
        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var xmlDocId = typeSymbol.GetDocumentationCommentId() ?? string.Empty;

        // Get XML documentation
        var xmlDoc = typeSymbol.GetDocumentationCommentXml();
        var summary = ExtractSummaryFromXmlDoc(xmlDoc ?? string.Empty);
        var remarks = ExtractRemarksFromXmlDoc(xmlDoc ?? string.Empty);

        // Get base type and interfaces
        var baseType = typeSymbol.BaseType?.ToDisplayString();
        var interfaces = typeSymbol.Interfaces.Select(i => i.ToDisplayString()).ToList();

        // Create declaration syntax
        var declaration = CreateTypeDeclaration(typeSymbol);

        return new ApiType
        {
            XmlDocId = xmlDocId,
            Name = typeSymbol.Name,
            FullName = typeSymbol.ToDisplayString(),
            MinimalFullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            Declaration = declaration,
            Url = $"/{_options.BasePageUrl}/type/{HttpUtility.UrlEncode(xmlDocId)}/",
            Namespace = namespaceName,
            TypeKind = typeSymbol.TypeKind.ToString().ToLowerInvariant(),
            BaseType = baseType,
            Interfaces = interfaces,
            Summary = summary,
            Remarks = remarks,
            Members = typeMembers
        };
    }

    private List<ApiMember> CreateApiMembers(INamedTypeSymbol typeSymbol, List<ISymbol> allMembers)
    {
        var typeMembers = allMembers
            .Where(m => SymbolEqualityComparer.Default.Equals(m.ContainingType, typeSymbol))
            .Where(m => m.DeclaredAccessibility == Accessibility.Public)
            .Where(m => !ShouldFilterMember(m, typeSymbol))
            .OrderBy(m => m.Name)
            .ToList();

        // For records, we also need to get members directly from the type symbol
        // This ensures we capture primary constructor parameters and record-specific members
        if (typeSymbol.IsRecord)
        {
            var directMembers = typeSymbol.GetMembers()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                .Where(m => m is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
                .Where(m => !ShouldFilterMember(m, typeSymbol))
                .Where(m => !typeMembers.Any(tm => SymbolEqualityComparer.Default.Equals(tm, m)))
                .ToList();
            
            typeMembers.AddRange(directMembers);
        }

        var apiMembers = new List<ApiMember>();

        foreach (var member in typeMembers.OrderBy(m => m.Name))
        {
            var apiMember = CreateApiMember(member);
            if (apiMember != null)
            {
                apiMembers.Add(apiMember);
            }
        }

        return apiMembers;
    }

    private ApiMember? CreateApiMember(ISymbol memberSymbol)
    {
        var xmlDocId = memberSymbol.GetDocumentationCommentId();
        if (string.IsNullOrEmpty(xmlDocId)) return null;

        var containingType = memberSymbol.ContainingType?.Name ?? string.Empty;
        var namespaceName = memberSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        // Get XML documentation
        var xmlDoc = memberSymbol.GetDocumentationCommentXml();
        var summary = ExtractSummaryFromXmlDoc(xmlDoc ?? string.Empty);
        var remarks = ExtractRemarksFromXmlDoc(xmlDoc ?? string.Empty);

        // Create declaration and get member-specific info
        string declaration;
        string returnType = string.Empty;
        string returnTypeDisplayName = string.Empty;
        var parameters = new List<ApiParameter>();

        switch (memberSymbol)
        {
            case IMethodSymbol method:
                declaration = CreateMethodDeclaration(method);
                returnType = method.ReturnType.ToDisplayString();
                returnTypeDisplayName= method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                parameters = CreateApiParameters(method, xmlDoc ?? string.Empty);
                break;
            case IPropertySymbol property:
                declaration = CreatePropertyDeclaration(property);
                returnType = property.Type.ToDisplayString();
                returnTypeDisplayName= property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                break;
            case IFieldSymbol field:
                declaration = CreateFieldDeclaration(field);
                returnType = field.Type.ToDisplayString();
                returnTypeDisplayName= field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                break;
            case IEventSymbol eventSymbol:
                declaration = CreateEventDeclaration(eventSymbol);
                returnType = eventSymbol.Type.ToDisplayString();
                returnTypeDisplayName= eventSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                break;
            default:
                declaration = memberSymbol.ToDisplayString();
                break;
        }

        var memberKind = GetMemberKind(memberSymbol);

        return new ApiMember
        {
            XmlDocId = xmlDocId,
            Name = memberSymbol.Name,
            FullName = memberSymbol.ToDisplayString(),
            MinimalFullName = memberSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            Declaration = declaration,
            Url = $"/{_options.BasePageUrl}/member/{HttpUtility.UrlEncode(xmlDocId)}/",
            ContainingType = containingType,
            Namespace = namespaceName,
            MemberKind = memberKind,
            ReturnType = returnType,
            ReturnTypeDisplayName = returnTypeDisplayName,
            Parameters = parameters,
            Summary = summary,
            Remarks = remarks
        };
    }

    private string CreateTypeDeclaration(INamedTypeSymbol typeSymbol)
    {
        var modifiers = new List<string>();

        if (typeSymbol.DeclaredAccessibility == Accessibility.Public)
            modifiers.Add("public");

        if (typeSymbol.IsStatic)
            modifiers.Add("static");

        if (typeSymbol.IsAbstract && typeSymbol.TypeKind == TypeKind.Class)
            modifiers.Add("abstract");

        if (typeSymbol.IsSealed)
            modifiers.Add("sealed");

        var typeKind = typeSymbol.TypeKind switch
        {
            TypeKind.Class => typeSymbol.IsRecord ? "record" : "class",
            TypeKind.Interface => "interface",
            TypeKind.Struct => "struct",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            _ => "type"
        };

        return $"{string.Join(" ", modifiers)} {typeKind} {typeSymbol.Name}";
    }

    private string CreateMethodDeclaration(IMethodSymbol method)
    {
        var modifiers = new List<string>();

        if (method.DeclaredAccessibility == Accessibility.Public)
            modifiers.Add("public");

        if (method.IsStatic)
            modifiers.Add("static");

        if (method.IsVirtual)
            modifiers.Add("virtual");

        if (method.IsOverride)
            modifiers.Add("override");

        if (method.IsAbstract)
            modifiers.Add("abstract");

        var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}"));

        return $"{string.Join(" ", modifiers)} {returnType} {method.Name}({parameters})";
    }

    private string CreatePropertyDeclaration(IPropertySymbol property)
    {
        var modifiers = new List<string>();

        if (property.DeclaredAccessibility == Accessibility.Public)
            modifiers.Add("public");

        if (property.IsStatic)
            modifiers.Add("static");

        if (property.IsVirtual)
            modifiers.Add("virtual");

        if (property.IsOverride)
            modifiers.Add("override");

        if (property.IsRequired)
            modifiers.Add("required");

        var accessors = new List<string>();
        if (property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            accessors.Add("get");
        if (property.SetMethod?.DeclaredAccessibility == Accessibility.Public)
        {
            // Check if it's an init-only setter (common in records)
            if (property.SetMethod.IsInitOnly)
                accessors.Add("init");
            else
                accessors.Add("set");
        }

        return
            $"{string.Join(" ", modifiers)} {property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {property.Name} {{ {string.Join("; ", accessors)}; }}";
    }

    private string CreateFieldDeclaration(IFieldSymbol field)
    {
        var modifiers = new List<string>();

        if (field.DeclaredAccessibility == Accessibility.Public)
            modifiers.Add("public");

        if (field.IsStatic)
            modifiers.Add("static");

        if (field.IsReadOnly)
            modifiers.Add("readonly");

        if (field.IsConst)
            modifiers.Add("const");

        return $"{string.Join(" ", modifiers)} {field.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {field.Name}";
    }

    private string CreateEventDeclaration(IEventSymbol eventSymbol)
    {
        var modifiers = new List<string>();

        if (eventSymbol.DeclaredAccessibility == Accessibility.Public)
            modifiers.Add("public");

        if (eventSymbol.IsStatic)
            modifiers.Add("static");

        if (eventSymbol.IsVirtual)
            modifiers.Add("virtual");

        if (eventSymbol.IsOverride)
            modifiers.Add("override");

        return $"{string.Join(" ", modifiers)} event {eventSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {eventSymbol.Name}";
    }

    private List<ApiParameter> CreateApiParameters(IMethodSymbol method, string xmlDoc)
    {
        var parameters = new List<ApiParameter>();
        var paramDocs = ExtractParameterDocsFromXmlDoc(xmlDoc);

        foreach (var param in method.Parameters)
        {
            var defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;

            parameters.Add(new ApiParameter
            {
                Name = param.Name,
                Type = param.Type.ToDisplayString(),
                TypeDisplayName = param.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                HasDefaultValue = param.HasExplicitDefaultValue,
                DefaultValue = defaultValue,
                Summary = paramDocs.GetValueOrDefault(param.Name)
            });
        }

        return parameters;
    }

    private string GetMemberKind(ISymbol symbol) => symbol switch
    {
        IMethodSymbol => "method",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        _ => "member"
    };

    private bool ShouldFilterMember(ISymbol member, INamedTypeSymbol containingType)
    {
        // Filter out compiler-generated members
        if (member.IsImplicitlyDeclared)
            return true;

        // For records, filter out common auto-generated methods
        if (containingType.IsRecord && member is IMethodSymbol method)
        {
            // Filter out property accessor methods (get_PropertyName, set_PropertyName)
            if (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet)
                return true;

            // Filter out common auto-generated record methods
            var methodName = method.Name;
            if (methodName == "Equals" && method.Parameters.Length <= 1)
                return true;
            if (methodName == "GetHashCode" && method.Parameters.Length == 0)
                return true;
            if (methodName == "ToString" && method.Parameters.Length == 0)
                return true;
            if (methodName == "Deconstruct")
                return true;
            if (methodName == "<Clone>$")
                return true;
            if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                return true;
        }

        return false;
    }

    private string GenerateMemberIdFromApiMember(ApiMember member)
    {
        if (member.MemberKind == "method" && member.Parameters.Any())
        {
            return $"{member.Name}({string.Join(",", member.Parameters.Select(p => p.Type.Split('.').Last()))})";
        }

        return member.Name;
    }

    private string ExtractSummaryFromXmlDoc(string xmlDoc)
    {
        if (string.IsNullOrEmpty(xmlDoc)) return string.Empty;

        try
        {
            var doc = XDocument.Parse(xmlDoc);
            var summaryElement = doc.Root?.Element("summary");
            return summaryElement != null ? ConvertXmlDocToHtml(summaryElement) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ExtractRemarksFromXmlDoc(string xmlDoc)
    {
        if (string.IsNullOrEmpty(xmlDoc)) return string.Empty;

        try
        {
            var doc = XDocument.Parse(xmlDoc);
            var remarksElement = doc.Root?.Element("remarks");
            return remarksElement != null ? ConvertXmlDocToHtml(remarksElement) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ConvertXmlDocToHtml(XElement element)
    {
        var result = new System.Text.StringBuilder();
        ConvertXmlNodeToHtml(element, result);
        return result.ToString().Trim();
    }

    private void ConvertXmlNodeToHtml(XNode node, System.Text.StringBuilder result)
    {
        switch (node)
        {
            case XText text:
                result.Append(System.Net.WebUtility.HtmlEncode(text.Value));
                break;

            case XElement element:
                switch (element.Name.LocalName.ToLowerInvariant())
                {
                    case "para":
                        result.Append("<p>");
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        result.Append("</p>");
                        break;

                    case "c":
                        result.Append("<code>");
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        result.Append("</code>");
                        break;

                    case "code":
                        result.Append("<pre><code>");
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        result.Append("</code></pre>");
                        break;

                    case "example":
                        result.Append("<div>");
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        result.Append("</div>");
                        break;

                    case "list":
                        ConvertListToHtml(element, result);
                        break;

                    case "summary":
                    case "remarks":
                        // For summary and remarks, just process the children without adding wrapper tags
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        break;

                    default:
                        // For any other elements, just process the children
                        foreach (var child in element.Nodes())
                        {
                            ConvertXmlNodeToHtml(child, result);
                        }

                        break;
                }

                break;
        }
    }

    private void ConvertListToHtml(XElement listElement, System.Text.StringBuilder result)
    {
        var type = listElement.Attribute("type")?.Value.ToLowerInvariant();

        switch (type)
        {
            case "table":
                ConvertTableListToHtml(listElement, result);
                break;

            case "number":
                ConvertSimpleListToHtml(listElement, result, "ol");
                break;

            case "bullet":
            default:
                ConvertSimpleListToHtml(listElement, result, "ul");
                break;
        }
    }

    private void ConvertTableListToHtml(XElement listElement, System.Text.StringBuilder result)
    {
        result.Append("<table>");

        var hasHeader = listElement.Element("listheader") != null;
        if (hasHeader)
        {
            result.Append("<thead><tr>");
            var header = listElement.Element("listheader");
            var termHeader = header?.Element("term");
            var descHeader = header?.Element("description");

            result.Append("<th>");
            if (termHeader != null)
            {
                foreach (var child in termHeader.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }

            result.Append("</th>");

            result.Append("<th>");
            if (descHeader != null)
            {
                foreach (var child in descHeader.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }

            result.Append("</th>");

            result.Append("</tr></thead>");
        }

        result.Append("<tbody>");
        foreach (var item in listElement.Elements("item"))
        {
            result.Append("<tr>");

            var term = item.Element("term");
            var description = item.Element("description");

            result.Append("<td>");
            if (term != null)
            {
                foreach (var child in term.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }
            else
            {
                // Fallback to item content if no term element
                foreach (var child in item.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }

            result.Append("</td>");

            result.Append("<td>");
            if (description != null)
            {
                foreach (var child in description.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }

            result.Append("</td>");

            result.Append("</tr>");
        }

        result.Append("</tbody></table>");
    }

    private void ConvertSimpleListToHtml(XElement listElement, System.Text.StringBuilder result, string listTag)
    {
        result.Append($"<{listTag}>");

        foreach (var item in listElement.Elements("item"))
        {
            result.Append("<li>");

            var term = item.Element("term");
            var description = item.Element("description");

            if (term != null || description != null)
            {
                // Handle structured items with term/description
                if (term != null)
                {
                    result.Append("<strong>");
                    foreach (var child in term.Nodes())
                    {
                        ConvertXmlNodeToHtml(child, result);
                    }

                    result.Append("</strong>");

                    if (description != null)
                    {
                        result.Append(" - ");
                    }
                }

                if (description != null)
                {
                    foreach (var child in description.Nodes())
                    {
                        ConvertXmlNodeToHtml(child, result);
                    }
                }
            }
            else
            {
                // Handle simple items (direct content)
                foreach (var child in item.Nodes())
                {
                    ConvertXmlNodeToHtml(child, result);
                }
            }

            result.Append("</li>");
        }

        result.Append($"</{listTag}>");
    }

    private Dictionary<string, string> ExtractParameterDocsFromXmlDoc(string xmlDoc)
    {
        var paramDocs = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(xmlDoc)) return paramDocs;

        try
        {
            var doc = XDocument.Parse(xmlDoc);
            var paramElements = doc.Root?.Elements("param");

            if (paramElements != null)
            {
                foreach (var param in paramElements)
                {
                    var name = param.Attribute("name")?.Value;
                    var value = param.Value.Trim();

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    {
                        paramDocs[name] = ConvertXmlDocToHtml(param);
                    }
                }
            }
        }
        catch
        {
            // Ignore XML parsing errors
        }

        return paramDocs;
    }

    /// <summary>
    /// Determines whether a namespace should be included based on the IncludeNamespace configuration.
    /// </summary>
    /// <param name="namespaceName">The name of the namespace to check</param>
    /// <returns>True if the namespace should be included, false otherwise</returns>
    private bool ShouldIncludeNamespace(string namespaceName)
    {
        // If the namespace name is null or empty, exclude it
        if (string.IsNullOrEmpty(namespaceName))
            return false;

        var isIncluded = _options.IncludeNamespace.Length == 0 ||
                         _options.IncludeNamespace.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal));
        
        var isExcluded = _options.ExcludedNamespace.Length > 0 &&
                         _options.ExcludedNamespace.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal));

        // If neither IncludeNamespace nor ExcludeNamespaces are set, include all namespaces
        if (_options.IncludeNamespace.Length == 0 && _options.ExcludedNamespace.Length == 0)
            return true;

        // If only IncludeNamespace is set
        if (_options.IncludeNamespace.Length > 0 && _options.ExcludedNamespace.Length == 0)
            return isIncluded;

        // If only ExcludeNamespaces is set
        if (_options.IncludeNamespace.Length == 0 && _options.ExcludedNamespace.Length > 0)
            return !isExcluded;

        // If both IncludeNamespace and ExcludeNamespaces are set
        return isIncluded && !isExcluded;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _apiDataCache.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal data structure for holding API reference data
/// </summary>
internal record ApiReferenceData(
    IReadOnlyList<ApiNamespace> Namespaces,
    IReadOnlyList<ApiType> Types,
    IReadOnlyList<ApiMember> Members);
