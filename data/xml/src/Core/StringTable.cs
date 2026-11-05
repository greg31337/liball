using System;
using System.Collections.Generic;

namespace xml;

internal class StringTable
{
    internal Dictionary<string, string> Entries;

    internal StringTable()
    {
        Entries = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal string GetOrAdd(string value)
    {
        Guard.NotNull(value, "value");
        if (Entries.ContainsKey(value))
        {
            return Entries[value];
        }

        Entries.Add(value, value);
        return value;
    }

    internal bool ContainsText(string value)
    {
        Guard.NotNull(value, "value");
        return Entries.ContainsKey(value);
    }

    internal int EntryCount()
    {
        return Entries.Count;
    }

    internal void ClearEntries()
    {
        Entries.Clear();
    }
}
