/**
 * Page Manager - Centralized JavaScript functionality
 * Handles theme switching, table of contents, tabs, syntax highlighting, and mobile navigation
 */
class PageManager {
    constructor() {
        this.init();
    }

    init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.initializeComponents());
        } else {
            this.initializeComponents();
        }
    }

    initializeComponents() {
        this.themeManager = new ThemeManager();
        this.outlineManager = new OutlineManager();
        this.tabManager = new TabManager();
        this.syntaxHighlighter = new SyntaxHighlighter();
        this.mermaidManager = new MermaidManager();
        this.mobileNavManager = new MobileNavManager();
        this.searchManager = new SearchManager();

        // Initialize all components
        this.outlineManager.init();
        this.tabManager.init();
        this.syntaxHighlighter.init();
        this.mermaidManager.init();
        this.mobileNavManager.init();
        this.searchManager.init();
    }
}

/**
 * Theme Manager - Handles dark/light theme switching
 */
class ThemeManager {
    constructor() {
        this.bindThemeToggleEvents();
        
        // Make swapTheme globally available for backwards compatibility
        window.swapTheme = this.swapTheme.bind(this);
    }

    bindThemeToggleEvents() {
        // Find all elements with data-theme-toggle attribute
        const themeToggleButtons = document.querySelectorAll('[data-theme-toggle]');
        
        themeToggleButtons.forEach(button => {
            button.addEventListener('click', () => {
                this.swapTheme();
            });
        });
    }

    swapTheme() {
        const isDark = document.documentElement.classList.contains('dark');

        if (isDark) {
            document.documentElement.classList.remove('dark');
            document.documentElement.dataset.theme = 'light';
            localStorage.theme = 'light';
        } else {
            document.documentElement.classList.add('dark');
            document.documentElement.dataset.theme = 'dark';
            localStorage.theme = 'dark';
        }

        // Re-initialize mermaid with new theme
        if (window.pageManager && window.pageManager.mermaidManager) {
            window.pageManager.mermaidManager.reinitializeForTheme();
        }
    }
}

/**
 * Outline Manager - Handles outline navigation and active section highlighting
 */
class OutlineManager {
    constructor() {
        this.outlineLinks = [];
        this.sectionMap = new Map();
        this.observer = null;
        this.visibleSections = new Set();
        this.passedSections = new Set(); // Track sections that have been scrolled past
    }

    init() {
        this.setupOutline();
        if (this.outlineLinks.length > 0) {
            this.setupIntersectionObserver();
        }
    }

    setupOutline() {
        this.outlineLinks = Array.from(document.querySelectorAll('[data-role="page-outline"] ul li a'));

        // Initialize all links and build section map
        this.outlineLinks.forEach(link => {
            link.dataset.selected ='false';

            const id = this.extractIdFromHref(link.getAttribute('href'));
            if (id) {
                const section = document.getElementById(id);
                if (section) {
                    this.sectionMap.set(section, link);
                }
            }
        });
    }

    extractIdFromHref(href) {
        return href?.split('#')[1] || null;
    }

    setupIntersectionObserver() {
        this.observer = new IntersectionObserver(
            this.handleIntersection.bind(this),
            {
                rootMargin: '-30px 0px -25%', // Very generous - catch sections with small margins
                threshold: [1] // Just needs any part visible
            }
        );

        // Observe all sections
        this.sectionMap.forEach((link, section) => {
            this.observer.observe(section);
        });
    }

    handleIntersection(entries) {
        let hasChanges = false;
        
        entries.forEach(entry => {
            const section = entry.target;
            
            if (entry.isIntersecting) {
                this.visibleSections.add(section);
                this.passedSections.add(section);
                hasChanges = true;
            } else {
                this.visibleSections.delete(section);
                // Keep in passedSections - it was scrolled past
                hasChanges = true;
            }
        });
        
        if (hasChanges) {
            this.updateActiveLinks();
        }
    }
    
