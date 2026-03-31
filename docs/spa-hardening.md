# SPA Razor Island — Hardening Punch List

Tracked issues and improvements for the SPA navigation feature, ordered by user impact.

---

## 1. Stale islands when a renderer returns `null`

**Status**: Done
**Severity**: Bug
**Files**: `spa-engine.js` (`commitIslands`), possibly `SpaPageDataService.cs`

When an `ISpaIslandRenderer` returns `null` (e.g. `RecipeInfoSlotRenderer` on the index page), that island key is omitted from the JSON envelope. On the client side, `commitIslands()` only writes keys present in the data — so islands absent from the JSON keep their previous content.

This is masked on the slow path because `showLoadingState()` clears/skeletonizes the island before the data arrives. On the fast path (most navigations), `showLoadingState` is never called, so stale content remains visible.

**Repro**: Navigate from pasta-carbonara to index quickly. The recipe-info sidebar keeps showing carbonara's card.

**Fix**: After writing present islands, `commitIslands` should clear any island element that exists in the DOM but is absent from the JSON response.

---

## 2. Scroll position not restored on back/forward

**Status**: Done
**Severity**: UX regression vs. native browser behavior
**Files**: `spa-engine.js` (`navigate`, `popstate` handler, `scrollToTarget`)

The `popstate` handler calls `navigate()` which calls `scrollToTarget()` which always scrolls to `(0, 0)`. Native browser behavior restores scroll position on back/forward — SPA navigation breaks that contract.

**Repro**: Scroll halfway through a long recipe, click to another page, hit Back. You land at the top instead of where you were.

**Fix**: Store `window.scrollY` in `history.state` before each navigation. On `popstate`, restore scroll position from `event.state` instead of scrolling to top.

---

## 3. Rapid clicks are silently dropped

**Status**: Done
**Severity**: UX — users must click again after in-flight navigation finishes
**Files**: `spa-engine.js` (`navigate`)

The `_navigating` boolean guard silently swallows clicks while a navigation is in flight. If a user clicks link A then immediately clicks link B, B is dropped. The expected behavior is that B cancels A.

**Fix**: Replace the boolean guard with an `AbortController`. When a new navigation starts, abort the in-flight fetch. Use a sequence counter or controller reference so only the latest navigation commits.

---

## 4. No accessibility announcements after navigation

**Status**: Done
**Severity**: Accessibility gap
**Files**: `spa-engine.js` (after `commitIslands`)

After a SPA content swap, screen readers receive no notification that the page changed. There is no focus management and no ARIA live region announcement. Content sites (docs, blogs, recipes) are exactly the kind of sites that should be accessible.

**Fix options** (do both):
- After `commitIslands`, move focus to the main content island or a visually-hidden heading inside it
- Add a visually-hidden ARIA live region that announces the new page title
- This matches what Gatsby, Next.js, and Astro do

---

## 5. Prefetching on hover

**Status**: Done
**Severity**: Enhancement — perceived performance
**Files**: `spa-engine.js` (new section)

For static content sites where JSON payloads are tiny and pre-built, prefetching on hover would make navigation feel instant nearly all the time. The skeleton system handles genuinely slow loads well, but most navigations could skip it entirely.

**Implementation**:
- Listen for `mouseenter`/`pointerenter` on SPA-eligible links
- Fire a low-priority `fetch()` and cache the result
- `navigate()` checks the cache before fetching
- Cap the cache size or use a simple LRU to avoid unbounded memory growth
- Consider `<link rel="prefetch">` as an alternative for broader browser support

Pairs naturally with #3 since both touch the fetch lifecycle.

---

## 6. `og:url` and `<link rel="canonical">` not updated

**Status**: Done
**Severity**: Low — crawlers hit static HTML, but completes the meta update story
**Files**: `spa-engine.js` (`applyMeta`)

`applyMeta()` updates title, description, OG title/description, and Twitter cards but not `og:url` or `<link rel="canonical">`. One-liner to fix.

**Fix**: Add to `applyMeta`:
```js
setMeta('meta[property="og:url"]', url.href);
const canon = document.querySelector('link[rel="canonical"]');
if (canon) canon.href = url.href;
```
