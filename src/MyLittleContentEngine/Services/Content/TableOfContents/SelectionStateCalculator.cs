namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal static class SelectionStateCalculator
{
    public static bool IsSelected(TreeNode node, string currentUrl, NavigationTreeItem[] children)
    {
        return NavigationUrlComparer.AreEqual(node.Url, currentUrl) ||
               children.Any(c => c.IsSelected);
    }
}