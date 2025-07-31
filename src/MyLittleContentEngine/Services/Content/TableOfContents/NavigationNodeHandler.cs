namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal abstract class NavigationNodeHandler
{
    public abstract NavigationTreeItem BuildNavigationItem(TreeNode node, string currentUrl, NavigationTreeItem[] children);

    public static NavigationNodeHandler GetHandler(TreeNode node)
    {
        if (node is { HasPage: true, IsIndex: true })
            return new IndexPageNodeHandler();

        if (node.HasPage)
            return new PageNodeHandler();

        return new FolderNodeHandler();
    }
}