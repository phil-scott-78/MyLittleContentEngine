using System.Reflection;
using System.Runtime.Loader;

namespace MyLittleContentEngine.Services.Content.Roslyn;

internal class RoslynAssemblyLoadContext() : AssemblyLoadContext(isCollectible: true)
{
    protected override Assembly? Load(AssemblyName assemblyName) => null;
}