    updateActiveLinks() {
        // Reset all links first
        this.resetAllLinks();
        
        const sectionToHighlight = this.findSectionToHighlight();
        if (sectionToHighlight) {
            const link = this.sectionMap.get(sectionToHighlight);
            if (link) {
                this.activateLink(link);
            }
        }
    }
    
    findSectionToHighlight() {
        // Get all sections sorted by document order
        const allSections = Array.from(this.sectionMap.keys());
        const sectionPositions = allSections.map(section => ({
            section,
            top: section.getBoundingClientRect().top
        }));
        
        // Rule 1: If only one is visible, highlight it
        if (this.visibleSections.size === 1) {
            return this.visibleSections.values().next().value;
        }
        
        // Rule 2: If multiple are visible, highlight the top-most visible item
        if (this.visibleSections.size > 1) {
            const visibleSectionPositions = sectionPositions.filter(({section}) => 
                this.visibleSections.has(section)
            );
            // Sort by top position (ascending) to get the top-most
            visibleSectionPositions.sort((a, b) => a.top - b.top);
            return visibleSectionPositions[0].section;
        }
        
        // Rule 3: If none are visible, highlight the first item that is above the top of the screen
        const sectionsAbove = sectionPositions.filter(({top}) => top < 0);
        if (sectionsAbove.length > 0) {
            // Sort by top position descending (closest to 0, meaning most recently passed)
            sectionsAbove.sort((a, b) => b.top - a.top);
            return sectionsAbove[0].section;
        }
        
        // Rule 4: If none are visible and no item is above the top of the screen, highlight the first item
        if (allSections.length > 0) {
            // Sort sections by document order (using their DOM position)
            const sortedSections = allSections.sort((a, b) => {
                const posA = a.compareDocumentPosition(b);
                return posA & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
            });
            return sortedSections[0];
        }
        
        return null;
    }

    resetAllLinks() {
        this.outlineLinks.forEach(link => {
            link.dataset.selected = 'false';
            link.parentElement?.classList.remove('active');
        });
    }

    activateLink(link) {
        link.dataset.selected = 'true';
        link.parentElement?.classList.add('active');
    }

    destroy() {
        if (this.observer) {
            this.observer.disconnect();
        }
    }
}

/**
 * Tab Manager - Handles tab navigation and content switching
 */
class TabManager {
    constructor() {
        this.tablists = [];
    }

    init() {
        this.tablists = Array.from(document.querySelectorAll('[role="tablist"]'));
        this.tablists.forEach(tablist => this.setupTablist(tablist));
    }

    setupTablist(tablist) {
        const tablistId = tablist.id;
        if (!tablistId) return;

        const tabs = Array.from(tablist.querySelectorAll('[role="tab"]'));
        if (tabs.length === 0) return;

        // Set up event listeners
        tabs.forEach(tab => {
            tab.addEventListener('click', () => this.activateTab(tab, tabs));
        });

        // Initialize active state
        this.initializeActiveTab(tablist, tabs);
    }

    initializeActiveTab(tablist, tabs) {
        const activeTab = tablist.querySelector('[data="true"]');

        if (!activeTab && tabs.length > 0) {
            this.activateTab(tabs[0], tabs);
        } else if (activeTab) {
            this.showTabContent(activeTab);
        }
    }

    activateTab(selectedTab, allTabs) {
        // Deactivate all tabs
        allTabs.forEach(tab => {
            tab.dataset.selected  ='false';
            tab.setAttribute('data-state', 'inactive');
            tab.setAttribute('tabindex', '-1');
        });

        // Activate the selected tab
        selectedTab.dataset.selected = 'true';
        selectedTab.setAttribute('data-state', 'active');
        selectedTab.setAttribute('tabindex', '0');

        // Show corresponding content
        this.showTabContent(selectedTab);
    }

