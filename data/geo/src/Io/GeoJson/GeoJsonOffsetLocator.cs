using System.Collections.Generic;
using System.Text.Json;

namespace geo;

internal static class GeoJsonOffsetLocator
{
    internal static Dictionary<string, long> CreateOffsetMap(byte[] source, int maximumDepth)
    {
        Dictionary<string, long> offsets = new Dictionary<string, long>();
        string[] paths = new string[maximumDepth + 1];
        bool[] arrays = new bool[maximumDepth + 1];
        int[] indexes = new int[maximumDepth + 1];
        string[] properties = new string[maximumDepth + 1];
        int stackCount = 0;
        JsonReaderOptions readerOptions = new JsonReaderOptions();
        readerOptions.MaxDepth = maximumDepth;
        Utf8JsonReader reader = new Utf8JsonReader(source, readerOptions);
        while (reader.Read())
        {
            JsonTokenType token = reader.TokenType;
            if (token == JsonTokenType.PropertyName)
            {
                string? decodedName = reader.GetString();
                string name = decodedName == null ? "" : decodedName;
                properties[stackCount - 1] = name;
                string propertyPath = paths[stackCount - 1] + "." + name;
                offsets[propertyPath] = reader.TokenStartIndex;
            }
            else if (token == JsonTokenType.EndObject || token == JsonTokenType.EndArray)
            {
                if (stackCount != 0)
                {
                    offsets[paths[stackCount - 1] + "#end"] = reader.TokenStartIndex;
                    stackCount--;
                }
            }
            else
            {
                string valuePath = GetNextValuePath(paths, arrays, indexes, properties, stackCount);
                offsets[valuePath] = reader.TokenStartIndex;
                if (token == JsonTokenType.StartObject || token == JsonTokenType.StartArray)
                {
                    paths[stackCount] = valuePath;
                    arrays[stackCount] = token == JsonTokenType.StartArray;
                    indexes[stackCount] = 0;
                    properties[stackCount] = "";
                    stackCount++;
                }
            }
        }
        return offsets;
    }

    internal static string GetNextValuePath(
        string[] paths, bool[] arrays, int[] indexes, string[] properties, int stackCount)
    {
        if (stackCount == 0) return "$";
        int parent = stackCount - 1;
        if (arrays[parent])
        {
            string path = paths[parent] + "[" + indexes[parent] + "]";
            indexes[parent]++;
            return path;
        }
        string property = properties[parent];
        properties[parent] = "";
        return paths[parent] + "." + property;
    }

    internal static long ResolveOffset(Dictionary<string, long> offsets, string path)
    {
        if (offsets.ContainsKey(path)) return offsets[path];
        string current = path;
        while (current.Length > 1)
        {
            int bracket = current.LastIndexOf('[');
            int dot = current.LastIndexOf('.');
            int split = bracket > dot ? bracket : dot;
            if (split < 1) break;
            current = current.Substring(0, split);
            string endPath = current + "#end";
            if (offsets.ContainsKey(endPath)) return offsets[endPath];
            if (offsets.ContainsKey(current)) return offsets[current];
        }
        return offsets.ContainsKey("$") ? offsets["$"] : 0;
    }
}
