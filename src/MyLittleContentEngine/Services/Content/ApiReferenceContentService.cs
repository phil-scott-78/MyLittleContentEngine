using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SymbolAnalysis;
using MyLittleContentEngine.Services.Content.CodeAnalysis.SolutionWorkspace;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace MyLittleContentEngine.Services.Content;

/// <summary>
/// Content service for generating API reference documentation from .NET assemblies using Roslyn
/// </summary>
public class ApiReferenceContentService : IContentService, IDisposable
{
    /// <inheritdoc />
    public int SearchPriority => 5; // Medium priority for API reference content
    private readonly ISymbolExtractionService _symbolService;
    private readonly ISolutionWorkspaceService _workspaceService;
    private readonly CodeAnalysisOptions _codeAnalysisOptions;
    private readonly ILogger<ApiReferenceContentService> _logger;
    private readonly ApiReferenceContentOptions _options;
    private readonly AsyncLazy<ApiReferenceData> _apiDataCache;
    private bool _disposed;

    public ApiReferenceContentService(
        ApiReferenceContentOptions options,
        ISymbolExtractionService symbolService,
        ISolutionWorkspaceService workspaceService,
        CodeAnalysisOptions codeAnalysisOptions,
        ILogger<ApiReferenceContentService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _symbolService = symbolService ?? throw new ArgumentNullException(nameof(symbolService));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _codeAnalysisOptions = codeAnalysisOptions;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiDataCache = new AsyncLazy<ApiReferenceData>(
            async () => await BuildApiReferenceDataAsync(),
            AsyncLazyFlags.RetryOnFailure);
    }

    /// <inheritdoc />
    public async Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var apiData = await _apiDataCache;
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

        // Add namespace pages if enabled
        if (_options.UrlOptions.GenerateNamespacePages)
        {
            foreach (var ns in apiData.Namespaces)
            {
                var url = BuildUrlFromTemplate(_options.UrlOptions.NamespaceUrlTemplate, ns);
                var outputFile = BuildUrlFromTemplate(_options.UrlOptions.NamespaceOutputTemplate, ns);

                pages.Add(new PageToGenerate(
                    Url: url,
                    OutputFile: outputFile,
                    Metadata: new Models.Metadata
                    {
                        Title = $"{ns.Name} Namespace",
                        Description = $"Types in the {ns.Name} namespace",
                        Order = 1
                    }));
            }
        }

        // Add type pages if enabled
        if (_options.UrlOptions.GenerateTypePages)
        {
            foreach (var type in apiData.Types)
            {
                var url = BuildUrlFromTemplate(_options.UrlOptions.TypeUrlTemplate, type);
                var outputFile = BuildUrlFromTemplate(_options.UrlOptions.TypeOutputTemplate, type);

                pages.Add(new PageToGenerate(
                    Url: url,
                    OutputFile: outputFile,
                    Metadata: new Models.Metadata
                    {
                        Title = $"{type.Name} {type.TypeKind}",
                        Description = type.Summary ?? $"{type.TypeKind} {type.FullName}",
                        Order = 2
                    }));
            }
        }

        // Member pages are no longer generated - members are included inline in type pages