    showTabContent(tab) {
        const contentId = tab.getAttribute('aria-controls');
        if (!contentId) return;

        const contentPanel = document.getElementById(contentId);
        if (!contentPanel) return;

        // Hide all related content panels
        this.hideRelatedContentPanels(tab);

        // Show the selected content panel
        contentPanel.removeAttribute('hidden');
        contentPanel.dataset.selected = 'true';
    }

    hideRelatedContentPanels(tab) {
        const tabId = tab.id;
        const match = tabId.match(/^tabButton(.*)-\d+$/);

        if (match) {
            const baseId = match[1];
            const allContentPanels = document.querySelectorAll(`[id^="tab-content${baseId}-"]`);

            allContentPanels.forEach(panel => {
                panel.dataset.selected = 'false';
                panel.setAttribute('hidden', '');
            });
        }
    }
}

/**
 * Mermaid Manager - Handles mermaid diagram rendering with theme support
 */
class MermaidManager {
    constructor() {
        this.mermaidLoaded = false;
        this.mermaidInstance = null;
        this.diagrams = [];
        this.renderedDiagrams = []; // Track rendered diagram containers
    }

    async init() {
        this.diagrams = this.findMermaidDiagrams();
        if (this.diagrams.length === 0) return;

        try {
            await this.loadMermaid();
            await this.renderDiagrams();
        } catch (error) {
            console.error('Failed to initialize mermaid:', error);
        }
    }

    findMermaidDiagrams() {
        // Look for code blocks with class 'language-mermaid'
        return Array.from(document.querySelectorAll('code.language-mermaid'));
    }

    async loadMermaid() {
        if (this.mermaidLoaded) return;

        // Dynamically load mermaid from CDN
        this.mermaidInstance = await import('https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs');
        this.mermaidLoaded = true;
        
        this.initializeMermaid();
    }

    initializeMermaid() {
        if (!this.mermaidInstance) return;

        const isDark = document.documentElement.classList.contains('dark');
        const config = this.getMermaidConfig(isDark);
        
        console.log('Mermaid config:', config); // Debug logging
        
        // Use the correct initialization method
        this.mermaidInstance.default.initialize(config);
    }

    getMermaidConfig(isDark) {
        // Helper function to get CSS variables with fallbacks
        function getCSSVariable(variable, fallback) {
            if (typeof window === 'undefined' || typeof document === 'undefined') {
                return fallback;
            }

            const value = getComputedStyle(document.documentElement).getPropertyValue(variable).trim() || fallback;

            if (value.startsWith('oklch(')) {
                console.log('oklch value detected:', value);
                let s = oklchToHex(value);
                console.log('converted oklch to hex:', s);
                return s;
            }

            console.log('falling back to CSS variable:', variable, 'with value:', value);
            return value;
        }

        // Convert OKLCH string to hex (e.g. "oklch(0.881 0.061 210)" → "#hex")
        function oklchToHex(oklchStr) {
            // Parse the values from the string
            const match = oklchStr.match(/oklch\(\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)\s*\)/);
            if (!match) return '#000000';

            const [_, l, c, h] = match.map(Number);

            // Convert OKLCH to OKLab
            const hRad = (h * Math.PI) / 180; // Correct hue conversion (360° range)
            const a = Math.cos(hRad) * c;
            const b = Math.sin(hRad) * c;

            // Convert OKLab to LMS (cone response)
            const l_lms = l + 0.3963377774 * a + 0.2158037573 * b;
            const m_lms = l - 0.1055613458 * a - 0.0638541728 * b;
            const s_lms = l - 0.0894841775 * a - 1.2914855480 * b;

            // Cube the LMS values to get linear LMS
            const l_linear = Math.pow(l_lms, 3);
            const m_linear = Math.pow(m_lms, 3);
            const s_linear = Math.pow(s_lms, 3);

            // Convert linear LMS to linear RGB
            const r_linear = +4.0767416621 * l_linear - 3.3077115913 * m_linear + 0.2309699292 * s_linear;
            const g_linear = -1.2684380046 * l_linear + 2.6097574011 * m_linear - 0.3413193965 * s_linear;
            const b_linear = -0.0041960863 * l_linear - 0.7034186147 * m_linear + 1.7076147010 * s_linear;

            // Convert linear RGB to sRGB
            const r = srgbTransferFn(r_linear);
            const g = srgbTransferFn(g_linear);
            const b_srgb = srgbTransferFn(b_linear);

            return rgbToHex(r, g, b_srgb);
        }

