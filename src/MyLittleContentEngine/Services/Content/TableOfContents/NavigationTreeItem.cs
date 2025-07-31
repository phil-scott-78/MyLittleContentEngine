namespace MyLittleContentEngine.Services.Content.TableOfContents;

public class NavigationTreeItem
{
    public required string Name { get; init; }
    public required string? Href { get; init; }
    public required NavigationTreeItem[] Items { get; init; }
    public required int Order { get; init; } = int.MaxValue;
    public required bool IsSelected { get; init; }
}