# Services/Content/CodeAnalysis/SolutionWorkspace

MSBuild workspace management services for loading solutions, filtering projects, and managing compilation.

## Files

### ISolutionWorkspaceService.cs
- **ISolutionWorkspaceService** - Service interface for managing MSBuild workspace operations including solution loading, project filtering, and compilation

### SolutionWorkspaceService.cs
- **SolutionWorkspaceService** - Implementation of ISolutionWorkspaceService managing MSBuild workspace operations with caching, file watching, and temp build artifact management