        function srgbTransferFn(x) {
            // Clamp to valid range first
            x = Math.max(0, Math.min(1, x));
            
            return x <= 0.0031308
                ? 12.92 * x
                : 1.055 * Math.pow(x, 1 / 2.4) - 0.055;
        }

        function rgbToHex(r, g, b) {
            const to255 = (x) => Math.max(0, Math.min(255, Math.round(x * 255)));
            return (
                '#' +
                to255(r).toString(16).padStart(2, '0') +
                to255(g).toString(16).padStart(2, '0') +
                to255(b).toString(16).padStart(2, '0')
            );
        }

        if (isDark) {
            return {
                startOnLoad: false,
                securityLevel: 'loose',
                logLevel: 'error',
                theme: 'base',
                darkMode: true,
                themeVariables: {
                    fontFamily: 'Lexend, sans-serif',
                    
                    // Main colors
                    primaryColor: getCSSVariable('--monorail-color-primary-600', '#BB2528'),
                    primaryTextColor: getCSSVariable('--monorail-color-primary-50', '#ffffff'),
                    
                    // Secondary colors
                    secondaryColor: getCSSVariable('--monorail-color-accent-600', '#006100'),
                    tertiaryColor: getCSSVariable('--monorail-color-tertiary-one-600', '#666666'),
                    
                    // Background colors
                    background: getCSSVariable('--monorail-color-base-950', '#0a0a0a'),
                    mainBkg: getCSSVariable('--monorail-color-base-900', '#1a1a1a'),
                    secondaryBkg: getCSSVariable('--monorail-color-base-800', '#2a2a2a'),
                    tertiaryBkg: getCSSVariable('--monorail-color-base-700', '#333333'),

                    // Note colors
                    noteBorderColor: getCSSVariable('--monorail-color-base-600', '#333333'),
                    noteBkgColor: getCSSVariable('--monorail-color-base-800', '#333333'),
                    
                    // Lines and borders
                    lineColor: getCSSVariable('--monorail-color-accent-400', '#4ade80'),
                    primaryBorderColor: getCSSVariable('--monorail-color-primary-500', '#dc2626'),
                    secondaryBorderColor: getCSSVariable('--monorail-color-accent-500', '#22c55e'),
                    tertiaryBorderColor: getCSSVariable('--monorail-color-tertiary-one-500', '#6b7280'),
                    
                    // Text colors
                    textColor: getCSSVariable('--monorail-color-base-300', '#f3f4f6'),
                    nodeTextColor: getCSSVariable('--monorail-color-primary-50', '#ffffff'),
                    edgeLabelColor: getCSSVariable('--monorail-color-base-200', '#e5e7eb'),
                    
                    // Edge and label backgrounds
                    edgeLabelBackground: getCSSVariable('--monorail-color-base-800', '#1f2937'),
                    
                    // Additional node colors for variety
                    node0: getCSSVariable('--monorail-color-primary-600', '#dc2626'),
                    node1: getCSSVariable('--monorail-color-accent-600', '#059669'),
                    node2: getCSSVariable('--monorail-color-tertiary-one-600', '#4b5563'),
                    node3: getCSSVariable('--monorail-color-tertiary-two-600', '#7c3aed')
                }
            };
        } else {
            return {
                startOnLoad: false,
                securityLevel: 'loose',
                logLevel: 'error',
                theme: 'base',
                darkMode: false,
                themeVariables: {
                    // Main colors
                    primaryColor: getCSSVariable('--monorail-color-primary-700', '#BB2528'),
                    primaryTextColor: getCSSVariable('--monorail-color-base-500', '#ffffff'),
                    
                    // Secondary colors
                    secondaryColor: getCSSVariable('--monorail-color-accent-700', '#006100'),
                    tertiaryColor: getCSSVariable('--monorail-color-tertiary-one-600', '#4b5563'),
                    
                    // Background colors
                    background: getCSSVariable('--monorail-color-base-50', '#f9fafb'),
                    mainBkg: getCSSVariable('--monorail-color-base-100', '#f3f4f6'),
                    secondaryBkg: getCSSVariable('--monorail-color-base-200', '#e5e7eb'),
                    tertiaryBkg: getCSSVariable('--monorail-color-base-150', '#f0f0f0'),

                    // Note colors
                    noteBorderColor: getCSSVariable('--monorail-color-base-200', '#333333'),
                    noteBkgColor: getCSSVariable('--monorail-color-base-100', '#333333'),


                    // Lines and borders
                    lineColor: getCSSVariable('--monorail-color-accent-600', '#16a34a'),
                    primaryBorderColor: getCSSVariable('--monorail-color-primary-600', '#dc2626'),
                    secondaryBorderColor: getCSSVariable('--monorail-color-accent-600', '#16a34a'),
                    tertiaryBorderColor: getCSSVariable('--monorail-color-tertiary-one-400', '#9ca3af'),
                    
                    // Text colors
                    textColor: getCSSVariable('--monorail-color-base-900', '#111827'),
                    nodeTextColor: getCSSVariable('--monorail-color-base-900', '#ffffff'),
                    edgeLabelColor: getCSSVariable('--monorail-color-base-700', '#374151'),
                    
                    // Edge and label backgrounds
                    edgeLabelBackground: getCSSVariable('--monorail-color-base-100', '#f3f4f6'),
                    
                    // Additional node colors for variety
                    node0: getCSSVariable('--monorail-color-primary-600', '#dc2626'),
                    node1: getCSSVariable('--monorail-color-accent-600', '#16a34a'),
                    node2: getCSSVariable('--monorail-color-tertiary-one-600', '#4b5563'),
                    node3: getCSSVariable('--monorail-color-tertiary-two-600', '#7c3aed')
                }
            };
        }
    }

    async renderDiagrams() {
        if (!this.mermaidInstance || this.diagrams.length === 0) return;

        for (let i = 0; i < this.diagrams.length; i++) {
            const codeElement = this.diagrams[i];
            const diagramText = codeElement.textContent;
            
            try {
                const {svg} = await this.mermaidInstance.default.render(`mermaid-diagram-${i}`, diagramText);
                
                // Create a div to hold the SVG
                const diagramContainer = document.createElement('div');
                diagramContainer.className = 'mermaid-diagram';
                diagramContainer.innerHTML = svg;
                diagramContainer.dataset.originalText = diagramText; // Store original text for re-rendering
                
                // Replace the code element with the rendered diagram
                codeElement.parentNode.replaceChild(diagramContainer, codeElement);
                
                // Track the rendered diagram
                this.renderedDiagrams.push(diagramContainer);
            } catch (error) {
                console.error(`Failed to render mermaid diagram ${i}:`, error);
            }
        }
    }

    async reinitializeForTheme() {
        if (!this.mermaidLoaded || this.renderedDiagrams.length === 0) return;

        // Re-initialize mermaid with new theme
        this.initializeMermaid();
        
        // Re-render all existing diagrams
        for (let i = 0; i < this.renderedDiagrams.length; i++) {
            const diagramContainer = this.renderedDiagrams[i];
            const diagramText = diagramContainer.dataset.originalText;
            
            if (diagramText) {
                try {
                    const {svg} = await this.mermaidInstance.default.render(`mermaid-diagram-theme-${i}`, diagramText);
                    diagramContainer.innerHTML = svg;
                } catch (error) {
                    console.error(`Failed to re-render mermaid diagram ${i} for theme:`, error);
                }
            }
        }
    }
}

