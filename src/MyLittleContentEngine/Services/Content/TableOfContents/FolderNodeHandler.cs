namespace MyLittleContentEngine.Services.Content.TableOfContents;

internal class FolderNodeHandler : NavigationNodeHandler
{
    public override NavigationTreeItem BuildNavigationItem(TreeNode node, string currentUrl, NavigationTreeItem[] children)
    {
        // Check if this folder node has a direct child that is an index page
        var indexChildTreeNode = node.Children.Values
            .FirstOrDefault(childNode => childNode is { IsIndex: true, HasPage: true });

        return indexChildTreeNode != null
            ? BuildFolderWithIndexChild(node, indexChildTreeNode, currentUrl, children)
            : BuildStandardFolder(node, currentUrl, children);
    }

    private static NavigationTreeItem BuildFolderWithIndexChild(TreeNode node, TreeNode indexChildTreeNode, string currentUrl, NavigationTreeItem[] children)
    {
        NavigationTreeItem? indexChildTocEntry = null;
        var indexOfIndexChildTocEntry = -1;

        for (var i = 0; i < children.Length; i++)
        {
            if (children[i].Href != null &&
                indexChildTreeNode.Url != null &&
                NavigationUrlComparer.AreEqual(children[i].Href, indexChildTreeNode.Url))
            {
                indexChildTocEntry = children[i];
                indexOfIndexChildTocEntry = i;
                break;
            }
        }

        // Fallback to standard folder if index child not found
        if (indexChildTocEntry == null)
        {
            return BuildStandardFolder(node, currentUrl, children);
        }

        var itemsForFolder = children.Where((_, i) => i != indexOfIndexChildTocEntry).ToList();
        itemsForFolder.AddRange(indexChildTocEntry.Items);
        itemsForFolder = itemsForFolder.OrderBy(e => e.Order).ToList();

        var isSelectedForAbsorbedFolder = NavigationUrlComparer.AreEqual(indexChildTreeNode.Url, currentUrl) ||
                                          itemsForFolder.Any(item => item.IsSelected);

        return new NavigationTreeItem
        {
            Name = indexChildTreeNode.Title!,
            Href = indexChildTreeNode.Url,
            Items = itemsForFolder.ToArray(),
            Order = indexChildTreeNode.Order,
            IsSelected = isSelectedForAbsorbedFolder
        };
    }

    private static NavigationTreeItem BuildStandardFolder(TreeNode node, string currentUrl, NavigationTreeItem[] children)
    {
        var folderOrder = children.Length != 0
            ? children.Min(e => e.Order)
            : int.MaxValue;

        var anyDescendantSelected = children.Any(e => e.IsSelected);

        return new NavigationTreeItem
        {
            Name = FolderToTitle(node),
            Href = null,
            Items = children,
            Order = folderOrder,
            IsSelected = anyDescendantSelected
        };
    }

    private static string FolderToTitle(TreeNode node)
    {
        const string dashReplacement = "(!gonna be a dash here!)";
        return node.Segment
            .Replace("--", dashReplacement)
            .Replace("-", " ")
            .Replace(dashReplacement, "-")
            .ToApaTitleCase();
    }
}