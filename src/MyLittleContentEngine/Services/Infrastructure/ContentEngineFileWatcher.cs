using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Infrastructure;

[assembly: MetadataUpdateHandler(typeof(ContentEngineFileWatcher))]

namespace MyLittleContentEngine.Services.Infrastructure;

public interface IContentEngineFileWatcher
{
    /// <summary>
    /// Adds a watch for a single directory for file changes with a specific file pattern.
    /// </summary>
    /// <param name="path">The directory path to watch.</param>
    /// <param name="filePattern">The file pattern to watch (e.g., "*.cs", "*.razor").</param>
    /// <param name="onFileChanged">Action to execute when a file changes, with the full path of the changed file.</param>
    /// <param name="includeSubdirectories">Whether to include subdirectories in the watch. Default is true.</param>
    void AddPathWatch(string path, string filePattern, Action<string> onFileChanged, bool includeSubdirectories = true);

    /// <summary>
    /// Adds watches for multiple directories for any file changes.
    /// </summary>
    /// <param name="paths">Collection of directory paths to watch.</param>
    /// <param name="onUpdate">Action to execute when any file changes in any of the watched directories.</param>
    /// <param name="includeSubdirectories">Whether to include subdirectories in the watch. Default is true.</param>
    void AddPathsWatch(IEnumerable<string> paths, Action onUpdate, bool includeSubdirectories = true);

    /// <summary>
    /// Subscribes to all update events for metadata changes.
    /// </summary>
    /// <param name="onUpdate"></param>
    void SubscribeToMetadataUpdate(Action onUpdate);

    /// <summary>
    /// Releases all resources used by this instance.
    /// </summary>
    void Dispose();
}

/// <summary>
/// A generic file system watcher that monitors specified directories for file changes.
/// Supports watching for specific file types and triggering callbacks with optional file path information.
///
/// Additionally, it supports hot-reloading of content in a Blazor static site by listening for file changes to those
/// configured in the csproj as being watching for changes.
/// </summary>
internal sealed class ContentEngineFileWatcher : IDisposable, IContentEngineFileWatcher
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private static readonly ConcurrentBag<Action> UpdateActions = [];
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentEngineFileWatcher"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public ContentEngineFileWatcher(IFileSystem fileSystem, ILogger? logger = null)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Adds a watch for a single directory for file changes with a specific file pattern.
    /// </summary>
    /// <param name="path">The directory path to watch.</param>
    /// <param name="filePattern">The file pattern to watch (e.g., "*.cs", "*.razor").</param>
    /// <param name="onFileChanged">Action to execute when a file changes, with the full path of the changed file.</param>
    /// <param name="includeSubdirectories">Whether to include subdirectories in the watch. Default is true.</param>
    public void AddPathWatch(string path, string filePattern, Action<string> onFileChanged,
        bool includeSubdirectories = true)
    {
        if (!_fileSystem.Directory.Exists(path))
        {
            _logger?.LogWarning("Directory {Path} does not exist and will not be watched", path);
            return;
        }

        var directoryInfo = _fileSystem.DirectoryInfo.New(path);
        var watchKey = $"{directoryInfo.FullName}|{filePattern}";

        // Skip if we already have a watcher for this path and pattern
        if (_watchers.ContainsKey(watchKey))
        {
            return;
        }

        _logger?.LogDebug("Watching {Path} for {Pattern} file changes", directoryInfo.FullName, filePattern);

        try
        {
            var watcher = new FileSystemWatcher(directoryInfo.FullName, filePattern)
            {
                IncludeSubdirectories = includeSubdirectories,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName |
                               NotifyFilters.CreationTime
            };

            watcher.Changed += (_, e) => onFileChanged(e.FullPath);
            watcher.Created += (_, e) => onFileChanged(e.FullPath);
            watcher.Deleted += (_, e) => onFileChanged(e.FullPath);
            watcher.Renamed += (_, e) => onFileChanged(e.FullPath);

            _watchers.Add(watchKey, watcher);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting up file watcher for {Path} with pattern {Pattern}", path, filePattern);
        }
    }
    
    public void SubscribeToMetadataUpdate(Action onUpdate)
    {
        UpdateActions.Add(onUpdate);
    }

    /// <summary>
    /// Adds watches for multiple directories for any file changes.
    /// </summary>
    /// <param name="paths">Collection of directory paths to watch.</param>
    /// <param name="onUpdate">Action to execute when any file changes in any of the watched directories.</param>
    /// <param name="includeSubdirectories">Whether to include subdirectories in the watch. Default is true.</param>
    public void AddPathsWatch(IEnumerable<string> paths, Action onUpdate, bool includeSubdirectories = true)
    {
        UpdateActions.Add(onUpdate);

        foreach (var path in paths)
        {
            if (!_fileSystem.Directory.Exists(path))
            {
                _logger?.LogWarning("Directory {Path} does not exist and will not be watched", path);
                continue;
            }

            // Skip if we already have a watcher for this path
            if (_watchers.ContainsKey(path))
            {
                continue;
            }

            _logger?.LogDebug("Watching {Path} for any file changes", path);

            try
            {
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = includeSubdirectories,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                   NotifyFilters.CreationTime
                };

                watcher.Changed += OnAnyContentChanged;
                watcher.Created += OnAnyContentChanged;
                watcher.Deleted += OnAnyContentChanged;
                watcher.Renamed += OnAnyContentChanged;

                _watchers.Add(path, watcher);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting up file watcher for {Path}", path);
            }
        }
    }

    internal static void ClearCache(Type[]? _)
    {
        OnUpdate();
    }

    internal static void UpdateContent(string assemblyName, bool isApplicationProject, string relativePath,
        byte[] contents)
    {
        // not sure if this actually gets called, but if it does, we want to trigger an update
        OnUpdate();
    }

    private void OnAnyContentChanged(object sender, FileSystemEventArgs e)
    {
        OnUpdate();
    }

    private static void OnUpdate()
    {
        foreach (var action in UpdateActions)
        {
            action.Invoke();
        }
    }

    /// <summary>
    /// Releases all resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
            UpdateActions.Clear();
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ContentEngineFileWatcher"/> class.
    /// </summary>
    ~ContentEngineFileWatcher()
    {
        Dispose(false);
    }
}