namespace gltf;

internal class JsonPointerBuilder
{
    internal string AppendProperty(string parent, string memberName)
    {
        string escapedName = memberName.Replace("~", "~0").Replace("/", "~1");
        return $"{parent}/{escapedName}";
    }

    internal string AppendIndex(string parent, int index)
    {
        return $"{parent}/{index}";
    }
}
