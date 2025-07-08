namespace MyLittleContentEngine.Models;

/// <summary>
/// Content that will be copied to the output during a static build.
/// </summary>
/// <param name="SourcePath"></param>
/// <param name="TargetPath"></param>
public record ContentToCopy(string SourcePath, string TargetPath);