/**
 * Mobile Navigation Manager - Handles mobile menu toggle and interaction
 */
class MobileNavManager {
    constructor() {
        this.menuToggle = null;
        this.navSidebar = null;
        this.mobileOverlay = null;
        this.isInitialized = false;
    }

    init() {
        this.menuToggle = document.getElementById('menu-toggle');
        this.navSidebar = document.getElementById('nav-sidebar');
        this.mobileOverlay = document.getElementById('mobile-overlay');
        
        if (this.menuToggle && this.navSidebar) {
            this.setupEventListeners();
            this.isInitialized = true;
        }
    }

    setupEventListeners() {
        // Toggle menu on button click
        this.menuToggle.addEventListener('click', () => {
            this.toggleMenu();
        });
        
        // Close menu when clicking on a link (mobile only)
        this.navSidebar.addEventListener('click', (e) => {
            if (e.target.tagName === 'A' && window.innerWidth < 1024) {
                this.closeMenu();
            }
        });
        
        // Close menu when clicking on overlay
        if (this.mobileOverlay) {
            this.mobileOverlay.addEventListener('click', () => {
                this.closeMenu();
            });
        }
        
        // Close menu when clicking outside (mobile only)
        document.addEventListener('click', (e) => {
            if (window.innerWidth < 1024 && 
                !this.navSidebar.contains(e.target) && 
                !this.menuToggle.contains(e.target) && 
                this.isMenuOpen()) {
                this.closeMenu();
            }
        });

        // Close menu on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isMenuOpen()) {
                this.closeMenu();
            }
        });
    }

    toggleMenu() {
        if (this.isMenuOpen()) {
            this.closeMenu();
        } else {
            this.openMenu();
        }
    }

    isMenuOpen() {
        return this.navSidebar.getAttribute('aria-expanded') === 'true';
    }

    closeMenu() {
        this.navSidebar.dataset.expanded = 'false';
        
        if (this.mobileOverlay) {
            this.mobileOverlay.setAttribute('aria-hidden', 'true');
        }
        
        // Re-enable body scrolling
        document.body.setAttribute('data-mobile-menu-open', 'false');
    }

    openMenu() {
        this.navSidebar.dataset.expanded = 'true';
        
        if (this.mobileOverlay) {
            this.mobileOverlay.setAttribute('aria-hidden', 'false');
        }
        
        // Prevent body scrolling when menu is open
        document.body.setAttribute('data-mobile-menu-open', 'true');
    }
}

