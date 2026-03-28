/**
 * SPA Navigation for DocSite
 *
 * First page load: full static HTML (normal browser behaviour).
 * Subsequent in-site navigation: intercept link clicks, fetch the pre-generated
 * /_page-data/{url}.json, and rehydrate the page content without a full reload.
 *
 * Falls back to a full page load when no page-data file is available
 * (e.g. API reference pages, custom Razor pages).
 */
(function () {
    'use strict';

    // --- helpers ---------------------------------------------------------------

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // Derive the site title once from the initial <title> so we can keep the
    // "SiteTitle - PageTitle" format during SPA navigation.
    const initialTitle = document.title;
    const dashIdx = initialTitle.indexOf(' - ');
    const siteTitle = dashIdx > -1 ? initialTitle.substring(0, dashIdx) : initialTitle;

    // --- link filtering --------------------------------------------------------

    function isSameOriginDocLink(anchor) {
        if (!anchor.href) return false;
        try {
            const url = new URL(anchor.href);
            if (url.origin !== window.location.origin) return false;
            // Skip page-data URLs themselves
            if (url.pathname.startsWith('/_page-data/')) return false;
            // Skip in-page anchors on the same path
            if (url.pathname === window.location.pathname && url.hash) return false;
            // Skip links that open in a new context or trigger downloads
            if (anchor.target === '_blank' || anchor.hasAttribute('download')) return false;
            return true;
        } catch {
            return false;
        }
    }

    // --- article reconstruction ------------------------------------------------

    const PROSE_CLASSES =
        'prose dark:prose-invert dark:text-base-300 max-w-full prose-sm md:prose-base min-w-0 ' +
        'prose-headings:scroll-m-18 prose-headings:font-display ' +
        'prose-headings:text-base-900 dark:prose-headings:text-base-50';

    const H1_CLASSES =
        'font-display text-2xl lg:text-4xl font-bold tracking-tight text-base-900 dark:text-base-50';

    const PREV_NEXT_BTN_CLASSES =
        'inline-flex gap-0.5 justify-center overflow-hidden text-sm font-medium font-display transition ' +
        'rounded-xl bg-base-200 pt-1.5 pb-1 px-3 lg:pt-2 lg:pb.15 lg:px-4 ' +
        'hover:bg-base-300/75 dark:bg-base-800/40 text-base-800 dark:text-base-400 ' +
        'ring-1 dark:ring-inset ring-base-300/75 dark:ring-base-800 ' +
        'dark:hover:bg-base-800 dark:hover:text-base-300';

    const PREV_NEXT_LABEL_CLASSES =
        'text-sm lg:text-base font-semibold text-base-700 transition ' +
        'hover:text-base-600 dark:text-base-400 dark:hover:text-base-300';

    const ARROW_PATH = 'm11.5 6.5 3 3.5m0 0-3 3.5m3-3.5h-9';

    function arrowSvg(isNext) {
        const rotate = isNext ? '' : ' rotate-180';
        const margin = isNext ? '-mr-1' : '-ml-1';
        return `<svg viewBox="0 0 20 20" fill="none" aria-hidden="true" ` +
            `class="mt-0.5 h-5 w-5 ${margin}${rotate}">` +
            `<path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" d="${ARROW_PATH}"></path>` +
            `</svg>`;
    }

    function navButton(page, isNext) {
        const label = isNext ? 'Next' : 'Previous';
        const align = isNext ? 'end' : 'start';
        const marginClass = isNext ? 'ml-auto' : '';
        const btnContent = isNext
            ? `${label}${arrowSvg(true)}`
            : `${arrowSvg(false)}${label}`;

        return `<div class="flex flex-col items-${align} ${marginClass} gap-3">` +
            `<a class="${PREV_NEXT_BTN_CLASSES}" aria-label="${label}: ${escapeHtml(page.name)}" href="${escapeHtml(page.href)}">` +
            btnContent +
            `</a>` +
            `<a tabindex="-1" aria-hidden="true" class="${PREV_NEXT_LABEL_CLASSES}" href="${escapeHtml(page.href)}">` +
            escapeHtml(page.name) +
            `</a>` +
            `</div>`;
    }

    function buildArticleInnerHtml(data) {
        let html =
            `<header class="transition-all mb-4 lg:mb-8">` +
            `<h1 class="${H1_CLASSES}">${escapeHtml(data.title)}</h1>` +
            `</header>` +
            `<main class="${PROSE_CLASSES}">` +
            data.htmlContent +
            `</main>`;

        if (data.previousPage || data.nextPage) {
            html +=
                `<div class="flex my-12 border-t border-base-200 dark:border-base-700 pt-4 lg:pt-8">` +
                (data.previousPage ? navButton(data.previousPage, false) : '') +
                (data.nextPage ? navButton(data.nextPage, true) : '') +
                `</div>`;
        }

        return html;
    }

    // --- DOM updates -----------------------------------------------------------

    function updateMetaTag(selector, value) {
        const el = document.querySelector(selector);
        if (!el) return;
        if (value) el.setAttribute('content', value);
        else el.removeAttribute('content');
    }

    function updatePage(data, url) {
        // Title
        document.title = `${siteTitle} - ${data.title}`;

        // Head meta tags
        updateMetaTag('meta[name="description"]', data.description);
        updateMetaTag('meta[property="og:description"]', data.description);
        updateMetaTag('meta[name="twitter:description"]', data.description);
        updateMetaTag('meta[property="og:title"]', data.title);
        updateMetaTag('meta[name="twitter:title"]', data.title);
        if (data.canonicalUrl) {
            updateMetaTag('meta[property="og:url"]', data.canonicalUrl);
        }

        // Article content
        const article = document.querySelector('article');
        if (article) {
            article.innerHTML = buildArticleInnerHtml(data);

            // Re-run syntax highlighting on any new code blocks
            window.pageManager?.syntaxHighlighter?.init();
        }

        // Rebuild right-sidebar outline from the new DOM headings
        const om = window.pageManager?.outlineManager;
        if (om) {
            om.sectionMap = new Map();
            om.sections = [];
            om.outlineLinks = [];
            om.init();
        }

        // Update active item in the left navigation
        document.querySelectorAll('[data-current="true"]').forEach(el => {
            el.setAttribute('data-current', 'false');
        });
        const normalized = url.pathname.replace(/\/$/, '') || '/';
        for (const a of document.querySelectorAll('nav a[href]')) {
            const href = (a.getAttribute('href') || '').replace(/\/$/, '') || '/';
            if (href === normalized) {
                a.setAttribute('data-current', 'true');
                break;
            }
        }

        // Reload stylesheet in development so any newly collected CSS classes are applied.
        // In production the href contains ?v=...; skip the unnecessary extra fetch there.
        reloadStylesheetIfDev();

        // Scroll to top (or to hash target if present)
        if (url.hash) {
            const target = document.querySelector(url.hash);
            if (target) {
                target.scrollIntoView();
                return;
            }
        }
        window.scrollTo(0, 0);
    }

    function reloadStylesheetIfDev() {
        const link = document.querySelector('link[rel="stylesheet"]');
        if (!link) return;
        // In production, App.razor adds ?v=TIMESTAMP to the href; use this as a dev signal.
        if (link.href.includes('?v=')) return;

        const url = new URL(link.href);
        url.searchParams.set('_t', Date.now());
        link.href = url.toString();
    }

    // --- navigation ------------------------------------------------------------

    async function navigate(url, pushState) {
        const slug = (url.pathname === '/' || url.pathname === '')
            ? 'index'
            : url.pathname.replace(/^\//, '');

        let data;
        try {
            const response = await fetch(`/_page-data/${slug}.json`);
            if (!response.ok) {
                window.location.href = url.href;
                return;
            }
            data = await response.json();
        } catch {
            window.location.href = url.href;
            return;
        }

        if (pushState) {
            history.pushState({ url: url.href }, data.title, url.href);
        }

        updatePage(data, url);
    }

    // --- event listeners -------------------------------------------------------

    document.addEventListener('click', (e) => {
        // Allow modifier-key clicks (new tab, etc.)
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

        const anchor = e.target.closest('a');
        if (!anchor || !isSameOriginDocLink(anchor)) return;

        e.preventDefault();
        navigate(new URL(anchor.href), true);
    });

    window.addEventListener('popstate', () => {
        navigate(new URL(window.location.href), false);
    });

})();
