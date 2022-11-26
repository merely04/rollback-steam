using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RollbackSteam.Services;

public static class ResourcesService
{
    private static readonly Assembly _assembly;

    static ResourcesService()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    public static List<string> GetResourcesList()
    {
        return _assembly
            .GetManifestResourceNames()
            .ToList();
    }

    public static void ExtractResource(string resourceName, string fileName)
    {
        using var resource = _assembly.GetManifestResourceStream(resourceName);
        using var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
        resource?.CopyTo(fs);
    }
}