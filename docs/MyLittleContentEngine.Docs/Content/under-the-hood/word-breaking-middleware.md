---
title: "Word Breaking Middleware"
description: "Improve responsive design with automatic word break opportunities for long technical terms"
uid: "docs.guides.word-breaking-middleware"
order: 3590
---

Long technical terms, especially .NET identifiers and API references, can break responsive layouts by extending beyond container boundaries. MyLittleContentEngine's `WordBreakingMiddleware` automatically inserts word break opportunities (`<wbr />` tags) in long words to improve text wrapping and maintain clean layouts across all screen sizes.

## How Word Breaking Works

The WordBreakingMiddleware processes HTML responses to insert `<wbr />` (word break) tags after specific characters in long words. These invisible HTML tags tell the browser where it's acceptable to break a word if necessary for layout purposes.

### Break Characters

The middleware inserts word breaks after these characters:

- **Dots**: `.` (namespace separators, method calls)
- **Plus signs**: `+` (nested types)
- **Commas**: `,` (parameter lists)
- **Angle brackets**: `<` `>` (generic type parameters)
- **Square brackets**: `[` `]` (array notations, attributes)
- **Ampersands**: `&` (reference types)
- **Asterisks**: `*` (pointer types)
- **Backticks**: `` ` `` (generic type name suffixes)

### Example Transformations

```html
<!-- Before -->
System.Collections.Generic.Dictionary<string,List<MyNamespace.MyType>>

<!-- After -->
System.<wbr />Collections.<wbr />Generic.<wbr />Dictionary<wbr /><string,<wbr />List<wbr /><MyNamespace.<wbr />MyType>>
```

This allows the browser to break the long identifier at natural points when space is limited:

```text
System.Collections.Generic.
Dictionary<string,List<
MyNamespace.MyType>>
```


## Performance and Processing

The middleware only processes requests when:

- **GET requests**: Only HTTP GET requests are processed
- **HTML responses**: Only `text/html` content types are processed  
- **Successful responses**: Only 2xx status codes are processed
- **Long words present**: Only words meeting the minimum length requirement

## Summary

The WordBreakingMiddleware ensures your technical documentation remains readable across all device sizes while preserving the semantic meaning of your content.