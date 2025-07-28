using System.Runtime.CompilerServices;

namespace MyLittleContentEngine.IntegrationTests;

public static class CurrentFilePath
{
    public static string GetUnitTestProjectRoot()
    {
        return Path.GetDirectoryName(InternalFilePathCall()) ?? throw new InvalidOperationException("Could not get test project root");
    }


    private static string InternalFilePathCall([CallerFilePath] string? filePath = null) => filePath ?? throw new ArgumentNullException(nameof(filePath));
}