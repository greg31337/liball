using System.Collections.Generic;

namespace iso20022;

internal class XmiPassOneResult
{
    internal List<XmiPassOneEntry> Entries = new List<XmiPassOneEntry>();
    internal Dictionary<string, XmiPassOneEntry> EntriesByIdentifier =
        new Dictionary<string, XmiPassOneEntry>(System.StringComparer.Ordinal);
    internal List<XmiDuplicateIdentifier> Duplicates = new List<XmiDuplicateIdentifier>();
    internal XmiPassOneEntry? Repository;
    internal XmiPassOneEntry? DataDictionary;
    internal XmiPassOneEntry? BusinessProcessCatalogue;
    internal int ElementCount;
    internal int IdentifiedElementCount;
    internal int UnidentifiedElementCount;

    internal XmiPassOneEntry? FindIdentifier(string identifier)
    {
        if (identifier == null)
        {
            throw new System.ArgumentNullException("identifier");
        }

        if (EntriesByIdentifier.ContainsKey(identifier))
        {
            return EntriesByIdentifier[identifier];
        }

        return null;
    }
}
