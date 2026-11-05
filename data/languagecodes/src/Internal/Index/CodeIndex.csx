namespace languagecodes;

internal class CodeIndexEntry
{
    internal string Value;
    internal int EntityIndex;
    internal LanguageCodeForm Form;
    internal Iso639Set Set;
    internal LanguageCodeStatus Status;
    internal string? PreferredValue;

    internal CodeIndexEntry(
        string value,
        int entityIndex,
        LanguageCodeForm form,
        Iso639Set set,
        LanguageCodeStatus status,
        string? preferredValue)
    {
        Value = value;
        EntityIndex = entityIndex;
        Form = form;
        Set = set;
        Status = status;
        PreferredValue = preferredValue;
    }
}

internal static class CodeIndex
{
    internal static readonly CodeIndexEntry[] EmptySpanEntries = new CodeIndexEntry[0];

    internal static readonly global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]> EntriesByValue =
        CodeIndexBuilder.BuildIndex(
            LanguageDataTables.LanguageRows,
            LanguageDataTables.CodeIndexSeeds,
            LanguageDataTables.RetirementRows,
            LanguageDataTables.AliasRows);

    internal static readonly CodeIndexEntry[][] EntriesBySpanIndex =
        CodeIndexBuilder.BuildSpanIndex(EntriesByValue, EmptySpanEntries);

    internal static CodeIndexEntry[] FindAll(string? value)
    {
        if (value == null || !EntriesByValue.ContainsKey(value))
        {
            return new CodeIndexEntry[0];
        }

        return CodeIndexBuilder.CopyEntries(EntriesByValue[value]);
    }

    internal static CodeIndexEntry? FindExact(string? value, LanguageCodeForm form)
    {
        if (value == null || !EntriesByValue.ContainsKey(value))
        {
            return null;
        }

        CodeIndexEntry[] entries = EntriesByValue[value];
        int index;
        for (index = 0; index < entries.Length; index++)
        {
            if (entries[index].Form == form)
            {
                return entries[index];
            }
        }

        return null;
    }

    internal static CodeIndexEntry[] FindAllSpan(global::System.ReadOnlySpan<char> value)
    {
        int index = CodeIndexBuilder.FindSpanIndex(value);
        if (index < 0)
        {
            return EmptySpanEntries;
        }

        return EntriesBySpanIndex[index];
    }
}

