/**
 * SPA Navigation for DocSite
 *
 * First page load: full static HTML (normal browser behaviour).
 * Subsequent in-site navigation: intercept link clicks, fetch the pre-generated
 * /_page-data/{url}.json, and rehydrate the page content without a full reload.
 *
 * Two-phase transition strategy:
 *   Phase 1 — fade out current content (FADE_OUT_MS) while the fetch runs in parallel.
 *   Phase 2a — if data arrived before/during the fade-out, inject it immediately (fast/cached).
 *   Phase 2b — if still loading, show a skeleton with the page title; inject real content
 *              when the fetch completes, then fade in.
 *
 * Falls back to a full page load when no page-data file is available
 * (e.g. API reference pages, custom Razor pages).
 */
(function () {
    'use strict';

    // Inject the skeleton shimmer keyframe once.
    const _s = document.createElement('style');
    _s.textContent =
        '@keyframes spa-shimmer{0%{background-position:200% 0}100%{background-position:-200% 0}}';
    document.head.appendChild(_s);

    // ---------------------------------------------------------------------------
    // Constants
    // ---------------------------------------------------------------------------

    const FADE_OUT_MS = 50;
    const FADE_IN_MS  = 180;

    const PROSE_CLASSES =
        'prose dark:prose-invert dark:text-base-300 max-w-full prose-sm md:prose-base min-w-0 ' +
        'prose-headings:scroll-m-18 prose-headings:font-display ' +
        'prose-headings:text-base-900 dark:prose-headings:text-base-50';

    const H1_CLASSES =
        'font-display text-2xl lg:text-4xl font-bold tracking-tight text-base-900 dark:text-base-50';

    const PREV_NEXT_BTN =
        'inline-flex gap-0.5 justify-center overflow-hidden text-sm font-medium font-display transition ' +
        'rounded-xl bg-base-200 pt-1.5 pb-1 px-3 lg:pt-2 lg:pb.15 lg:px-4 ' +
        'hover:bg-base-300/75 dark:bg-base-800/40 text-base-800 dark:text-base-400 ' +
        'ring-1 dark:ring-inset ring-base-300/75 dark:ring-base-800 ' +
        'dark:hover:bg-base-800 dark:hover:text-base-300';

    const PREV_NEXT_LBL =
        'text-sm lg:text-base font-semibold text-base-700 transition ' +
        'hover:text-base-600 dark:text-base-400 dark:hover:text-base-300';

    // Derive site title once ("SiteTitle - PageTitle" → "SiteTitle")
    const _dash = document.title.indexOf(' - ');
    const SITE_TITLE = _dash > -1 ? document.title.substring(0, _dash) : document.title;

    // Base path prefix for subdirectory deployments (e.g. "/mybase"). Empty in dev.
    const BASE_PATH = (document.documentElement.dataset.basePath || '').replace(/\/$/, '');

    // Generation counter prevents stale style-cleanup timeouts from clobbering
    // a navigation that started after the one that set the timeout.
    let _gen = 0;

    // ---------------------------------------------------------------------------
    // Utilities
    // ---------------------------------------------------------------------------

    const delay = (ms) => new Promise(r => setTimeout(r, ms));

    function escapeHtml(s) {
        return (s || '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    /** Extract the best available pending title from a clicked anchor. */
    function pendingTitleFrom(anchor) {
        // Prev/next buttons carry the real page title in aria-label ("Previous: Page Title")
        const aria = anchor.getAttribute('aria-label') || '';
        const m = aria.match(/^(?:Previous|Next): (.+)$/);
        return m ? m[1] : anchor.textContent.trim();
    }

    // ---------------------------------------------------------------------------
    // Link filtering
    // ---------------------------------------------------------------------------

    function isSameOriginDocLink(anchor) {
        if (!anchor.href) return false;
        // Hash-only links (outline / in-page TOC) should scroll natively, not trigger SPA nav.
        if ((anchor.getAttribute('href') || '').startsWith('#')) return false;
        try {
            const url = new URL(anchor.href);
            if (url.origin !== window.location.origin) return false;
            if (url.pathname.includes('/_page-data/')) return false;
            if (url.pathname === window.location.pathname && url.hash) return false;
            return !(anchor.target === '_blank' || anchor.hasAttribute('download'));
            
        } catch { return false; }
    }

    // ---------------------------------------------------------------------------
    // Article HTML builders
    // ---------------------------------------------------------------------------

    function arrowSvg(isNext) {
        const cls = isNext ? 'mt-0.5 h-5 w-5 -mr-1' : 'mt-0.5 h-5 w-5 -ml-1 rotate-180';
        return `<svg viewBox="0 0 20 20" fill="none" aria-hidden="true" class="${cls}">` +
            `<path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" ` +
            `d="m11.5 6.5 3 3.5m0 0-3 3.5m3-3.5h-9"></path></svg>`;
    }

    function navButton(page, isNext) {
        const lbl = isNext ? 'Next' : 'Previous';
        const content = isNext ? lbl + arrowSvg(true) : arrowSvg(false) + lbl;
        return `<div class="flex flex-col items-${isNext ? 'end' : 'start'} ${isNext ? 'ml-auto' : ''} gap-3">` +
            `<a class="${PREV_NEXT_BTN}" aria-label="${lbl}: ${escapeHtml(page.name)}" href="${escapeHtml(page.href)}">${content}</a>` +
            `<a tabindex="-1" aria-hidden="true" class="${PREV_NEXT_LBL}" href="${escapeHtml(page.href)}">${escapeHtml(page.name)}</a>` +
            `</div>`;
    }

    function buildArticleBodyHtml(data) {
        let html = `<main class="${PROSE_CLASSES}">${data.htmlContent}</main>`;
        if (data.previousPage || data.nextPage) {
            html +=
                `<div class="flex my-12 border-t border-base-200 dark:border-base-700 pt-4 lg:pt-8">` +
                (data.previousPage ? navButton(data.previousPage, false) : '') +
                (data.nextPage    ? navButton(data.nextPage,     true)  : '') +
                `</div>`;
        }
        return html;
    }

    function buildArticleHtml(data) {
        return `<header class="transition-all mb-4 lg:mb-8">` +
            `<h1 class="${H1_CLASSES}">${escapeHtml(data.title)}</h1>` +
            `</header>` +
            buildArticleBodyHtml(data);
    }

    /** Skeleton shown while a slow page is loading.  Title is real; content is shimmer lines. */
    function buildSkeletonHtml(title) {
        const line = (w) =>
            `<div style="height:.875rem;width:${w}%;border-radius:.375rem;margin-bottom:.75rem;` +
            `background:linear-gradient(90deg,rgba(128,128,128,.1) 25%,rgba(128,128,128,.2) 50%,rgba(128,128,128,.1) 75%);` +
            `background-size:200% 100%;animation:spa-shimmer 1.4s ease-in-out infinite"></div>`;
        return `<header class="transition-all mb-4 lg:mb-8">` +
            `<h1 class="${H1_CLASSES}">${escapeHtml(title)}</h1></header>` +
            `<div data-role="skeleton-body" class="mt-8">` +
            line(88) + line(72) + line(80) +
            `<div style="height:1.5rem"></div>` +
            line(92) + line(65) + line(78) + line(55) +
            `</div>`;
    }

    // ---------------------------------------------------------------------------
    // Transitions — inline styles give precise control over the fade sequence:
    //   fadeOut  →  opacity held at 0  →  innerHTML replaced  →  fadeIn
    // ---------------------------------------------------------------------------

    async function fadeOut(...els) {
        els.filter(Boolean).forEach(el => {
            el.style.transition    = `opacity ${FADE_OUT_MS}ms ease-out`;
            el.style.pointerEvents = 'none';
            el.style.opacity       = '0';
        });
        return delay(FADE_OUT_MS);
        // opacity intentionally left at '0' so there is no flash when innerHTML is replaced
    }

    function fadeIn(...els) {
        els = els.filter(Boolean);
        const gen = ++_gen;
        // Start from translateY(6px) so the content appears to lift in slightly
        els.forEach(el => {
            el.style.transition = '';
            el.style.transform  = 'translateY(6px)';
        });
        void els[0]?.offsetWidth; // force reflow to register the starting state for all elements
        els.forEach(el => {
            el.style.transition    = `opacity ${FADE_IN_MS}ms ease-out, transform ${FADE_IN_MS}ms ease-out`;
            el.style.opacity       = '1';
            el.style.transform     = 'translateY(0)';
            el.style.pointerEvents = '';
        });
        // Clean up after the animation unless a newer navigation has started
        setTimeout(() => {
            if (_gen !== gen) return;
            els.forEach(el => {
                el.style.transition = '';
                el.style.transform  = '';
                el.style.opacity    = '';
            });
        }, FADE_IN_MS + 60);
    }

    // ---------------------------------------------------------------------------
    // DOM helpers
    // ---------------------------------------------------------------------------

    function applyMeta(data) {
        document.title = `${SITE_TITLE} - ${data.title}`;
        const set = (sel, val) => {
            const el = document.querySelector(sel);
            if (!el) return;
            val ? el.setAttribute('content', val) : el.removeAttribute('content');
        };
        set('meta[name="description"]',          data.description);
        set('meta[property="og:description"]',   data.description);
        set('meta[name="twitter:description"]',  data.description);
        set('meta[property="og:title"]',         data.title);
        set('meta[name="twitter:title"]',        data.title);
        if (data.canonicalUrl) set('meta[property="og:url"]', data.canonicalUrl);
    }

    function rebuildOutline() {
        const om = window.pageManager?.outlineManager;
        if (!om) return;
        om.sectionMap = new Map();
        om.sections   = [];
        om.outlineLinks = [];
        om.init();
    }

    function updateNavActive(pathname) {
        document.querySelectorAll('[data-current="true"]')
            .forEach(el => el.setAttribute('data-current', 'false'));
        const norm = pathname.replace(/\/$/, '') || '/';
        for (const a of document.querySelectorAll('nav a[href]')) {
            if ((a.getAttribute('href') || '').replace(/\/$/, '') === norm) {
                a.setAttribute('data-current', 'true');
                break;
            }
        }
    }

    function reloadStylesheetIfDev() {
        // MonorailCSS only generates CSS dynamically on a live dev server.
        // Deployed static sites have pre-built CSS — no reload needed or wanted.
        if (location.hostname !== 'localhost' && location.hostname !== '127.0.0.1') return;
        const link = document.querySelector('link[rel="stylesheet"]');
        if (!link) return;
        const u = new URL(link.href);
        u.searchParams.set('_t', Date.now());
        link.href = u.toString();
    }

    // ---------------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------------

    let _navigating = false;
    // Tracks the pathname of the page currently rendered in the article, so the
    // popstate handler can tell hash-only history entries apart from real page changes.
    let _currentPathname = window.location.pathname;

    function getSlug(url) {
        let p = url.pathname;
        if (BASE_PATH && p.startsWith(BASE_PATH)) p = p.substring(BASE_PATH.length) || '/';
        return (!p || p === '/') ? 'index' : p.replace(/^\//, '');
    }

    /** Apply navigation state — history, meta tags, nav highlight, syntax, stylesheet. */
    function applyNavigationState(data, url, pushState) {
        _currentPathname = url.pathname;
        if (pushState) history.pushState({ title: data.title }, data.title, url.href);
        applyMeta(data);
        updateNavActive(url.pathname);
        window.pageManager?.syntaxHighlighter?.init();
        reloadStylesheetIfDev();
    }

    /** Commit data to the DOM and finish navigation. */
    function commit(article, data, url, pushState, outlineEl) {
        applyNavigationState(data, url, pushState);
        article.innerHTML = buildArticleHtml(data);
        rebuildOutline();
        fadeIn(article, outlineEl);
        if (url.hash) {
            const t = document.querySelector(url.hash);
            if (t) { t.scrollIntoView(); return; }
        }
        window.scrollTo(0, 0);
    }

    async function navigate(url, pushState, hint) {
        if (_navigating) return;
        _navigating = true;

        const article = document.querySelector('article');
        if (!article) { window.location.href = url.href; _navigating = false; return; }

        // Track whether the fetch settled during the fade-out.
        let fetchDone = false;
        let fetchData = null;
        let fetchFail = false;

        const dataPromise = fetch(`${BASE_PATH}/_page-data/${getSlug(url)}.json`)
            .then(r => { if (!r.ok) throw new Error(r.status); return r.json(); })
            .then(d  => { fetchDone = true; fetchData = d; })
            .catch(() => { fetchDone = true; fetchFail = true; });

        // ── Phase 1: fade out current content while the fetch runs in parallel ──
        const outlineEl = document.querySelector('[data-role="page-outline"]');
        await fadeOut(article, outlineEl);

        if (fetchFail) { window.location.href = url.href; _navigating = false; return; }

        if (fetchDone) {
            // ── Phase 2a: data arrived in time — seamless, no skeleton needed ──
            commit(article, fetchData, url, pushState, outlineEl);
        } else {
            // ── Phase 2b: still loading — show skeleton with the page title ──
            // Both article and outline are already at opacity 0 from Phase 1 fade-out.
            // Clear the outline UL while it's invisible, scroll to top, show skeleton.
            window.scrollTo(0, 0);
            const _ol = outlineEl?.querySelector('ul');
            if (_ol) _ol.innerHTML = '';
            article.innerHTML = buildSkeletonHtml(hint || '');
            fadeIn(article); // outline stays faded — nothing to show until real content arrives

            await dataPromise;          // wait for the rest of the fetch

            if (fetchFail) { window.location.href = url.href; _navigating = false; return; }

            // Cancel any in-progress article fade-in; ensure article is fully visible
            article.style.transition = article.style.transform = '';
            article.style.opacity = '1';
            ++_gen; // invalidate previous fadeIn cleanup timer

            // Fade out only the shimmer block, leaving the title untouched
            const skeletonBody = article.querySelector('[data-role="skeleton-body"]');
            await fadeOut(skeletonBody);
            skeletonBody?.remove();

            // Update title in case the hint differed from the real page title
            const h1 = article.querySelector('h1');
            if (h1) h1.textContent = fetchData.title;

            // Insert real body elements pre-hidden so fadeIn can animate them in
            const tmp = document.createElement('div');
            tmp.innerHTML = buildArticleBodyHtml(fetchData);
            [...tmp.children].forEach(el => { el.style.opacity = '0'; });
            while (tmp.firstChild) article.appendChild(tmp.firstChild);

            applyNavigationState(fetchData, url, pushState);

            // Rebuild outline (needs <main> in DOM first)
            rebuildOutline();

            // Fade in new content elements + TOC together
            const newMain = article.querySelector('main');
            const newNav  = article.querySelector('.flex.my-12');
            fadeIn(newMain, newNav, outlineEl);

            if (url.hash) {
                const t = document.querySelector(url.hash);
                if (t) { t.scrollIntoView(); _navigating = false; return; }
            }
        }

        _navigating = false;
    }

    // ---------------------------------------------------------------------------
    // Event listeners
    // ---------------------------------------------------------------------------

    document.addEventListener('click', (e) => {
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        const anchor = e.target.closest('a');
        if (!anchor || !isSameOriginDocLink(anchor)) return;
        e.preventDefault();
        navigate(new URL(anchor.href), true, pendingTitleFrom(anchor));
    });

    window.addEventListener('popstate', (e) => {
        const url = new URL(window.location.href);
        // Hash-only history entries (from in-page anchor clicks) share the same
        // pathname — let the browser handle scrolling rather than reloading the page.
        if (url.pathname === _currentPathname) return;
        navigate(url, false, e.state?.title || '');
    });

})();
