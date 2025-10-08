# Services/Content/CodeAnalysis/SymbolAnalysis

Symbol extraction and analysis services for working with Roslyn symbols, extracting code fragments, and managing symbol information.

## Files

### CodeFragmentExtractor.cs
- **CodeFragmentExtractor** - Internal static utility for extracting code fragments from documents with support for extracting method/class bodies only

### ISymbolExtractionService.cs
- **ISymbolExtractionService** - Service interface for extracting and analyzing symbols from Roslyn solutions with cache management
- **SymbolInfo** - Record containing information about a symbol including its Roslyn symbol, document, syntax node, and location details

### SymbolExtractionService.cs
- **SymbolExtractionService** - Implementation of ISymbolExtractionService that extracts symbols from Roslyn solutions using parallel processing and lazy loading
