namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal record TreeNode
{
    public string Segment { get; init; } = "";

    public Dictionary<string, TreeNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasPage { get; set; }
    public bool IsIndex { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public int Order { get; set; }
}