/**
 * DocSite SPA Integration
 *
 * Bridges the generic spa-engine.js with DocSite's interactive features.
 * Listens for spa:commit / spa:before-navigate events and reinitialises
 * content managers, the page outline, navigation highlighting, and meta tags.
 */
(function () {
    'use strict';

    // -------------------------------------------------------------------
    // Before navigation — clear the outline while content is loading
    // -------------------------------------------------------------------

    document.addEventListener('spa:before-navigate', () => {
        const outlineUl = document.querySelector('[data-role="page-outline"] ul');
        if (outlineUl) outlineUl.innerHTML = '';
    });

    // -------------------------------------------------------------------
    // After commit — reinitialise everything
    // -------------------------------------------------------------------

    document.addEventListener('spa:commit', (e) => {
        const { url, data } = e.detail;
        const pm = window.pageManager;
        if (!pm) return;

        // 1. Content managers
        pm.syntaxHighlighter?.init();
        pm.tabManager?.init();
        pm.mermaidManager?.init();

        // 2. Outline (OutlineManager reads headings from the article DOM)
        const om = pm.outlineManager;
        if (om) {
            om.sectionMap = new Map();
            om.sections = [];
            om.outlineLinks = [];
            om.init();
        }

        // 3. Navigation active-link highlighting
        const pathname = url.pathname;
        document.querySelectorAll('[data-current="true"]')
            .forEach(el => el.setAttribute('data-current', 'false'));
        const norm = pathname.replace(/\/$/, '') || '/';
        for (const a of document.querySelectorAll('nav a[href]')) {
            if ((a.getAttribute('href') || '').replace(/\/$/, '') === norm) {
                a.setAttribute('data-current', 'true');
                break;
            }
        }

        // 4. Extended meta tags (spa-engine handles title + description;
        //    DocSite also needs og:title, twitter:*, og:url)
        const set = (sel, val) => {
            const el = document.querySelector(sel);
            if (!el) return;
            val ? el.setAttribute('content', val) : el.removeAttribute('content');
        };
        set('meta[property="og:title"]', data.title);
        set('meta[name="twitter:title"]', data.title);
        set('meta[name="twitter:description"]', data.description);

        // 5. Dev stylesheet reload (MonorailCSS regenerates on change)
        if (location.hostname === 'localhost' || location.hostname === '127.0.0.1') {
            const link = document.querySelector('link[rel="stylesheet"]');
            if (link) {
                const u = new URL(link.href);
                u.searchParams.set('_t', Date.now());
                const next = link.cloneNode();
                next.href = u.toString();
                next.onload = () => link.remove();
                link.after(next);
            }
        }
    });
})();
