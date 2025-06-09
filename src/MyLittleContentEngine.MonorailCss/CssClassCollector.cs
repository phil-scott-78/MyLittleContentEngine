// [assembly: MetadataUpdateHandler(typeof(CssClassCollector))]

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
    private static readonly Lock Lock = new();
    
    private static void OnUpdate()
    {
        lock (Lock)
        {
            Classes.Clear();
        }
    }

    // ClearCache should be the only one we need. Clearing with the rest might be problematic.
    // internal static void ClearCache(Type[]? _) => OnUpdate();
    // internal static void UpdateApplication(Type[]? _) => OnUpdate();
    // internal static void UpdateContent(string assemblyName, bool isApplicationProject, string relativePath, byte[] contents) => OnUpdate();

    public void AddClasses(string url, IEnumerable<string> classes)
    {
        lock (Lock)
        {
            foreach (var cls in classes)
            {
                Classes.Add(cls);
            }
        }
    }

    public IReadOnlyCollection<string> GetClasses()
    {
        lock (Lock)
        {
            return Classes.ToList().AsReadOnly();
        }
    }

    // Much like the other timing issue, at one point we were using this to determine if we should process the URL
    // then clearing it out on a hot reload. But the timing was off. For now, we'll always just return true.
    public bool ShouldProcess(string url) => true;
}