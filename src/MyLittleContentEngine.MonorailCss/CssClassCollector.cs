﻿using System.Reflection.Metadata;
using MyLittleContentEngine.MonorailCss;

[assembly: MetadataUpdateHandler(typeof(CssClassCollector))]

namespace MyLittleContentEngine.MonorailCss;

public class CssClassCollector
{
    // At one point we were using MetaDataUpdateHandler, but it was causing issues with the
    // timing. Sometimes the browser refresh would happen before the call for this ClearCache would occur,
    // which would result in the classes being added to the collection then immediately removed.
    // 
    // For now, we'll just keep adding to the Classes hashset and not worry about clearing it.
    // It'll cause some CSS classes to be added that are not used during hot reload scenarios,
    // but that's not a big deal. The classes will be removed on the next build.
    private static readonly HashSet<string> Classes = [];
    private static readonly ReaderWriterLockSlim ProcessingLock = new(LockRecursionPolicy.SupportsRecursion);
    
    private static void OnUpdate()
    {
        ProcessingLock.EnterWriteLock();
        try
        {
            Classes.Clear();
        }
        finally
        {
            ProcessingLock.ExitWriteLock();
        }
    }

    internal static void ClearCache(Type[]? _) => OnUpdate();
    internal static void UpdateContent(string assemblyName, bool isApplicationProject, string relativePath, byte[] contents) => OnUpdate();

    public void AddClasses(string url, IEnumerable<string> classes)
    {
        // This is called from within middleware processing, so we're already holding the write lock
        foreach (var cls in classes)
        {
            Classes.Add(cls);
        }
    }
    
    public void BeginProcessing()
    {
        ProcessingLock.EnterWriteLock();
    }
    
    public void EndProcessing()
    {
        ProcessingLock.ExitWriteLock();
    }

    public IReadOnlyCollection<string> GetClasses()
    {
        ProcessingLock.EnterReadLock();
        try
        {
            return Classes.ToList().AsReadOnly();
        }
        finally
        {
            ProcessingLock.ExitReadLock();
        }
    }

    // Much like the other timing issue, at one point we were using this to determine if we should process the URL
    // then clearing it out on a hot reload. But the timing was off. For now, we'll always just return true.
    public bool ShouldProcess(string url) => true;
}