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
- Unit tests should cover core business logic
- Example projects serve as functional tests
- Aim for high code coverage on critical paths

## Contributing Guidelines
- Follow the established project structure
- Maintain backward compatibility when possible
- Update documentation for public API changes
- Add examples for significant new features