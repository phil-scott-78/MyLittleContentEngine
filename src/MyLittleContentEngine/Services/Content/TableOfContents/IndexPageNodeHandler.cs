namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal class IndexPageNodeHandler : NavigationNodeHandler
{
    public override NavigationTreeItem BuildNavigationItem(TreeNode node, string currentUrl, NavigationTreeItem[] children)
    {
        var isSelected = SelectionStateCalculator.IsSelected(node, currentUrl, children);
        return new NavigationTreeItem
        {
            Name = node.Title!,
            Href = node.Url,
            Items = children,
            Order = node.Order,
            IsSelected = isSelected
        };
    }
}