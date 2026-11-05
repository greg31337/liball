namespace languagecodes;

internal class NameIndexEntry
{
    internal string NormalizedValue;
    internal int NameIndex;
    internal int EntityIndex;
    internal string EntityKey;
    internal string Text;
    internal string NameLanguage;
    internal LanguageNameKind Kind;
    internal ReconciledSource Source;
    internal bool IsReferenceName;

    internal NameIndexEntry(
        string normalizedValue,
        int nameIndex,
        int entityIndex,
        string entityKey,
        string text,
        string nameLanguage,
        LanguageNameKind kind,
        ReconciledSource source,
        bool isReferenceName)
    {
        NormalizedValue = normalizedValue;
        NameIndex = nameIndex;
        EntityIndex = entityIndex;
        EntityKey = entityKey;
        Text = text;
        NameLanguage = nameLanguage;
        Kind = kind;
        Source = source;
        IsReferenceName = isReferenceName;
    }
}

internal static class NameIndex
{
    internal static readonly global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]>
        EntriesByNormalizedValue = NameIndexBuilder.BuildIndex(
            LanguageDataTables.LanguageRows,
            LanguageDataTables.NameRows);
    internal static readonly string[] SortedNormalizedValues =
        NameIndexBuilder.BuildSortedValues(EntriesByNormalizedValue);

    internal static NameIndexEntry[] FindExact(string? query)
    {
        string normalized = UnicodeName.Normalize(query);
        if (normalized.Length == 0 || !EntriesByNormalizedValue.ContainsKey(normalized))
        {
            return new NameIndexEntry[0];
        }

        return NameIndexBuilder.CopyNameEntries(EntriesByNormalizedValue[normalized]);
    }

    internal static NameIndexEntry[] Search(string? query, LanguageSearchMode mode, int limit)
    {
        return NameIndexBuilder.SearchEntries(
            EntriesByNormalizedValue,
            SortedNormalizedValues,
            query,
            mode,
            limit);
    }
}

internal static class NameIndexBuilder
{
    internal static global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> BuildIndex(
        LanguageRow[] languageRows,
        NameRow[] nameRows)
    {
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes =
            BuildNameEntityIndexes(languageRows);
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<NameIndexEntry>>
            entries = new global::System.Collections.Generic.Dictionary<
                string,
                global::System.Collections.Generic.List<NameIndexEntry>>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < nameRows.Length; index++)
        {
            NameRow row = nameRows[index];
            if (!entityIndexes.ContainsKey(row.EntityKey))
            {
                throw new global::System.InvalidOperationException(
                    "Language name refers to a missing entity: " + row.EntityKey);
            }

            string normalized = UnicodeName.Normalize(row.Text);
            if (normalized.Length > 0)
            {
                AddNameEntry(
                    entries,
                    new NameIndexEntry(
                        normalized,
                        index,
                        entityIndexes[row.EntityKey],
                        row.EntityKey,
                        row.Text,
                        row.NameLanguage,
                        row.Kind,
                        row.Source,
                        row.IsReferenceName));
            }
        }

        return FreezeNameEntries(entries);
    }

    internal static global::System.Collections.Generic.Dictionary<string, int> BuildNameEntityIndexes(
        LanguageRow[] languageRows)
    {
        global::System.Collections.Generic.Dictionary<string, int> indexes =
            new global::System.Collections.Generic.Dictionary<string, int>(global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < languageRows.Length; index++)
        {
            string key = languageRows[index].Key;
            if (indexes.ContainsKey(key))
            {
                throw new global::System.InvalidOperationException("Duplicate language entity key: " + key);
            }

            indexes.Add(key, index);
        }

        return indexes;
    }

    internal static void AddNameEntry(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<NameIndexEntry>> entries,
        NameIndexEntry entry)
    {
        if (!entries.ContainsKey(entry.NormalizedValue))
        {
            entries.Add(
                entry.NormalizedValue,
                new global::System.Collections.Generic.List<NameIndexEntry>());
        }

        entries[entry.NormalizedValue].Add(entry);
    }

    internal static global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> FreezeNameEntries(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<NameIndexEntry>> source)
    {
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> destination =
            new global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]>(
                global::System.StringComparer.Ordinal);
        foreach (
            global::System.Collections.Generic.KeyValuePair<
                string,
                global::System.Collections.Generic.List<NameIndexEntry>> pair in source)
        {
            global::System.Collections.Generic.List<NameIndexEntry> entries = pair.Value;
            entries.Sort(
                (left, right) =>
                {
                    int comparison = left.EntityIndex.CompareTo(right.EntityIndex);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = string.CompareOrdinal(left.NameLanguage, right.NameLanguage);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = ((int)left.Kind).CompareTo((int)right.Kind);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = ((int)left.Source).CompareTo((int)right.Source);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = string.CompareOrdinal(left.Text, right.Text);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    return left.NameIndex.CompareTo(right.NameIndex);
                });
            destination.Add(pair.Key, entries.ToArray());
        }

        return destination;
    }

    internal static string[] BuildSortedValues(
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> entries)
    {
        string[] values = new string[entries.Count];
        int index = 0;
        foreach (string value in entries.Keys)
        {
            values[index] = value;
            index++;
        }

        global::System.Array.Sort(values, global::System.StringComparer.Ordinal);
        return values;
    }

    internal static NameIndexEntry[] SearchEntries(
        global::System.Collections.Generic.Dictionary<string, NameIndexEntry[]> entries,
        string[] sortedValues,
        string? query,
        LanguageSearchMode mode,
        int limit)
    {
        string normalized = UnicodeName.Normalize(query);
        if (normalized.Length == 0 || limit <= 0)
        {
            return new NameIndexEntry[0];
        }

        global::System.Collections.Generic.List<NameIndexEntry> matches =
            new global::System.Collections.Generic.List<NameIndexEntry>();
        if (mode == LanguageSearchMode.Exact)
        {
            if (entries.ContainsKey(normalized))
            {
                AppendNameCopies(matches, entries[normalized], limit);
            }

            return matches.ToArray();
        }

        int keyIndex;
        for (keyIndex = 0; keyIndex < sortedValues.Length && matches.Count < limit; keyIndex++)
        {
            string value = sortedValues[keyIndex];
            bool isMatch = false;
            if (mode == LanguageSearchMode.Prefix)
            {
                isMatch = value.StartsWith(normalized, global::System.StringComparison.Ordinal);
            }
            else if (mode == LanguageSearchMode.Contains)
            {
                isMatch = value.Contains(normalized, global::System.StringComparison.Ordinal);
            }

            if (isMatch)
            {
                AppendNameCopies(matches, entries[value], limit);
            }
        }

        return matches.ToArray();
    }

    internal static void AppendNameCopies(
        global::System.Collections.Generic.List<NameIndexEntry> destination,
        NameIndexEntry[] source,
        int limit)
    {
        int index;
        for (index = 0; index < source.Length && destination.Count < limit; index++)
        {
            destination.Add(CopyNameEntry(source[index]));
        }
    }

    internal static NameIndexEntry CopyNameEntry(NameIndexEntry entry)
    {
        return new NameIndexEntry(
            entry.NormalizedValue,
            entry.NameIndex,
            entry.EntityIndex,
            entry.EntityKey,
            entry.Text,
            entry.NameLanguage,
            entry.Kind,
            entry.Source,
            entry.IsReferenceName);
    }

    internal static NameIndexEntry[] CopyNameEntries(NameIndexEntry[] source)
    {
        NameIndexEntry[] destination = new NameIndexEntry[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyNameEntry(source[index]);
        }

        return destination;
    }
}