        _logger.LogInformation("Generated {Count} API reference pages", pages.Count);
        return pages.ToImmutableList();
    }

    /// <inheritdoc />
    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
    {
        // Only expose the root /api/ entry in the table of contents
        var rootApiEntry = new ContentTocItem(
            "API Reference",
            $"/{_options.BasePageUrl}/",
            int.MaxValue, // Put it at the end of the TOC
            [_options.BasePageUrl]); // Single hierarchy part for API

        return Task.FromResult(ImmutableList.Create(rootApiEntry));
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
        var apiData = await _apiDataCache;
        var crossRefs = new List<CrossReference>();

        // Add namespace cross-references if namespace pages are enabled
        if (_options.UrlOptions.GenerateNamespacePages)
        {
            foreach (var ns in apiData.Namespaces)
            {
                var url = BuildUrlFromTemplate(_options.UrlOptions.NamespaceUrlTemplate, ns);
                // Ensure URL ends with trailing slash for consistency
                if (!url.EndsWith('/'))
                    url += "/";

                crossRefs.Add(new CrossReference
                {
                    Uid = ns.XmlDocId,
                    Title = ns.Name,
                    Url = url
                });
            }
        }

        // Add type cross-references if type pages are enabled
        if (_options.UrlOptions.GenerateTypePages)
        {
            foreach (var type in apiData.Types)
            {
                var url = BuildUrlFromTemplate(_options.UrlOptions.TypeUrlTemplate, type);
                // Ensure URL ends with trailing slash for consistency
                if (!url.EndsWith('/'))
                    url += "/";

                crossRefs.Add(new CrossReference
                {
                    Uid = type.XmlDocId,
                    Title = type.FullName,
                    Url = url
                });
            }
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
        var apiData = await _apiDataCache;
        return apiData.Namespaces.ToImmutableList();
    }

    /// <summary>
    /// Gets all API types for display purposes
    /// </summary>
    public async Task<ImmutableList<ApiType>> GetTypesAsync()
    {
        var apiData = await _apiDataCache;
        return apiData.Types.ToImmutableList();
    }

    /// <summary>
    /// Gets all API members for display purposes
    /// </summary>
    public async Task<ImmutableList<ApiMember>> GetMembersAsync()
    {
        var apiData = await _apiDataCache;
        return apiData.Members.ToImmutableList();
    }

    /// <summary>
    /// Gets a specific namespace by name
    /// </summary>
    public async Task<ApiNamespace?> GetNamespaceByNameAsync(string name)
    {
        var apiData = await _apiDataCache;
        return apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific namespace by XML Doc ID
    /// </summary>
    public async Task<ApiNamespace?> GetNamespaceByXmlDocIdAsync(string xmlDocId)
    {
        var apiData = await _apiDataCache;
        return apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.XmlDocId, xmlDocId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific type by namespace and name
    /// </summary>
    public async Task<ApiType?> GetTypeByNameAsync(string namespaceName, string typeName)
    {
        var apiData = await _apiDataCache;

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
        var apiData = await _apiDataCache;
        return apiData.Types.FirstOrDefault(t =>
            string.Equals(t.XmlDocId, xmlDocId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific namespace by slug identifier
    /// </summary>
    public async Task<ApiNamespace?> GetNamespaceBySlugAsync(string slug)
    {
        var apiData = await _apiDataCache;
        return apiData.Namespaces.FirstOrDefault(n =>
            string.Equals(n.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a specific type by slug identifier
    /// </summary>
    public async Task<ApiType?> GetTypeBySlugAsync(string slug)
    {
        var apiData = await _apiDataCache;
        return apiData.Types.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets members by slug identifier (can return multiple results for overloads)
    /// </summary>
    public async Task<ImmutableList<ApiMember>> GetMembersBySlugAsync(string slug)
    {
        var apiData = await _apiDataCache;
        var members = apiData.Members.Where(m =>
            string.Equals(m.Slug, slug, StringComparison.OrdinalIgnoreCase)).ToList();
        return members.ToImmutableList();
    }

    // Member retrieval methods removed - members are now accessed through their containing types

    private async Task<ApiReferenceData> BuildApiReferenceDataAsync()
    {
        _logger.LogInformation("Building API reference data from Roslyn workspace");

        // Get solution path from options or fall back to Roslyn options
        var solutionPath = _options.SolutionPath ?? _codeAnalysisOptions.SolutionPath;
        if (string.IsNullOrEmpty(solutionPath))
        {
            throw new InvalidOperationException("No solution path configured for API reference generation");
        }

        // Load solution and extract all symbols
        var solution = await _workspaceService.LoadSolutionAsync(solutionPath);
        var allSymbols = await _symbolService.ExtractSymbolsAsync(solution);

        var typeSymbols = new List<INamedTypeSymbol>();
        var memberSymbols = new List<ISymbol>();

        // Extract symbols, filtering for public members only
        foreach (var kvp in allSymbols)
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
                Slug = GenerateSlug(namespaceName),
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
            Slug = GenerateSlug(namespaceName, typeSymbol.Name),
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
            Slug = GenerateSlug(namespaceName, containingType, memberSymbol.Name),
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
                    case "see":
                        var cref = element.Attribute("cref")?.Value;
                        if (!string.IsNullOrEmpty(cref))
                        {
                            // Use xref syntax that will be handled by BaseUrlRewritingMiddleware
                            result.Append("<a href=\"xref:");
                            result.Append(System.Net.WebUtility.HtmlEncode(cref));
                            result.Append("\">");

                            // Extract the last part of the cref as display text
                            var name = cref.Split('.').Last().Split('`').First();
                            result.Append("<code>");
                            result.Append(System.Net.WebUtility.HtmlEncode(name));
                            result.Append("</code>");

                            result.Append("</a>");
                        }
                        break;

                    case "seealso":
                        var seealsoCref = element.Attribute("cref")?.Value;
                        if (!string.IsNullOrEmpty(seealsoCref))
                        {
                            // Use xref syntax for seealso references
                            result.Append("<a href=\"xref:");
                            result.Append(System.Net.WebUtility.HtmlEncode(seealsoCref));
                            result.Append("\">");

                            // Extract the last part of the cref as display text
                            var seealsoName = seealsoCref.Split('.').Last().Split('`').First();
                            result.Append(System.Net.WebUtility.HtmlEncode(seealsoName));

                            result.Append("</a>");
                        }
                        break;

                    case "paramref":
                        var paramName = element.Attribute("name")?.Value;
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            result.Append("<code>");
                            result.Append(System.Net.WebUtility.HtmlEncode(paramName));
                            result.Append("</code>");
                        }
                        break;

                    case "typeparamref":
                        var typeParamName = element.Attribute("name")?.Value;
                        if (!string.IsNullOrEmpty(typeParamName))
                        {
                            result.Append("<code>");
                            result.Append(System.Net.WebUtility.HtmlEncode(typeParamName));
                            result.Append("</code>");
                        }
                        break;

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

    /// <summary>
    /// Generates a URL-friendly slug identifier for a namespace
    /// </summary>
    /// <param name="namespaceName">The namespace name</param>
    /// <returns>URL-friendly slug identifier (e.g., "system.collections.generic")</returns>
    private static string GenerateSlug(string namespaceName)
    {
        return namespaceName.ToLowerInvariant();
    }

    /// <summary>
    /// Generates a URL-friendly slug identifier for a type
    /// </summary>
    /// <param name="namespaceName">The namespace name</param>
    /// <param name="typeName">The type name</param>
    /// <returns>URL-friendly slug identifier (e.g., "system.collections.generic.list-1")</returns>
    private static string GenerateSlug(string namespaceName, string typeName)
    {
        var safeTypeName = typeName.Replace("`", "-");
        return $"{namespaceName.ToLowerInvariant()}.{safeTypeName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Generates a URL-friendly slug identifier for a member
    /// </summary>
    /// <param name="namespaceName">The namespace name</param>
    /// <param name="typeName">The type name</param>
    /// <param name="memberName">The member name</param>
    /// <returns>URL-friendly slug identifier (e.g., "system.collections.generic.list-1.add")</returns>
    private static string GenerateSlug(string namespaceName, string typeName, string memberName)
    {
        var safeTypeName = typeName.Replace("`", "-");
        return $"{namespaceName.ToLowerInvariant()}.{safeTypeName.ToLowerInvariant()}.{memberName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Builds a URL from a template by replacing placeholders with values from a namespace.
    /// </summary>
    /// <param name="template">The URL template containing placeholders like {BasePageUrl}, {Slug}, {Name}</param>
    /// <param name="ns">The namespace to get values from</param>
    /// <returns>The URL with placeholders replaced</returns>
    private string BuildUrlFromTemplate(string template, ApiNamespace ns)
    {
        return template
            .Replace("{BasePageUrl}", _options.BasePageUrl)
            .Replace("{Slug}", ns.Slug)
            .Replace("{Name}", ns.Name);
    }

    /// <summary>
    /// Builds a URL from a template by replacing placeholders with values from a type.
    /// </summary>
    /// <param name="template">The URL template containing placeholders like {BasePageUrl}, {Slug}, {Name}, {Namespace}, {TypeName}</param>
    /// <param name="type">The type to get values from</param>
    /// <returns>The URL with placeholders replaced</returns>
    private string BuildUrlFromTemplate(string template, ApiType type)
    {
        return template
            .Replace("{BasePageUrl}", _options.BasePageUrl)
            .Replace("{Slug}", type.Slug)
            .Replace("{Name}", type.Name)
            .Replace("{Namespace}", type.Namespace)
            .Replace("{TypeName}", type.Name);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // AsyncLazy doesn't need explicit disposal
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
