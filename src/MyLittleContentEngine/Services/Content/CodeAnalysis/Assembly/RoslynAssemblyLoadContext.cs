using System.Reflection;
using System.Runtime.Loader;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Assembly;

internal class RoslynAssemblyLoadContext() : AssemblyLoadContext(isCollectible: true)
{
    protected override System.Reflection.Assembly? Load(AssemblyName assemblyName) => null;
}
