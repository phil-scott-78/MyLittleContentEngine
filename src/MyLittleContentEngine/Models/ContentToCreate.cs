namespace MyLittleContentEngine.Models;

/// <summary>
/// Content that will be written to the output during a static build.
/// </summary>
/// <param name="TargetPath"></param>
/// <param name="Bytes"></param>
public record ContentToCreate(string TargetPath, byte[] Bytes);