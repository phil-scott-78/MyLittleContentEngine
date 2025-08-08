using MyLittleContentEngine.Models;

namespace Spectre.Console;

public class BaseSpectreConsoleFrontMatter : IFrontMatter
{
    public string Title { get; init; } = "Empty title";
    public string Description { get; init; } = string.Empty;
    public string? Uid { get; init; } = null;

    public DateTime Date { get; init; } = DateTime.Now;
    public bool IsDraft { get; init; } = false;
    public string[] Tags { get; init; } = [];
    public string? RedirectUrl { get; init; }
    public string? Section { get; init; }
    public int Order { get; init; } = int.MaxValue;
    
    public Metadata AsMetadata()
    {
        return new Metadata()
        {
            Title = Title,
            Description = Description,
            LastMod = Date,
            RssItem = true,
            Order = Order
        };
    }
}

public class SpectreConsoleCliFrontMatter : BaseSpectreConsoleFrontMatter
{
}

public class SpectreConsoleFrontMatter : BaseSpectreConsoleFrontMatter
{
}