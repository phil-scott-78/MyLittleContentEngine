# Services/Content/TableOfContents

Table of contents generation, hierarchical navigation tree building, breadcrumb navigation, and page sequencing for content sites.

## Files

### BreadcrumbItem.cs
- **BreadcrumbItem** - Represents a single item in a breadcrumb navigation trail with name, URL, and current state

### ContentTocItem.cs
- **ContentTocItem** - Record representing a Table of Contents entry with hierarchy parts for building navigation structure

### FolderNodeHandler.cs
- **FolderNodeHandler** - Handles building navigation items for folder nodes, with special logic for folders containing index pages

### IndexPageNodeHandler.cs
- **IndexPageNodeHandler** - Handles building navigation items for index page nodes with selection state calculation

### ITableOfContentService.cs
- **ITableOfContentService** - Interface for service that generates and provides table of contents data with navigation, breadcrumbs, and sections

### NavigationInfo.cs
- **NavigationInfo** - Contains navigation information for a URL including section, breadcrumbs, page title, and next/previous pages

### NavigationNodeHandler.cs
- **NavigationNodeHandler** - Abstract base class for handling different types of navigation tree nodes with factory method for handler selection

### NavigationTreeItem.cs
- **NavigationTreeItem** - Represents a hierarchical navigation tree item with name, URL, children, order, and selection state

### NavigationUrlComparer.cs
- **NavigationUrlComparer** - Static utility for comparing URLs with normalization (handles index pages and trailing slashes)

### PageNodeHandler.cs
- **PageNodeHandler** - Handles building navigation items for regular page nodes with selection state calculation

### PageWithOrder.cs
- **PageWithOrder** - Internal record holding page data with title, URL, order, and hierarchy parts for TOC building

### SelectionStateCalculator.cs
- **SelectionStateCalculator** - Static utility for determining if a navigation node should be marked as selected

### TableOfContentService.cs
- **TableOfContentService** - Main implementation of ITableOfContentService that builds hierarchical navigation from content services

### TreeNode.cs
- **TreeNode** - Internal record representing a tree node in the navigation hierarchy with children, page data, and metadata
