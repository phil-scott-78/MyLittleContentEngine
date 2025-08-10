using MyLittleContentEngine.Services;

namespace MyLittleContentEngine.Models;

/// <summary>
/// Content that will be written to the output during a static build.
/// </summary>
/// <param name="TargetPath">The target file path to write to</param>
/// <param name="Bytes">The content bytes to write</param>
public record ContentToCreate(FilePath TargetPath, byte[] Bytes);