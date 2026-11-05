namespace odf;

internal static class ResourceGarbageCollector
{
    internal static string[] FindUnusedResources(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfEditException(
                "A document is required for resource inspection.");
        }
        string[] result = DrawingResourceManager.FindOrphanedResources(
            document);
        document.OrphanedResourcePaths = CopyStrings(result);
        return result;
    }

    internal static OdfEditResult CollectUnusedResources(
        OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfEditException(
                "A document is required for resource garbage collection.");
        }
        return DrawingResourceManager.CollectGarbage(document);
    }

    static string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        global::System.Array.Copy(source, result, source.Length);
        return result;
    }
}
