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

The JavaScript is organized into modular ES6 classes managed by a central `PageManager`.

### Core Manager

**`PageManager`** - Main orchestrator that initializes all component managers on DOM ready.

### Component Managers

**`ThemeManager`**
- Dark/light theme switching
- Persists theme preference to localStorage
- Integrates with Mermaid diagrams for theme-aware rendering
- Exposes global `swapTheme()` for backwards compatibility

**`OutlineManager`**
- Tracks scroll position and highlights active section in page outline
- Uses IntersectionObserver-style logic with configurable header offset (130px)
- Updates visual highlighter indicator with smooth transitions
- Builds section map from outline links and heading IDs

**`TabManager`**
- Manages tab navigation with ARIA attributes
- Handles tab activation, content panel visibility
- Supports multiple tab groups per page via unique IDs

**`MermaidManager`**
- Dynamic loading of Mermaid.js from CDN
- Theme-aware diagram rendering (light/dark modes)
- Converts OKLCH CSS variables to hex for Mermaid theme configuration
- Supports diagram re-rendering on theme change
- Custom theme variables mapped to MonorailCSS color tokens

**`MobileNavManager`**
- Hamburger menu toggle for mobile navigation sidebar
- Overlay backdrop with click-to-close
- Auto-close on link click (mobile only)
- Escape key support and body scroll prevention

**`MainSiteNavManager`**
- Mobile menu for main site navigation links
- Auto-closes on window resize to desktop breakpoint (768px)
- Click-outside and escape key handlers

**`SidebarToggleManager`**
- Table of contents sidebar toggle for Spectre.Console-style layouts
- Overlay-based sidebar with close button
- Prevents event propagation when clicking inside panel

**`SearchManager`**
- FlexSearch-powered client-side search
- Modal interface with keyboard shortcuts (Cmd/Ctrl+K)
- Lazy-loads search index and FlexSearch library on first use
- Weighted field scoring (title > description > headings > content)
- Search priority multiplier support
- Snippet generation with query highlighting
- Error handling for failed index loading

**`SyntaxHighlighter`**
- Highlight.js integration for code syntax highlighting
- Supports 20+ languages (JavaScript, TypeScript, Python, C#, etc.)
- Auto-detects language from CSS classes (`language-*`)
- Excludes special classes (mermaid, text)
- Fallback to auto-detection for unregistered languages

### Utility Functions

**OKLCH to Hex Conversion** - Converts CSS `oklch()` color values to hex for libraries that don't support modern CSS colors (used by Mermaid).

### Global Exports

- `window.pageManager` - Access to PageManager instance
- `window.swapTheme()` - Legacy theme toggle function
