---
title: "Implement Search Functionality"
description: "Add client-side search to your content site with indexing and result highlighting"
order: 2060
---

Adding search functionality to your MyLittleContentEngine site makes it easier for users to find relevant content
quickly. This guide shows you how to integrate Algolia DocSearch for powerful, client-side search capabilities.

## Prerequisites

Before implementing search, ensure you have:

- A deployed MyLittleContentEngine site
- Content that's publicly accessible - Algolia doesn't work locally
- Administrative access to configure search settings

<Steps>
<Step stepNumber="1">
## Set up Algolia DocSearch

### Create an Algolia Account

1. Visit [docsearch.algolia.com](https://docsearch.algolia.com/)
2. Click "Apply for DocSearch" if you qualify for the free tier (open source projects, technical documentation)
3. Alternatively, create a paid Algolia account at [algolia.com](https://www.algolia.com/)

### Configure Your Search Index

For free DocSearch:

1. Fill out the application form with your site details
2. Wait for approval (typically 1-2 weeks)
3. Once approved, you'll receive your search credentials
</Step>
<Step stepNumber="2">
## Add Search Scripts

MyLittleContentEngine.UI includes built-in support for DocSearch. The required JavaScript is already included in the
`scripts.js` file that's part of the `MyLittleContentEngine.UI package`.

```bash
dotnet add package MyLittleContentEngine.UI
```

Then make sure to include the UI scripts in your project:

```html
<script src="_content/MyLittleContentEngine.UI/scripts.js" defer></script>
```


</Step>
<Step stepNumber="3">
## Add Search Component to Your Layout

Add the search container to your site's header or navigation area:

```html

<div id="docsearch"
     data-search-app-id="YOUR_APP_ID"
     data-search-index-name="YOUR_INDEX_NAME"
     data-search-api-key="YOUR_SEARCH_API_KEY">
</div>
```
</Step>
<Step stepNumber="4">
## Customize Search Appearance (Optional)

`MyLittleContentEngine.MonorailCss` will automatically style the search component using your `base`, `primary` 
and `accent` colors.
 
</Step>
</Steps>

## Testing Your Search

Once you've configured DocSearch and your search index is populated:

1. **Verify Search Button**: Check that the search button appears in your layout
2. **Test Search Queries**: Try searching for content from your site
3. **Check Results**: Ensure search results link to the correct pages
4. **Mobile Testing**: Verify search works properly on mobile devices

### Troubleshooting

**Search not appearing:**
- Verify all three data attributes are set correctly
- Check browser console for JavaScript errors
- Ensure `scripts.js` is loading properly

**No search results:**
- Wait for Algolia to index your site (can take 24-48 hours)
- Verify your site is publicly accessible
- Check Algolia dashboard for indexing status


Your search functionality is now ready to help users find content quickly and efficiently!