internal static class CodeIndexBuilder
{
    internal static CodeIndexEntry[][] BuildSpanIndex(
        global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]> source,
        CodeIndexEntry[] emptyEntries)
    {
        CodeIndexEntry[][] destination = new CodeIndexEntry[18252][];
        int index;
        for (index = 0; index < destination.Length; index++)
        {
            destination[index] = emptyEntries;
        }

        foreach (
            global::System.Collections.Generic.KeyValuePair<string, CodeIndexEntry[]> pair
                in source)
        {
            int spanIndex = FindSpanIndex(pair.Key);
            if (spanIndex >= 0)
            {
                destination[spanIndex] = pair.Value;
            }
        }

        return destination;
    }

    internal static int FindSpanIndex(global::System.ReadOnlySpan<char> value)
    {
        if (!AsciiCode.IsLexicallyValid(value))
        {
            return -1;
        }

        int first = AsciiCode.ToLowerAscii(value[0]) - 'a';
        int second = AsciiCode.ToLowerAscii(value[1]) - 'a';
        if (value.Length == 2)
        {
            return first * 26 + second;
        }

        int third = AsciiCode.ToLowerAscii(value[2]) - 'a';
        return 676 + first * 676 + second * 26 + third;
    }

    internal static global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]> BuildIndex(
        LanguageRow[] languageRows,
        CodeIndexSeedRow[] activeSeeds,
        RetirementRow[] retirementRows,
        AliasRow[] aliasRows)
    {
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes =
            BuildEntityIndexes(languageRows);
        global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<CodeIndexEntry>>
            entries = new global::System.Collections.Generic.Dictionary<
                string,
                global::System.Collections.Generic.List<CodeIndexEntry>>(global::System.StringComparer.Ordinal);

        AddActiveSeeds(entries, activeSeeds);
        AddRetirementRows(entries, retirementRows, aliasRows, entityIndexes);
        AddReservedRange(entries);
        AddIanaAliases(entries, aliasRows, entityIndexes);
        return FreezeEntries(entries);
    }

    internal static global::System.Collections.Generic.Dictionary<string, int> BuildEntityIndexes(
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

    internal static void AddActiveSeeds(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> entries,
        CodeIndexSeedRow[] activeSeeds)
    {
        int index;
        for (index = 0; index < activeSeeds.Length; index++)
        {
            CodeIndexSeedRow seed = activeSeeds[index];
            AddEntry(
                entries,
                new CodeIndexEntry(
                    seed.Value,
                    seed.EntityIndex,
                    seed.Form,
                    seed.Set,
                    LanguageCodeStatus.Active,
                    null));
        }
    }

    internal static void AddRetirementRows(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> entries,
        RetirementRow[] retirementRows,
        AliasRow[] aliasRows,
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes)
    {
        int index;
        for (index = 0; index < retirementRows.Length; index++)
        {
            RetirementRow retirement = retirementRows[index];
            string? entityKey = FindRetiredEntityKey(retirement.RetiredCode, aliasRows);
            int entityIndex = FindEntityIndex(entityKey, entityIndexes);
            string? preferredValue = null;
            if (retirement.ReplacementCodes.Length == 1)
            {
                preferredValue = retirement.ReplacementCodes[0];
            }

            AddEntry(
                entries,
                new CodeIndexEntry(
                    retirement.RetiredCode,
                    entityIndex,
                    LanguageCodeForm.RetiredSet3,
                    Iso639Set.Set3,
                    LanguageCodeStatus.Retired,
                    preferredValue));
        }
    }

    internal static void AddReservedRange(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> entries)
    {
        char second;
        for (second = 'a'; second <= 't'; second++)
        {
            char third;
            for (third = 'a'; third <= 'z'; third++)
            {
                string value = new string(new char[] { 'q', second, third });
                AddEntry(
                    entries,
                    new CodeIndexEntry(
                        value,
                        -1,
                        LanguageCodeForm.PrivateUse,
                        Iso639Set.Set3,
                        LanguageCodeStatus.PrivateUse,
                        null));
            }
        }
    }

    internal static void AddIanaAliases(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> entries,
        AliasRow[] aliasRows,
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes)
    {
        int index;
        for (index = 0; index < aliasRows.Length; index++)
        {
            AliasRow alias = aliasRows[index];
            bool include = false;
            if (alias.Kind == LanguageAliasKind.IanaDeprecated ||
                alias.Kind == LanguageAliasKind.Grandfathered)
            {
                include = true;
            }
            else if (alias.Kind == LanguageAliasKind.IanaPreferred &&
                IsDirectIanaSubtagSource(alias.SourceRecordIdentifier))
            {
                include = true;
            }

            if (include)
            {
                LanguageCodeStatus status = alias.IsDeprecated
                    ? LanguageCodeStatus.Deprecated
                    : LanguageCodeStatus.Active;
                AddEntry(
                    entries,
                    new CodeIndexEntry(
                        alias.Value,
                        FindEntityIndex(alias.EntityKey, entityIndexes),
                        InferIanaForm(alias.SourceRecordIdentifier),
                        Iso639Set.None,
                        status,
                        alias.PreferredValue));
            }
        }
    }

    internal static string? FindRetiredEntityKey(string value, AliasRow[] aliasRows)
    {
        string? entityKey = null;
        int index;
        for (index = 0; index < aliasRows.Length; index++)
        {
            AliasRow alias = aliasRows[index];
            if (alias.Kind == LanguageAliasKind.RetiredSet3 && alias.Value == value)
            {
                if (entityKey != null && alias.EntityKey != entityKey)
                {
                    throw new global::System.InvalidOperationException(
                        "Conflicting retired-code targets for: " + value);
                }

                entityKey = alias.EntityKey;
            }
        }

        return entityKey;
    }

    internal static int FindEntityIndex(
        string? entityKey,
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes)
    {
        if (entityKey == null || !entityIndexes.ContainsKey(entityKey))
        {
            return -1;
        }

        return entityIndexes[entityKey];
    }

    internal static LanguageCodeForm InferIanaForm(string sourceRecordIdentifier)
    {
        if (sourceRecordIdentifier.StartsWith("extlang:", global::System.StringComparison.Ordinal))
        {
            return LanguageCodeForm.IanaExtlang;
        }

        if (sourceRecordIdentifier.StartsWith(
            "grandfathered:",
            global::System.StringComparison.Ordinal))
        {
            return LanguageCodeForm.GrandfatheredTag;
        }

        if (sourceRecordIdentifier.StartsWith("redundant:", global::System.StringComparison.Ordinal))
        {
            return LanguageCodeForm.RedundantTag;
        }

        return LanguageCodeForm.IanaLanguage;
    }

    internal static bool IsDirectIanaSubtagSource(string sourceRecordIdentifier)
    {
        return sourceRecordIdentifier.StartsWith(
                "language:",
                global::System.StringComparison.Ordinal) ||
            sourceRecordIdentifier.StartsWith(
                "extlang:",
                global::System.StringComparison.Ordinal);
    }

    internal static void AddEntry(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> entries,
        CodeIndexEntry entry)
    {
        if (!entries.ContainsKey(entry.Value))
        {
            entries.Add(entry.Value, new global::System.Collections.Generic.List<CodeIndexEntry>());
        }

        global::System.Collections.Generic.List<CodeIndexEntry> sameValue = entries[entry.Value];
        int index;
        for (index = 0; index < sameValue.Count; index++)
        {
            CodeIndexEntry existing = sameValue[index];
            if (existing.Form == entry.Form)
            {
                if (!IsSameTarget(existing, entry))
                {
                    throw new global::System.InvalidOperationException(
                        "Conflicting code mapping for " + entry.Value + " as " + entry.Form);
                }

                if (existing.Status != entry.Status)
                {
                    if (IsCompatibleIanaStatus(existing.Status, entry.Status))
                    {
                        existing.Status = LanguageCodeStatus.Deprecated;
                    }
                    else
                    {
                        throw new global::System.InvalidOperationException(
                            "Conflicting code status for " + entry.Value + " as " + entry.Form);
                    }
                }

                return;
            }
        }

        sameValue.Add(entry);
    }

    internal static bool IsSameTarget(CodeIndexEntry left, CodeIndexEntry right)
    {
        return left.EntityIndex == right.EntityIndex &&
            left.Set == right.Set &&
            left.PreferredValue == right.PreferredValue;
    }

    internal static bool IsCompatibleIanaStatus(
        LanguageCodeStatus left,
        LanguageCodeStatus right)
    {
        return left == LanguageCodeStatus.Active && right == LanguageCodeStatus.Deprecated ||
            left == LanguageCodeStatus.Deprecated && right == LanguageCodeStatus.Active;
    }

    internal static global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]> FreezeEntries(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<CodeIndexEntry>> source)
    {
        global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]> destination =
            new global::System.Collections.Generic.Dictionary<string, CodeIndexEntry[]>(
                global::System.StringComparer.Ordinal);
        foreach (
            global::System.Collections.Generic.KeyValuePair<
                string,
                global::System.Collections.Generic.List<CodeIndexEntry>> pair in source)
        {
            global::System.Collections.Generic.List<CodeIndexEntry> entries = pair.Value;
            entries.Sort(
                (left, right) =>
                {
                    int formComparison = ((int)left.Form).CompareTo((int)right.Form);
                    if (formComparison != 0)
                    {
                        return formComparison;
                    }

                    return left.EntityIndex.CompareTo(right.EntityIndex);
                });
            destination.Add(pair.Key, entries.ToArray());
        }

        return destination;
    }

    internal static CodeIndexEntry[] CopyEntries(CodeIndexEntry[] source)
    {
        CodeIndexEntry[] destination = new CodeIndexEntry[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            CodeIndexEntry entry = source[index];
            destination[index] = new CodeIndexEntry(
                entry.Value,
                entry.EntityIndex,
                entry.Form,
                entry.Set,
                entry.Status,
                entry.PreferredValue);
        }

        return destination;
    }
}
