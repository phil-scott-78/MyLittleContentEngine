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
            : BuildStandardFolder(node, children);
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
            return BuildStandardFolder(node, children);
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

    private static NavigationTreeItem BuildStandardFolder(TreeNode node, NavigationTreeItem[] children)
    {
        var folderOrder = node.FolderMetadata?.Order ??
            (children.Length != 0 ? children.Min(e => e.Order) : int.MaxValue);

        var anyDescendantSelected = children.Any(e => e.IsSelected);

        return new NavigationTreeItem
        {
            Name = GetFolderTitle(node),
            Href = null,
            Items = children,
            Order = folderOrder,
            IsSelected = anyDescendantSelected
        };
    }

    private static string GetFolderTitle(TreeNode node)
    {
        // Priority 1: Folder metadata (_index.metadata.yml Title property)
        if (node.FolderMetadata?.Title != null)
        {
            return node.FolderMetadata.Title;
        }

        // Priority 2: Index.md frontmatter (check for index child with Title)
        var indexChild = node.Children.Values
            .FirstOrDefault(child => child is { IsIndex: true, HasPage: true, Title: not null });

        if (indexChild?.Title != null)
        {
            return indexChild.Title;
        }

        // Priority 3: Folder name conversion (existing behavior)
        return FolderToTitle(node);
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