/**
 * Search Manager - Handles Algolia DocSearch initialization
 */
class SearchManager {
    constructor() {
        this.searchContainer = null;
        this.docsearchLoaded = false;
    }

    async init() {
        this.searchContainer = document.getElementById('docsearch');
        if (!this.searchContainer) return;

        const appId = this.searchContainer.dataset.searchAppId;
        const indexName = this.searchContainer.dataset.searchIndexName;
        const apiKey = this.searchContainer.dataset.searchApiKey;

        if (!appId || !indexName || !apiKey) {
            console.warn('DocSearch: Missing required data attributes');
            return;
        }

        try {
            await this.loadDocSearch();
            await this.initializeDocSearch(appId, indexName, apiKey);
        } catch (error) {
            console.error('Failed to initialize DocSearch:', error);
        }
    }

    async loadDocSearch() {
        if (this.docsearchLoaded) return;

        // Dynamically load DocSearch JS and CSS
        await Promise.all([
            this.loadScript('https://cdn.jsdelivr.net/npm/@docsearch/js@3'),
            this.loadCSS('https://cdn.jsdelivr.net/npm/@docsearch/css@3')
        ]);

        this.docsearchLoaded = true;
    }

    loadScript(src) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    loadCSS(href) {
        return new Promise((resolve, reject) => {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onload = resolve;
            link.onerror = reject;
            document.head.appendChild(link);
        });
    }

