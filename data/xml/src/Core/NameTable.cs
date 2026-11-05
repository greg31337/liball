using System.Collections.Generic;

namespace xml;

internal class NameEntry
{
    internal string NamespaceUri;
    internal string LocalName;

    internal NameEntry(string namespaceUri, string localName)
    {
        NamespaceUri = namespaceUri;
        LocalName = localName;
    }
}

internal class NameTable
{
    internal StringTable Strings;
    internal Dictionary<string, Dictionary<string, NameEntry>> Namespaces;
    internal int Count;

    internal NameTable(StringTable strings)
    {
        Guard.NotNull(strings, "strings");
        Strings = strings;
        Namespaces = new Dictionary<string, Dictionary<string, NameEntry>>(System.StringComparer.Ordinal);
        Count = 0;
    }

    internal NameEntry GetOrAddName(string namespaceUri, string localName)
    {
        Guard.NotNull(namespaceUri, "namespaceUri");
        Guard.NotNullOrEmpty(localName, "localName");

        string internedNamespace = Strings.GetOrAdd(namespaceUri);
        string internedLocalName = Strings.GetOrAdd(localName);
        Dictionary<string, NameEntry> localNames;
        if (Namespaces.ContainsKey(internedNamespace))
        {
            localNames = Namespaces[internedNamespace];
        }
        else
        {
            localNames = new Dictionary<string, NameEntry>(System.StringComparer.Ordinal);
            Namespaces.Add(internedNamespace, localNames);
        }

        if (localNames.ContainsKey(internedLocalName))
        {
            return localNames[internedLocalName];
        }

        NameEntry entry = new NameEntry(internedNamespace, internedLocalName);
        localNames.Add(internedLocalName, entry);
        Count = Count + 1;
        return entry;
    }

    internal int NameCount()
    {
        return Count;
    }

    internal void ClearNames()
    {
        Namespaces.Clear();
        Count = 0;
    }
}
