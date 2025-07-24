using System.Collections.Immutable;
using System.IO.Abstractions;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace RecipeExample;

public interface IResponsiveImageContentService : IContentService
{
    Task<byte[]?> ProcessImageAsync(string filename, string size);
    Task<byte[]?> GenerateLqipAsync(string filename);
    Task<(int width, int height)?> GetOriginalImageDimensionsAsync(string filename);
}

internal class ResponsiveImageContentService : IResponsiveImageContentService
{
    public int SearchPriority => 5;
    
    private readonly RecipeContentOptions _options;
    private readonly IFileSystem _fileSystem;
    
    private static readonly string[] ImageSizes = ["xs", "sm", "md", "lg", "xl"];
    private static readonly string[] AllSizes = ["lqip", "xs", "sm", "md", "lg", "xl"];

    public ResponsiveImageContentService(RecipeContentOptions options, IFileSystem fileSystem)
    {
        _options = options;
        _fileSystem = fileSystem;
    }

    public (int width, int height) GetImageDimensions(string size)
    {
        return size.ToLowerInvariant() switch
        {
            "lqip" => (width: 40, height: 30),
            "xs" => (width: 480, height: 360),
            "sm" => (width: 768, height: 576),
            "md" => (width: 1024, height: 768),
            "lg" => (width: 1440, height: 1080),
            "xl" => (width: 1920, height: 1440),
            "full" => (width: 0, height: 0), // Original size
            _ => (width: 1024, height: 768) // Default to md
        };
    }

    public async Task<byte[]?> ProcessImageAsync(string filename, string size)
    {
        var sourcePath = _fileSystem.Path.Combine(_options.RecipePath, $"{filename}.webp");
        
        if (!_fileSystem.File.Exists(sourcePath))
        {
            return null;
        }

        var dimensions = GetImageDimensions(size);

        try
        {
            await using var sourceStream = _fileSystem.File.OpenRead(sourcePath);
            using var image = await Image.LoadAsync(sourceStream);
            
            if (dimensions is { width: > 0, height: > 0 })
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(dimensions.width, dimensions.height),
                    Mode = ResizeMode.Max
                }));
            }

            using var memoryStream = new MemoryStream();
            var encoder = new WebpEncoder()
            {
                UseAlphaCompression = true,
                FileFormat = WebpFileFormatType.Lossy,
                FilterStrength = 60,
                Method = WebpEncodingMethod.Level4,
                Quality = size == "lqip" ? 20 : 75,
            };
            
            if (size == "lqip")
            {
                image.Mutate(x => x.GaussianBlur(2f));
            }
            
            await image.SaveAsWebpAsync(memoryStream, encoder);
            return memoryStream.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        var pages = new List<PageToGenerate>();
        
        if (!_fileSystem.Directory.Exists(_options.RecipePath))
        {
            return Task.FromResult(pages.ToImmutableList());
        }

        var imageFiles = _fileSystem.Directory.GetFiles(_options.RecipePath, "*.webp");
        
        foreach (var imagePath in imageFiles)
        {
            var filename = _fileSystem.Path.GetFileNameWithoutExtension(imagePath);
            
            foreach (var size in AllSizes)
            {
                var url = $"/images/{filename}-{size}.webp";
                var outputPath = $"images/{filename}-{size}.webp";
                
                pages.Add(new PageToGenerate(url, outputPath, new Metadata(), true));
            }
        }

        return Task.FromResult(pages.ToImmutableList());
    }

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
    {
        return Task.FromResult(ImmutableList<ContentTocItem>.Empty);
    }

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync()
    {
        return Task.FromResult(ImmutableList<ContentToCopy>.Empty);
    }

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync()
    {
        return Task.FromResult(ImmutableList<CrossReference>.Empty);
    }

    public async Task<byte[]?> GenerateLqipAsync(string filename)
    {
        return await ProcessImageAsync(filename, "lqip");
    }

    public async Task<(int width, int height)?> GetOriginalImageDimensionsAsync(string filename)
    {
        var sourcePath = _fileSystem.Path.Combine(_options.RecipePath, $"{filename}.webp");
        
        if (!_fileSystem.File.Exists(sourcePath))
        {
            return null;
        }

        try
        {
            await using var sourceStream = _fileSystem.File.OpenRead(sourcePath);
            using var image = await Image.LoadAsync(sourceStream);
            return (image.Width, image.Height);
        }
        catch (Exception)
        {
            return null;
        }
    }
}