using System.Collections.Generic;
using System.Text;

namespace cbor;

internal class CddlInstancePath
{
    internal List<string> Segments = new List<string>();

    internal CddlInstancePath()
    {
    }

    internal void PushArrayIndex(int index)
    {
        Segments.Add("[" + index + "]");
    }

    internal void PushMapTextKey(string key)
    {
        string escaped = key.Replace("\\", "\\\\", System.StringComparison.Ordinal)
            .Replace("\"", "\\\"", System.StringComparison.Ordinal);
        Segments.Add("[\"" + escaped + "\"]");
    }

    internal void PushMapEntryIndex(int index)
    {
        Segments.Add("{entry:" + index + "}");
    }

    internal void PushTaggedValue()
    {
        Segments.Add(".value");
    }

    internal void PushEmbeddedValue()
    {
        Segments.Add(".embedded");
    }

    internal void PopSegment()
    {
        if (Segments.Count > 0)
        {
            Segments.RemoveAt(Segments.Count - 1);
        }
    }

    internal string CreatePath()
    {
        StringBuilder builder = new StringBuilder("$");
        int index = 0;
        while (index < Segments.Count)
        {
            builder.Append(Segments[index]);
            index = index + 1;
        }
        return builder.ToString();
    }
}

internal class CddlSchemaPath
{
    internal List<string> Segments = new List<string>();

    internal CddlSchemaPath()
    {
    }

    internal void PushRuleName(string name)
    {
        Segments.Add("/rule(" + name + ")");
    }

    internal void PushNodeIndex(int index)
    {
        Segments.Add("/node[" + index + "]");
    }

    internal void PushChoiceIndex(int index)
    {
        Segments.Add("/choice[" + index + "]");
    }

    internal void PushControlName(string name)
    {
        Segments.Add("/control(" + name + ")");
    }

    internal void PopSegment()
    {
        if (Segments.Count > 0)
        {
            Segments.RemoveAt(Segments.Count - 1);
        }
    }

    internal string CreatePath()
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < Segments.Count)
        {
            builder.Append(Segments[index]);
            index = index + 1;
        }
        return builder.ToString();
    }
}