    async initializeDocSearch(appId, indexName, apiKey) {
        if (!window.docsearch) {
            throw new Error('DocSearch library not loaded');
        }

        window.docsearch({
            container: '#docsearch',
            appId: appId,
            indexName: indexName,
            apiKey: apiKey,
        });
    }
}

/**
 * Syntax Highlighter - Handles code syntax highlighting with highlight.js
 */
class SyntaxHighlighter {
    constructor() {
        this.prefix = 'language-';
        this.hljs = null;
    }

    async init() {
        const codeNodes = this.getRelevantCodeNodes();
        if (codeNodes.length === 0) return;

        try {
            await this.setupHighlightJs();
            this.highlightCodeNodes(codeNodes);
        } catch (error) {
            console.error('Failed to initialize syntax highlighting:', error);
        }
    }

    getRelevantCodeNodes() {
        const codeNodes = Array.from(document.body.querySelectorAll('code'));
        return codeNodes.filter(node =>
            Array.from(node.classList).some(cls => cls.startsWith(this.prefix) && cls !== this.prefix + 'text' && cls !== this.prefix)
        );
    }

    async setupHighlightJs() {
        // Load highlight.js from CDN
        this.hljs = await import('https://cdn.jsdelivr.net/npm/highlight.js@11/lib/core.min.js');
        
        // Configure highlight.js
        this.hljs.default.configure({
            ignoreUnescapedHTML: true,
            throwUnescapedHTML: false
        });

        // Load common languages
        const languages = [
            'javascript', 'typescript', 'python', 'java', 'csharp', 'cpp', 'c',
            'css', 'html', 'xml', 'json', 'yaml', 'bash', 'shell', 'sql',
            'php', 'ruby', 'go', 'rust', 'kotlin', 'swift', 'markdown'
        ];

        for (const lang of languages) {
            try {
                const langModule = await import(`https://cdn.jsdelivr.net/npm/highlight.js@11/lib/languages/${lang}.min.js`);
                this.hljs.default.registerLanguage(lang, langModule.default);
            } catch (err) {
                // Language not available, skip silently
            }
        }
    }

    highlightCodeNodes(codeNodes) {
        for (const node of codeNodes) {
            try {
                this.highlightSingleNode(node);
            } catch (error) {
                console.error(`Failed to highlight code node:`, error);
            }
        }
    }

    highlightSingleNode(node) {
        const className = Array.from(node.classList)
            .find(cls => cls.startsWith(this.prefix));

        if (!className) return;

        const language = className.slice(this.prefix.length);
        
        // Map some common language aliases
        const languageMap = {
            'js': 'javascript',
            'ts': 'typescript',
            'cs': 'csharp',
            'py': 'python',
            'sh': 'bash',
            'yml': 'yaml'
        };

        const mappedLanguage = languageMap[language] || language;

        try {
            // Check if language is registered
            if (this.hljs.default.getLanguage(mappedLanguage)) {
                const result = this.hljs.default.highlight(node.textContent, { language: mappedLanguage });
                node.innerHTML = result.value;
                node.classList.add('hljs');
            } else {
                // Use auto-detection as fallback
                const result = this.hljs.default.highlightAuto(node.textContent);
                node.innerHTML = result.value;
                node.classList.add('hljs');
            }
        } catch (error) {
            console.warn(`Failed to highlight ${language}:`, error);
        }
    }
}

// Initialize the page manager
const pageManager = new PageManager();

// Make pageManager globally accessible
window.pageManager = pageManager;

