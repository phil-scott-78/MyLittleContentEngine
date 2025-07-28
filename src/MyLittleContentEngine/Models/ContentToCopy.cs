namespace MyLittleContentEngine.Models;

/// <summary>
/// Content that will be copied to the output during a static build.
/// </summary>
/// <param name="SourcePath">The source path to copy from</param>
/// <param name="TargetPath">The target path to copy to</param>
/// <param name="ExcludedExtensions">File extensions to exclude during copy operations (e.g., [".md", ".txt"]). If null, no extensions are excluded.</param>
public record ContentToCopy(string SourcePath, string TargetPath, string[]? ExcludedExtensions = null);