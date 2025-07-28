# Integration Tests Implementation Plan

## Project Overview
Create comprehensive integration tests for MyLittleContentEngine that:
1. **Docs Site Testing**: Launch and test the documentation site with full functionality verification
2. **Example Projects Testing**: Basic smoke tests for all example projects to ensure they start and function properly
3. **Shared Infrastructure**: Reusable test infrastructure for launching ASP.NET Core applications

## Testing Philosophy
**Keep tests basic and non-rigid on content.** For now, checking a URL for:
- 200 response status
- Expected title or other known content markers
- Basic functionality (not detailed content validation)

Use `[Theory]` with data-driven tests to load up lists of URLs with expected content so we don't need tons of `[Fact]` methods per project.

## Analysis Results

### Docs Project (`docs/MyLittleContentEngine.Docs/`)
- **Framework**: DocSite-based ASP.NET Core application
- **Launch**: Uses `RunDocSiteAsync(args)` method
- **Key Features**: API documentation generation, search, navigation, content rendering
- **Test Scope**: Basic functionality verification, not content validation

### Example Projects Analyzed
1. **MinimalExample**: Basic content engine with static content service
2. **BlogExample**: BlogSite implementation with complex configuration
3. **RecipeExample**: Custom content service with image processing
4. **ApiReferenceExample**: API documentation generation
5. **RoslynIntegrationExample**: Roslyn-based code highlighting
6. **SearchExample**: Search functionality demonstration
7. **UserInterfaceExample**: UI components showcase
8. **MultipleContentSourceExample**: Multiple content sources
9. **SingleFileApp**: Single-file application

## Implementation Plan

### ✅ Phase 1: Infrastructure Setup - COMPLETED
1. **✅ Project Structure - IMPLEMENTED**
   ```
   tests/MyLittleContentEngine.IntegrationTests/
   ├── Infrastructure/
   │   ├── WebApplicationTestFactory.cs      # ✅ Base test factory
   │   ├── TestServerExtensions.cs           # ✅ HTTP client helpers
   │   └── TestUrlData.cs                    # URL test data provider
   ├── DocsSite/
   │   ├── DocsWebApplicationFactory.cs      # ✅ Docs-specific factory
   │   └── DocsUrlTests.cs                   # ✅ Theory-based URL tests
   ├── ExampleProjects/
   │   └── ExampleProjectUrlTests.cs         # Theory-based smoke tests
   └── TestConfiguration/
       └── TestHelpers.cs
   ```

2. **✅ Shared Test Infrastructure - IMPLEMENTED**
   - ✅ `WebApplicationFactory`-based test setup
   - ✅ HTTP client configuration for testing
   - ✅ Test server startup and teardown
   - ✅ Content path configuration (solved DocSiteOptions override)
   - ✅ Program class accessibility (added public partial class)
   - ✅ URL test data providers

### ✅ Phase 2: Theory-Based URL Testing - IMPLEMENTED & WORKING
**✅ Simple, data-driven tests for basic functionality:**

```csharp
public class DocsUrlTests : IClassFixture<DocsWebApplicationFactory>
{
    [Theory]
    [MemberData(nameof(GetDocsUrlTestData))]
    public async Task DocsUrls_ShouldReturnSuccessWithExpectedContent(
        string url, string expectedContent)
    {
        // ✅ IMPLEMENTED: GET url, check 200 + content markers
        var response = await _client.GetAsync(url);
        await response.ShouldReturnSuccessWithContent(expectedContent);
    }
    
    public static IEnumerable<object[]> GetDocsUrlTestData()
    {
        // ✅ WORKING: 3 test cases passing
        yield return new object[] { "/", "My Little Content Engine" };
        yield return new object[] { "/getting-started/creating-first-site", "Creating" };
        yield return new object[] { "/api", "API Reference" };
    }
}
```

**✅ CURRENT STATUS: All 4 tests passing (3 docs + 1 framework test)**
**✅ KEY ACHIEVEMENT: Solved DocSiteOptions content path configuration using record `with` syntax**

### Phase 3: Example Project Smoke Tests
**Basic startup and accessibility verification:**

```csharp
public class ExampleProjectUrlTests
{
    [Theory]
    [MemberData(nameof(GetExampleProjectTestData))]
    public async Task ExampleProject_ShouldStartAndServeBasicContent(
        Type programType, string url, string expectedContent)
    {
        // Launch example, check basic URL works
    }
    
    public static IEnumerable<object[]> GetExampleProjectTestData()
    {
        yield return new object[] { typeof(MinimalExample.Program), "/", "My Little Content Engine" };
        yield return new object[] { typeof(BlogExample.Program), "/", "Calvin's Chewing Chronicles" };
        yield return new object[] { typeof(RecipeExample.Program), "/", "Recipe Collection" };
        // etc.
    }
}
```

## Technical Implementation Details

### Test Infrastructure Components

1. **WebApplicationTestFactory**
   ```csharp
   public abstract class WebApplicationTestFactory<TProgram> : WebApplicationFactory<TProgram>
       where TProgram : class
   {
       // Common configuration for all test scenarios
       // Simple environment setup, minimal overrides
   }
   ```

2. **HTTP Client Helpers**
   ```csharp
   public static class TestServerExtensions
   {
       public static async Task<bool> ShouldHaveContent(this HttpResponseMessage response, string expectedContent)
       {
           response.EnsureSuccessStatusCode();
           var content = await response.Content.ReadAsStringAsync();
           return content.Contains(expectedContent, StringComparison.OrdinalIgnoreCase);
       }
   }
   ```

3. **URL Test Data Management**
   - Static methods returning test data
   - Simple data structures for URL + expected content
   - Minimal configuration, focus on smoke testing

### Example Test Pattern
```csharp
[Theory]
[InlineData("/", "Expected Title")]
[InlineData("/about", "About")]
[InlineData("/search", "Search")]
public async Task Url_ShouldReturnSuccessWithTitle(string url, string expectedTitle)
{
    // Arrange
    var response = await _client.GetAsync(url);
    
    // Act & Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains(expectedTitle, content);
}
```

## Configuration Requirements

### Test Project Configuration
- Target same .NET version as main projects
- Minimal package references (just what's needed)
- Simple test runner configuration
- Environment variables for test isolation

### Benefits of This Approach

1. **Simple & Fast**: Basic checks, not content validation
2. **Data-Driven**: Theory tests reduce code duplication
3. **Smoke Testing**: Verifies applications start and serve content
4. **Maintainable**: Easy to add new URLs without new test methods
5. **Non-Brittle**: Doesn't break on content changes

## Execution Strategy

1. **Start Simple**: Basic URL + 200 response tests
2. **Use Theories**: Data-driven tests for multiple URLs
3. **Focus on Smoke**: Application starts, basic content serves
4. **Avoid Content Rigidity**: Check for presence, not exact content

This approach provides reliable integration testing without being fragile or overly complex.