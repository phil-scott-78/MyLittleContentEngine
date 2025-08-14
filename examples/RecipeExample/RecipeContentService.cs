using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Abstractions;
using Testably.Abstractions;
using System.Text.RegularExpressions;
using CooklangSharp;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using MyLittleContentEngine.Services.Infrastructure;
using MyLittleContentEngine.Services;
using RecipeExample.Models;
using YamlDotNet.Serialization;

namespace RecipeExample;

public interface IRecipeContentService : IContentService
{
    Task<RecipeContentPage?> GetRecipeByUrlOrDefault(string url);
    Task<ImmutableList<RecipeContentPage>> GetAllRecipesAsync();
}

internal class RecipeContentService : IDisposable, IRecipeContentService
{
    public int SearchPriority => 10;

    private readonly RecipeContentOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly AsyncLazy<ConcurrentDictionary<string, RecipeContentPage>> _recipeCache;
    private readonly IDeserializer _yamlDeserializer;
    private bool _isDisposed;

    public RecipeContentService(
        RecipeContentOptions options,
        IContentEngineFileWatcher fileWatcher,
        IFileSystem fileSystem)
    {
        _options = options;
        _fileSystem = fileSystem;
        _yamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        fileWatcher.AddPathWatch(options.ContentPath, "*.cook", s => { }, true );
        _recipeCache = new AsyncLazy<ConcurrentDictionary<string, RecipeContentPage>>(
            async () => await ProcessRecipeFiles(),
            AsyncLazyFlags.RetryOnFailure);
    }


    private async Task<ConcurrentDictionary<string, RecipeContentPage>> ProcessRecipeFiles()
    {
        var recipes = new ConcurrentDictionary<string, RecipeContentPage>();

        if (!_fileSystem.Directory.Exists(_options.RecipePath))
        {
            return recipes;
        }

        var recipeFiles = _fileSystem.Directory.GetFiles(_options.RecipePath, _options.FilePattern);

        foreach (var filePath in recipeFiles)
        {
            try
            {
                var content = await _fileSystem.File.ReadAllTextAsync(filePath);
                var fileName = _fileSystem.Path.GetFileNameWithoutExtension(filePath);
                var url = $"/recipes/{fileName}";

                // Parse front matter and content
                var (frontMatter, recipeContent) = ParseFrontMatter(content);
                
                // Parse the recipe content with CookLang
                var parseResult = CooklangParser.Parse(recipeContent);
                
                if (parseResult.Recipe != null)
                {
                    var recipePage = new RecipeContentPage(
                        parseResult.Recipe,
                        frontMatter,
                        fileName,
                        url,
                        content);
                        
                    recipes.TryAdd(url, recipePage);    
                }
                
            }
            catch (Exception)
            {
                // Skip invalid recipe files
            }
        }

        return recipes;
    }

    private (RecipeFrontMatter FrontMatter, string Content) ParseFrontMatter(string content)
    {
        var frontMatter = new RecipeFrontMatter();
        var recipeContent = content;

        // Match YAML front matter pattern: ---\n...\n---
        var frontMatterMatch = Regex.Match(content, @"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n(.*)", 
            RegexOptions.Singleline | RegexOptions.Multiline);

        if (frontMatterMatch.Success)
        {
            var yamlContent = frontMatterMatch.Groups[1].Value;
            recipeContent = frontMatterMatch.Groups[2].Value;

            try
            {
                frontMatter = _yamlDeserializer.Deserialize<RecipeFrontMatter>(yamlContent);
            }
            catch (Exception)
            {
                // If YAML parsing fails, use default front matter
                frontMatter = new RecipeFrontMatter();
            }
        }

        return (frontMatter, recipeContent);
    }

    public async Task<RecipeContentPage?> GetRecipeByUrlOrDefault(string url)
    {
        var data = await _recipeCache;
        return data.GetValueOrDefault(url);
    }

    public async Task<ImmutableList<RecipeContentPage>> GetAllRecipesAsync()
    {
        var data = await _recipeCache;
        return data.Values.ToImmutableList();
    }

    async Task<ImmutableList<PageToGenerate>> IContentService.GetPagesToGenerateAsync()
    {
        var data = await _recipeCache;
        var pages = new List<PageToGenerate>();

        // Add individual recipe pages
        foreach (var (url, recipePage) in data)
        {

            var relativePath = url.Replace('/', Path.DirectorySeparatorChar);
            
            
            var outputFile = _fileSystem.Path.Combine(_options.BasePageUrl, $"{relativePath}.html").TrimStart(Path.DirectorySeparatorChar);
            
            pages.Add(new PageToGenerate(url, outputFile, new Metadata()
            { 
                Title = recipePage.DisplayName 
            }));
        }

        return pages.ToImmutableList();
    }



    async Task<ImmutableList<ContentTocItem>> IContentService.GetContentTocEntriesAsync()
    {
        var pages = await ((IContentService)this).GetPagesToGenerateAsync();
        return pages.Where(p => p.Metadata?.Title != null)
            .Select(p => new ContentTocItem(
                p.Metadata!.Title!,
                p.Url,
                0,
                CreateHierarchyParts(p.Url)))
            .ToImmutableList();
    }

    private static string[] CreateHierarchyParts(string url)
    {
        return url.Trim('/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
    }

    Task<ImmutableList<ContentToCopy>> IContentService.GetContentToCopyAsync()
    {
        return Task.FromResult(ImmutableList<ContentToCopy>.Empty);
    }

    Task<ImmutableList<CrossReference>> IContentService.GetCrossReferencesAsync()
    {
        return Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // AsyncLazy doesn't need explicit disposal
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~RecipeContentService()
    {
        Dispose(disposing: false);
    }
}