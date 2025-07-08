using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Bogus;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using MyLittleContentEngine.Models;
using MyLittleContentEngine.Services.Content;
using MyLittleContentEngine.Services.Content.TableOfContents;

namespace SearchExample.Services;

public class RandomContentService : IContentService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly List<PageToGenerate> _items;
    private readonly Dictionary<string, string> _content = new();

    public RandomContentService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseYamlFrontMatter()
            .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
            .Build();
        
        var faker = new Faker { Random = new Randomizer(1229) };

        const int count = 1000;
        _items = [];
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        
        for (var i = 0; i < count; i++)
        {
            var parts = new[] {faker.Hacker.IngVerb(), faker.Hacker.Adjective() + " " + faker.Hacker.Noun(), faker.Hacker.Noun(), faker.Random.AlphaNumeric(10)};
            var title = textInfo.ToTitleCase(string.Join(' ', parts.Take(parts.Length - 1)));
            var urlRootedPath = "/" + Utils.Slashify(parts);
            var url = "/random" + urlRootedPath;
            
            _content.Add(urlRootedPath.Trim('/'), GetContentForUrl(urlRootedPath, title));
            _items.Add(new PageToGenerate(url, url + ".html", new Metadata
            {
                Title = title,
            }));
        }
    }

    public Task<ImmutableList<PageToGenerate>> GetPagesToGenerateAsync()
    {
        return Task.FromResult(_items.ToImmutableList());
    }

    public Task<ImmutableList<ContentTocItem>> GetContentTocEntriesAsync()
    {
        return Task.FromResult(ImmutableList<ContentTocItem>.Empty);
    }

    public Task<ImmutableList<ContentToCopy>> GetContentToCopyAsync() 
        => Task.FromResult(ImmutableList<ContentToCopy>.Empty);

    public Task<ImmutableList<CrossReference>> GetCrossReferencesAsync() 
        => Task.FromResult(ImmutableList<CrossReference>.Empty);

    public int SearchPriority { get; } = 1;

    public string GetContent(string url)
    {
        return _content.GetValueOrDefault(url.Trim('/'), "not found");
    }
    
    private string GetContentForUrl(string url, string title)
    {
        var faker = new Faker { Random = new Randomizer(url.GetHashCode()) };

        var sb =new StringBuilder($"# {title}");
        sb.AppendLine();
        sb.AppendLine();
        
        sb.AppendLine($"{GetParagraph(faker)}");
        sb.AppendLine();
        sb.AppendLine();
        var sectionCount = faker.Random.Int(2, 5);
        for (var i = 0; i < sectionCount; i++)
        {
            var sectionHeader = GetHeader(faker);
            sb.AppendLine($"## {sectionHeader}");
            sb.AppendLine();
            sb.AppendLine();
            
            var paragraphCount = faker.Random.Int(2, 4);
            for (var j = 0; j < paragraphCount; j++)
            {
                sb.AppendLine($"{GetParagraph(faker)}");
                sb.AppendLine();
                sb.AppendLine();
            }
            
            var subHeadingCount = faker.Random.Int(0, 3);
            for (var k = 0; k < subHeadingCount; k++)
            {
                var subHeader = GetHeader(faker);
                sb.AppendLine($"### {subHeader}");
                sb.AppendLine();
                sb.AppendLine();
                
                var subParagraphCount = faker.Random.Int(2, 4);
                for (var l = 0; l < subParagraphCount; l++)
                {
                    sb.AppendLine($"{GetParagraph(faker)}");
                    sb.AppendLine();
                    sb.AppendLine();
                }
            }
        }
        
        return Markdown.ToHtml(sb.ToString(), _pipeline);
        
    }

    private static string GetHeader(Faker faker) => faker.Lorem.Sentence(2, 3);
    private static string GetParagraph(Faker faker) => faker.Lorem.Paragraph();
}