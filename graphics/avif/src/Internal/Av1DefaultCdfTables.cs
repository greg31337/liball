using System.Collections.Generic;

namespace avif;

// All 96 normative default CDF tables from AV1 specification section 9.4.4.
// The packed representation retains each specification table name, declared
// shape, and every 16-bit CDF entry while avoiding a very large initializer graph.
internal class Av1DefaultCdfTable
{
    internal string Name;
    internal string Shape;
    internal ushort[] Values;

    internal Av1DefaultCdfTable(string name, string shape, ushort[] values)
    {
        Name = name;
        Shape = shape;
        Values = values;
    }
}

internal static class Av1DefaultCdfTables
{
    static readonly string[] compressedChunks = new string[]
    {
        "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",
        "ISpI3mb5h5NfjKf/AeBKUZbvsgAA",
    };

    static readonly List<string> names = new List<string>();
    static readonly Dictionary<string, Av1DefaultCdfTable> tables = LoadTables();

    internal static int GetTableCount()
    {
        return names.Count;
    }

    internal static string GetTableName(int index)
    {
        return index >= 0 && index < names.Count ? names[index] : string.Empty;
    }

    internal static bool Contains(string name)
    {
        return name != null && tables.ContainsKey(name);
    }

    internal static string GetShape(string name)
    {
        Av1DefaultCdfTable? table = GetTable(name);
        return table == null ? string.Empty : table.Shape;
    }

    internal static int GetValueCount(string name)
    {
        Av1DefaultCdfTable? table = GetTable(name);
        return table == null ? 0 : table.Values.Length;
    }

    internal static ushort[] GetValues(string name)
    {
        Av1DefaultCdfTable? table = GetTable(name);
        return table == null
            ? System.Array.Empty<ushort>()
            : (ushort[])table.Values.Clone();
    }

    internal static Av1DefaultCdfTable? GetTable(string name)
    {
        if (name == null || !tables.ContainsKey(name))
        {
            return null;
        }

        return tables[name];
    }

    static Dictionary<string, Av1DefaultCdfTable> LoadTables()
    {
        byte[] compressed = System.Convert.FromBase64String(JoinChunks());
        System.IO.MemoryStream input = new System.IO.MemoryStream(compressed);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        using (System.IO.Compression.GZipStream gzip =
            new System.IO.Compression.GZipStream(
                input,
                System.IO.Compression.CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }

        byte[] data = output.ToArray();
        input.Close();
        output.Close();
        int offset = 0;
        int tableCount = ReadInt32(data, offset);
        offset += 4;
        Dictionary<string, Av1DefaultCdfTable> loaded =
            new Dictionary<string, Av1DefaultCdfTable>();
        int tableIndex = 0;
        while (tableIndex < tableCount)
        {
            int nameLength = ReadUInt16(data, offset);
            offset += 2;
            string name = System.Text.Encoding.UTF8.GetString(data, offset, nameLength);
            offset += nameLength;
            int shapeLength = ReadUInt16(data, offset);
            offset += 2;
            string shape = System.Text.Encoding.UTF8.GetString(data, offset, shapeLength);
            offset += shapeLength;
            int valueCount = ReadInt32(data, offset);
            offset += 4;
            ushort[] values = new ushort[valueCount];
            int valueIndex = 0;
            while (valueIndex < valueCount)
            {
                values[valueIndex] = (ushort)ReadUInt16(data, offset);
                offset += 2;
                valueIndex++;
            }

            values = NormalizeCompactProducts(values);

            names.Add(name);
            loaded.Add(name, new Av1DefaultCdfTable(name, shape, values));
            tableIndex++;
        }

        return loaded;
    }

    static ushort[] NormalizeCompactProducts(ushort[] values)
    {
        List<ushort> normalized = new List<ushort>();
        int start = 0;
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] == 0
                && values[index - 1] == Av1SyntaxConstants.CdfProbabilityTop)
            {
                int rowLength = index - start + 1;
                if (rowLength == 4 && values[start] <= 256
                    && values[start + 1] <= 256)
                {
                    normalized.Add((ushort)(values[start] * values[start + 1]));
                    normalized.Add(values[index - 1]);
                    normalized.Add(values[index]);
                }
                else
                {
                    int copyIndex = start;
                    while (copyIndex <= index)
                    {
                        normalized.Add(values[copyIndex]);
                        copyIndex++;
                    }
                }

                start = index + 1;
            }

            index++;
        }

        while (start < values.Length)
        {
            normalized.Add(values[start]);
            start++;
        }

        return normalized.Count == values.Length ? values : normalized.ToArray();
    }

    static string JoinChunks()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < compressedChunks.Length)
        {
            builder.Append(compressedChunks[index]);
            index++;
        }

        return builder.ToString();
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] | data[offset + 1] << 8;
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return data[offset]
            | data[offset + 1] << 8
            | data[offset + 2] << 16
            | data[offset + 3] << 24;
    }
}
