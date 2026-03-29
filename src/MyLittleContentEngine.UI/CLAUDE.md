# MyLittleContentEngine.UI

Razor component library providing reusable UI components for content-driven websites. Includes navigation, layout, and content display components with built-in theming support.

## Razor Components

### Layout & Grid Components

- **`Card.razor`** - Displays content in a bordered card with optional icon and title. Supports color theming (primary/accent/base).
- **`LinkCard.razor`** - Clickable card variant that wraps content in an anchor tag with hover effects.
- **`CardGrid.razor`** - Responsive grid container for cards. Configurable column count (defaults to 2 columns).
- **`BigTable.razor`** - Wrapper for responsive tables with horizontal scroll on small screens.

### Content Components

- **`Badge.razor`** - Inline badge component with variants (success/tip/caution/danger/note) and sizes (small/medium/large).
- **`Steps.razor`** - Ordered list container for step-by-step instructions with vertical timeline styling.
- **`Step.razor`** - Individual step item with numbered indicator. Used within `Steps` component.

### Navigation Components

- **`OutlineNavigation.razor`** - Page outline/table of contents for current page. Shows H2/H3 headings with active section highlighting and smooth transitions. Supports nested entries.
- **`TableOfContentsNavigation.razor`** - Site-wide navigation tree. Renders hierarchical navigation with current page indication and customizable styling.

## JavaScript Functionality (`wwwroot/scripts.js`)

Modular ES6 classes managed by a central `PageManager` orchestrator.

### Component Managers

- **`ThemeManager`** - Dark/light theme switching with localStorage persistence; exposes `window.swapTheme()`
- **`OutlineManager`** - Scroll-based active section highlighting for page outline (130px header offset)
- **`TabManager`** - ARIA-compliant tab navigation; supports multiple tab groups per page
- **`MermaidManager`** - Lazy-loads Mermaid.js from CDN with theme-aware rendering
- **`MobileNavManager`** - Hamburger menu toggle with overlay, escape key, and body scroll lock
- **`MainSiteNavManager`** - Mobile main-nav menu; auto-closes at 768px breakpoint
- **`SidebarToggleManager`** - TOC sidebar overlay toggle for multi-section layouts
- **`SearchManager`** - FlexSearch-powered modal search (Cmd/Ctrl+K); lazy-loads index on first use
- **`SyntaxHighlighter`** - Highlight.js integration for 20+ languages
