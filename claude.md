# CLAUDE.md

MyLittleContentEngine is a .NET content management library with UI components and CSS integration.

## Project Structure
- `src/MyLittleContentEngine/` - Core content engine library
- `src/MyLittleContentEngine.UI/` - UI components  
- `src/MyLittleContentEngine.MonorailCSS/` - CSS styling integration
- `tests/MyLittleContentEngine.Tests/` - Unit tests
- `docs/` - Documentation (uses Diátaxis framework)
- `examples/` - Usage examples

## Development Commands
- `dotnet build` - Build entire solution
- `dotnet test` - Run all tests  
- `dotnet test --verbosity normal` - Detailed test output
- `dotnet test --filter "FullyQualifiedName~ServiceName"` - Test specific service
- `dotnet test --logger "console;verbosity=detailed"` - Stack traces

**Important**: Always use `dotnet` commands. Never run individual files directly.

## Code Standards
- **C# 12** features, prefer raw strings (`"""content"""`) over verbatim strings
- **Standard .NET conventions**: PascalCase public, camelCase private
- **Always write unit tests** for public APIs
- **XML comments** for public APIs
- **Keep test files parallel** to source structure
 

## Development Workflow
1. **Setup**: `dotnet restore`, `dotnet build`, `dotnet test`
2. **Make Changes**: Create feature branch, implement, write tests
3. **Verify**: Always run `dotnet build` and `dotnet test` before committing


## Testing Helpers
**Always write unit tests** instead of console apps for testing functionality.

### Key Test Utilities
- **ContentEngineTestBuilder**: Fluent builder for integration tests
  ```csharp
  var builder = new ContentEngineTestBuilder()
      .WithMarkdownFiles(("/content/test.md", "# Test\nContent here"));
  ```
- **MarkdownTestData**: Pre-built samples (`RichPost`, `SimplePost`, `DraftPost`)
- **ServiceMockFactory**: Mock content services
  ```csharp
  var mockService = ServiceMockFactory.CreateContentService(("Home", "/", 1));
  ```