using System.IO.Abstractions;

namespace MyLittleContentEngine.Services;

/// <summary>
/// Extension methods for FilePath that perform I/O operations.
/// These are separated from the value object to keep it pure and side-effect free.
/// </summary>
public static class FilePathExtensions
{
    private static IFileSystem? _defaultFileSystem;

    /// <summary>
    /// Gets or sets the default file system implementation to use for I/O operations.
    /// If not set, uses the real file system.
    /// </summary>
    public static IFileSystem DefaultFileSystem
    {
        get => _defaultFileSystem ??= new FileSystem();
        set => _defaultFileSystem = value;
    }

    /// <summary>
    /// Checks if the file exists on the file system.
    /// </summary>
    public static bool FileExists(this FilePath path, IFileSystem? fileSystem = null)
    {
        if (path.IsEmpty)
            return false;

        var fs = fileSystem ?? DefaultFileSystem;
        return fs.File.Exists(path.Value);
    }

    /// <summary>
    /// Checks if the directory exists on the file system.
    /// </summary>
    public static bool DirectoryExists(this FilePath path, IFileSystem? fileSystem = null)
    {
        if (path.IsEmpty)
            return false;

        var fs = fileSystem ?? DefaultFileSystem;
        return fs.Directory.Exists(path.Value);
    }

    /// <summary>
    /// Ensures the directory for this file path exists, creating it if necessary.
    /// </summary>
    public static void EnsureDirectoryExists(this FilePath path, IFileSystem? fileSystem = null)
    {
        if (path.IsEmpty)
            return;

        var fs = fileSystem ?? DefaultFileSystem;
        var directory = path.GetDirectory();
        if (!directory.IsEmpty && !directory.DirectoryExists(fs))
        {
            fs.Directory.CreateDirectory(directory.Value);
        }
    }

    /// <summary>
    /// Gets the parent directory path using file system operations.
    /// </summary>
    public static FilePath GetParentDirectory(this FilePath path, IFileSystem? fileSystem = null)
    {
        if (path.IsEmpty)
            return FilePath.Empty;

        var fs = fileSystem ?? DefaultFileSystem;
        var parent = fs.Directory.GetParent(path.Value)?.FullName;
        return new FilePath(parent);
    }
}