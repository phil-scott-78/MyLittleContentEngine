# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# MyLittleContentEngine Development Guide

## Project Overview
MyLittleContentEngine is a .NET content management library with UI components and CSS integration. This guide outlines the project structure, development practices, and workflow for contributors.

## Project Structure

```
/MyLittleContentEngine.sln              # Main .NET solution file
/docs/                                   # Project documentation and guides
/examples/                               # Usage examples and sample implementations
├── /BasicUsage/                         # Add new examples here
├── /AdvancedScenarios/
/src/
├── /MyLittleContentEngine/              # Core content engine library
├── /MyLittleContentEngine.UI/           # Complementary UI components
├── /MyLittleContentEngine.MonorailCSS/  # Monorail CSS styling integration
/tests/
├── /MyLittleContentEngine.Tests/        # Unit tests for all components
```

## Development Commands

### Essential Commands
- `dotnet build` - Build the entire solution
- `dotnet test` - Execute all unit tests
- `dotnet test --verbosity normal` - Run tests with detailed output

### Project-Specific Commands
- `dotnet build src/MyLittleContentEngine` - Build core library only
- `dotnet test tests/MyLittleContentEngine.Tests` - Run specific test project

### AI-Friendly Testing Commands
- `dotnet test --filter "FullyQualifiedName~ServiceName"` - Test specific service class
- `dotnet test --filter "DisplayName~keyword"` - Test methods containing keyword  
- `dotnet test --logger "console;verbosity=detailed"` - Detailed test output with stack traces
- `dotnet test tests/MyLittleContentEngine.Tests/Navigation/` - Test specific folder/namespace
- `dotnet test --collect:"XPlat Code Coverage"` - Generate coverage reports
- `dotnet watch test` - Continuous testing during development

NOTE: Always use `dotnet` commands for building and testing against the solution and project. 
NEVER try to run individual files directly, NEVER try and test individual files, and NEVER try to run single-file execution.

## Code Standards & Style Guide

### Language Features
- **Target Framework**: Use features up to C# 12
- **String Handling**: Prefer raw strings (`"""multiline content"""`) over verbatim strings (`@"content"`) for multi-line text
- **Modern C# Patterns**: Utilize pattern matching, record types, and nullable reference types where appropriate

### Code Quality Standards
- Write unit tests for all public APIs
- Use meaningful variable and method names
- Follow standard .NET naming conventions (PascalCase for public members, camelCase for private)
- Document public APIs with XML comments

### File Organization
- Place interfaces in separate files when they have multiple implementations
- Group related functionality in logical namespaces
- Keep test files parallel to source structure

### Documentation 
- Documentation uses the Diátaxis framework (https://diataxis.fr/)
   - Tutorials are in the /getting-started/ directory
   - How-to guides are in the /guides/ directory
   - Explanations are in the /under-the-hood/ directory
   - Reference documentation is in the /reference/ directory
- Ensure all documentation has YML front matter structure that matches existing content
 

## Development Workflow

### Before Starting
1. Clone the repository and restore dependencies: `dotnet restore`
2. Verify build: `dotnet build`
3. Confirm tests pass: `dotnet test`

### Making Changes
1. **Create Feature Branch**: Work on dedicated branches for features/fixes
2. **Implement Changes**: Follow code standards outlined above
3. **Write Tests**: Add or update unit tests for new functionality
4. **Build & Test**: Always run both commands after completing changes:
   ```bash
   dotnet build
   dotnet test
   ```
5. **Document**: Update relevant documentation in `/docs` if needed

### Common Pitfalls to Avoid
- **Never manually add global usings** - The project configuration handles this automatically
- **Don't modify AssemblyInfo directly** - Use project properties instead
- **Avoid single-file execution attempts** - Always use `dotnet build` or `dotnet test`
- **Don't skip testing** - Ensure all tests pass before committing

### Adding New Examples
- Place new example projects in `/examples/`
- Include a README explaining the example's purpose
- Ensure examples build successfully with the main solution

### Troubleshooting Build Issues
- Clean and rebuild: `dotnet clean && dotnet build`
- Check for missing dependencies: `dotnet restore`
- Review error messages for namespace or dependency conflicts

## Integration Points

### UI Components
- MyLittleContentEngine.UI provides reusable components
- Follow established patterns for theming and customization

## Testing Strategy

### Testing Approach
- **ALWAYS write unit tests instead of console apps** for testing functionality
- Unit tests should cover core business logic
- Example projects serve as functional tests
- Aim for high code coverage on critical paths

### Testing Infrastructure
The test project includes several helper utilities to simplify test creation:

#### ContentEngineTestBuilder
Fluent builder for creating integration test scenarios:

```csharp
// Basic usage
var builder = new ContentEngineTestBuilder()
    .WithMarkdownFiles(("/content/test.md", "# Test\nContent here"))
    .WithContentOptions(opts => opts.ContentPath = "/content");

var contentService = await builder.BuildContentServiceAsync();
var fileSystem = builder.GetFileSystem();

// Pre-configured sample content
var builder = ContentEngineTestBuilder.WithSampleContent();
```

#### MarkdownTestData
Pre-built markdown samples for common scenarios:

```csharp
// Use predefined samples
var richPost = MarkdownTestData.RichPost;  // Complex post with tags, dates
var simplePost = MarkdownTestData.SimplePost;  // Basic post
var draftPost = MarkdownTestData.DraftPost;  // Draft content

// Get all sample files  
var allSamples = MarkdownTestData.SampleFiles;
var publishedOnly = MarkdownTestData.PublishedFiles;

// Create custom posts
var custom = MarkdownTestData.CreatePost("Title", 1, "# Content", ["tag1", "tag2"]);
```

#### ServiceMockFactory
Common service mock configurations:

```csharp
// Create mock content service with pages
var mockService = ServiceMockFactory.CreateContentService(
    ("Home", "/", 1),
    ("About", "/about", 2)
);

// Create empty service
var emptyService = ServiceMockFactory.CreateEmptyContentService();

// Create with static content
var staticService = ServiceMockFactory.CreateContentServiceWithStaticContent(
    new ContentToCopy("source.css", "dest.css")
);

// Helper builders
var page = ServiceMockFactory.PageBuilder.Create("Title", "/url", 1);
var richPage = ServiceMockFactory.PageBuilder.CreateRich("Title", "/url", 1, ["tag"], false);
```

### Test Data Creation Patterns
```csharp
// Use MockFileSystem for file operations
var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
{
    { "/content/test.md", new MockFileData(MarkdownTestData.SimplePost) }
});

// Use ServiceMockFactory for mocking IContentService
var mockContentService = ServiceMockFactory.CreateContentService(("Test", "/test", 1));
```

### Test Organization Rules
- Mirror source structure in test project
- One test class per service/class being tested  
- Group related tests with nested classes or clear naming
- Use TestHelpers utilities to reduce boilerplate

## Contributing Guidelines
- Follow the established project structure
- Maintain backward compatibility when possible
- Update documentation for public API changes
- Add examples for significant new features