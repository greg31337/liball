using System;
using System.Collections.Generic;

namespace gltf;

internal class ExtensionDependencyEnumerator
{
    internal ExtensionUsageCollector Usage = new ExtensionUsageCollector();
    internal ExtensionContextLocator Contexts = new ExtensionContextLocator();
    internal GltfExtensionHandler Handler = new GltfExtensionHandler();

    internal List<GltfDependency> EnumerateExtensionDependencies(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<GltfDependency> result = new List<GltfDependency>();
        List<EncounteredExtension> encountered =
            Usage.CollectEncounteredExtensionLocations(document);
        int index = 0;
        while (index < encountered.Count)
        {
            EncounteredExtension item = encountered[index];
            string objectPointer = GetContainingObjectPointer(item.JsonPointer);
            GltfExtensionContext context = Contexts.CreateContext(
                document,
                item.Name,
                item.Json,
                objectPointer,
                document.Resources);
            GltfExtensionResult extension = Handler.ValidateExtension(context);
            AppendReferences(extension.References, result);
            index++;
        }

        return result;
    }

    internal string GetContainingObjectPointer(string extensionPointer)
    {
        int marker = extensionPointer.LastIndexOf(
            "/extensions/",
            StringComparison.Ordinal);
        if (marker < 0)
        {
            return extensionPointer;
        }

        return extensionPointer.Substring(0, marker);
    }

    internal void AppendReferences(
        List<GltfDependency> source,
        List<GltfDependency> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
