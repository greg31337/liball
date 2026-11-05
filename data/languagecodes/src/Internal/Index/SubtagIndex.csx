namespace languagecodes;

internal class SubtagIndexData
{
    internal IanaSubtagRow[] Records;
    internal global::System.Collections.Generic.Dictionary<string, int> RecordIndexesByKey;
    internal global::System.Collections.Generic.Dictionary<string, int[]> RecordIndexesByValue;
    internal global::System.Collections.Generic.Dictionary<string, int[]> RecordIndexesByPrefix;
    internal global::System.Collections.Generic.Dictionary<string, string> PreferredValuesByKey;
    internal global::System.Collections.Generic.Dictionary<string, string> SuppressScriptsByLanguage;
    internal global::System.Collections.Generic.Dictionary<string, int> GrandfatheredTagIndexesByValue;
    internal global::System.Collections.Generic.Dictionary<string, int> RedundantTagIndexesByValue;
    internal string[] SortedCompleteTagValues;

    internal SubtagIndexData(
        IanaSubtagRow[] records,
        global::System.Collections.Generic.Dictionary<string, int> recordIndexesByKey,
        global::System.Collections.Generic.Dictionary<string, int[]> recordIndexesByValue,
        global::System.Collections.Generic.Dictionary<string, int[]> recordIndexesByPrefix,
        global::System.Collections.Generic.Dictionary<string, string> preferredValuesByKey,
        global::System.Collections.Generic.Dictionary<string, string> suppressScriptsByLanguage,
        global::System.Collections.Generic.Dictionary<string, int> grandfatheredTagIndexesByValue,
        global::System.Collections.Generic.Dictionary<string, int> redundantTagIndexesByValue,
        string[] sortedCompleteTagValues)
    {
        Records = records;
        RecordIndexesByKey = recordIndexesByKey;
        RecordIndexesByValue = recordIndexesByValue;
        RecordIndexesByPrefix = recordIndexesByPrefix;
        PreferredValuesByKey = preferredValuesByKey;
        SuppressScriptsByLanguage = suppressScriptsByLanguage;
        GrandfatheredTagIndexesByValue = grandfatheredTagIndexesByValue;
        RedundantTagIndexesByValue = redundantTagIndexesByValue;
        SortedCompleteTagValues = sortedCompleteTagValues;
    }
}

internal static class SubtagIndex
{
    internal static readonly SubtagIndexData Data = SubtagIndexBuilder.BuildIndex(
        LanguageDataTables.IanaRows,
        LanguageDataTables.IanaIndexSeeds);

    internal static bool IsAssigned(LanguageSubtagType type, string? value)
    {
        if (value == null)
        {
            return false;
        }

        string key = SubtagIndexBuilder.CreateRecordKey(type, value);
        return Data.RecordIndexesByKey.ContainsKey(key);
    }

    internal static IanaSubtagRow? FindRecord(LanguageSubtagType type, string? value)
    {
        if (value == null)
        {
            return null;
        }

        string key = SubtagIndexBuilder.CreateRecordKey(type, value);
        if (!Data.RecordIndexesByKey.ContainsKey(key))
        {
            return null;
        }

        return SubtagIndexBuilder.CopyIanaRecord(Data.Records[Data.RecordIndexesByKey[key]]);
    }

    internal static IanaSubtagRow? FindRecordOrRange(
        LanguageSubtagType type,
        string? value)
    {
        IanaSubtagRow? exact = FindRecord(type, value);
        if (exact != null || value == null)
        {
            return exact;
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(value);
        int characterIndex;
        for (characterIndex = 0; characterIndex < normalized.Length; characterIndex++)
        {
            if (!AsciiCode.IsAsciiLetter(normalized[characterIndex]))
            {
                return null;
            }
        }

        int index;
        for (index = 0; index < Data.Records.Length; index++)
        {
            IanaSubtagRow record = Data.Records[index];
            if (record.RecordType != type)
            {
                continue;
            }

            string range = SubtagIndexBuilder.NormalizeValue(record.Value);
            int separator = range.IndexOf(
                "..",
                global::System.StringComparison.Ordinal);
            if (separator <= 0 || separator + 2 >= range.Length)
            {
                continue;
            }

            string first = range.Substring(0, separator);
            string last = range.Substring(separator + 2);
            if (normalized.Length == first.Length &&
                normalized.Length == last.Length &&
                global::System.String.CompareOrdinal(normalized, first) >= 0 &&
                global::System.String.CompareOrdinal(normalized, last) <= 0)
            {
                return SubtagIndexBuilder.CopyIanaRecord(record);
            }
        }

        return null;
    }

    internal static IanaSubtagRow[] FindRecordsByValue(string? value)
    {
        if (value == null)
        {
            return new IanaSubtagRow[0];
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(value);
        if (!Data.RecordIndexesByValue.ContainsKey(normalized))
        {
            return new IanaSubtagRow[0];
        }

        return SubtagIndexBuilder.CopyRecordsFromIndexes(
            Data.Records,
            Data.RecordIndexesByValue[normalized]);
    }

    internal static IanaSubtagRow[] FindRecordsForPrefix(string? prefix)
    {
        if (prefix == null)
        {
            return new IanaSubtagRow[0];
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(prefix);
        if (!Data.RecordIndexesByPrefix.ContainsKey(normalized))
        {
            return new IanaSubtagRow[0];
        }

        return SubtagIndexBuilder.CopyRecordsFromIndexes(
            Data.Records,
            Data.RecordIndexesByPrefix[normalized]);
    }

    internal static bool AllowsPrefix(
        LanguageSubtagType type,
        string? value,
        string? prefix)
    {
        if (value == null || prefix == null)
        {
            return false;
        }

        string key = SubtagIndexBuilder.CreateRecordKey(type, value);
        if (!Data.RecordIndexesByKey.ContainsKey(key))
        {
            return false;
        }

        string normalizedPrefix = SubtagIndexBuilder.NormalizeValue(prefix);
        IanaSubtagRow record = Data.Records[Data.RecordIndexesByKey[key]];
        int index;
        for (index = 0; index < record.Prefixes.Length; index++)
        {
            if (SubtagIndexBuilder.NormalizeValue(record.Prefixes[index]) == normalizedPrefix)
            {
                return true;
            }
        }

        return false;
    }

    internal static string? FindPreferredValue(LanguageSubtagType type, string? value)
    {
        if (value == null)
        {
            return null;
        }

        string key = SubtagIndexBuilder.CreateRecordKey(type, value);
        if (!Data.PreferredValuesByKey.ContainsKey(key))
        {
            return null;
        }

        return Data.PreferredValuesByKey[key];
    }

    internal static string? FindSuppressScript(string? language)
    {
        if (language == null)
        {
            return null;
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(language);
        if (!Data.SuppressScriptsByLanguage.ContainsKey(normalized))
        {
            return null;
        }

        return Data.SuppressScriptsByLanguage[normalized];
    }

    internal static IanaSubtagRow? FindGrandfatheredTag(string? tag)
    {
        if (tag == null)
        {
            return null;
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(tag);
        if (!Data.GrandfatheredTagIndexesByValue.ContainsKey(normalized))
        {
            return null;
        }

        return SubtagIndexBuilder.CopyIanaRecord(
            Data.Records[Data.GrandfatheredTagIndexesByValue[normalized]]);
    }

    internal static IanaSubtagRow? FindCompleteTag(string? tag)
    {
        if (tag == null)
        {
            return null;
        }

        string normalized = SubtagIndexBuilder.NormalizeValue(tag);
        if (Data.GrandfatheredTagIndexesByValue.ContainsKey(normalized))
        {
            return SubtagIndexBuilder.CopyIanaRecord(
                Data.Records[Data.GrandfatheredTagIndexesByValue[normalized]]);
        }

        if (Data.RedundantTagIndexesByValue.ContainsKey(normalized))
        {
            return SubtagIndexBuilder.CopyIanaRecord(
                Data.Records[Data.RedundantTagIndexesByValue[normalized]]);
        }

        return null;
    }
}

internal static class SubtagIndexBuilder
{
    internal static SubtagIndexData BuildIndex(
        IanaSubtagRow[] sourceRecords,
        IanaIndexSeedRow[] sourceSeeds)
    {
        IanaSubtagRow[] records = LanguageTableMaterializer.CopyIanaRows(sourceRecords);
        global::System.Collections.Generic.Dictionary<string, int> recordIndexesByKey =
            new global::System.Collections.Generic.Dictionary<string, int>(
                global::System.StringComparer.Ordinal);
        bool[] indexedRecords = new bool[records.Length];
        int index;
        for (index = 0; index < sourceSeeds.Length; index++)
        {
            IanaIndexSeedRow seed = sourceSeeds[index];
            if (seed.RecordIndex < 0 || seed.RecordIndex >= records.Length)
            {
                throw new global::System.InvalidOperationException(
                    "IANA index seed refers to an invalid record index.");
            }

            IanaSubtagRow record = records[seed.RecordIndex];
            if (seed.RecordType != record.RecordType || seed.Value != record.Value)
            {
                throw new global::System.InvalidOperationException(
                    "IANA index seed does not match its registry record.");
            }

            if (indexedRecords[seed.RecordIndex])
            {
                throw new global::System.InvalidOperationException(
                    "IANA registry record has more than one index seed: " + record.Value);
            }

            string key = CreateRecordKey(seed.RecordType, seed.Value);
            if (recordIndexesByKey.ContainsKey(key))
            {
                throw new global::System.InvalidOperationException(
                    "Duplicate IANA registry type and value: " + record.Value);
            }

            indexedRecords[seed.RecordIndex] = true;
            recordIndexesByKey.Add(key, seed.RecordIndex);
        }

        for (index = 0; index < indexedRecords.Length; index++)
        {
            if (!indexedRecords[index])
            {
                throw new global::System.InvalidOperationException(
                    "IANA registry record is missing an index seed: " + records[index].Value);
            }
        }

        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<int>> recordIndexesByValue =
                CreateRecordIndexLists();
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<int>> recordIndexesByPrefix =
                CreateRecordIndexLists();
        global::System.Collections.Generic.Dictionary<string, string> preferredValuesByKey =
            new global::System.Collections.Generic.Dictionary<string, string>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.Dictionary<string, string> suppressScriptsByLanguage =
            new global::System.Collections.Generic.Dictionary<string, string>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.Dictionary<string, int> grandfatheredTagsByValue =
            new global::System.Collections.Generic.Dictionary<string, int>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.Dictionary<string, int> redundantTagsByValue =
            new global::System.Collections.Generic.Dictionary<string, int>(
                global::System.StringComparer.Ordinal);
        for (index = 0; index < records.Length; index++)
        {
            IanaSubtagRow record = records[index];
            string normalizedValue = NormalizeValue(record.Value);
            AddRecordIndex(recordIndexesByValue, normalizedValue, index);
            int prefixIndex;
            for (prefixIndex = 0; prefixIndex < record.Prefixes.Length; prefixIndex++)
            {
                AddRecordIndex(
                    recordIndexesByPrefix,
                    NormalizeValue(record.Prefixes[prefixIndex]),
                    index);
            }

            string recordKey = CreateRecordKey(record.RecordType, record.Value);
            if (record.PreferredValue != null)
            {
                preferredValuesByKey.Add(recordKey, record.PreferredValue);
            }

            if (record.RecordType == LanguageSubtagType.Language && record.SuppressScript != null)
            {
                suppressScriptsByLanguage.Add(normalizedValue, record.SuppressScript);
            }

            if (record.RecordType == LanguageSubtagType.Grandfathered)
            {
                grandfatheredTagsByValue.Add(normalizedValue, index);
            }
            else if (record.RecordType == LanguageSubtagType.Redundant)
            {
                redundantTagsByValue.Add(normalizedValue, index);
            }
        }

        return new SubtagIndexData(
            records,
            recordIndexesByKey,
            FreezeRecordIndexLists(recordIndexesByValue, records),
            FreezeRecordIndexLists(recordIndexesByPrefix, records),
            preferredValuesByKey,
            suppressScriptsByLanguage,
            grandfatheredTagsByValue,
            redundantTagsByValue,
            BuildSortedCompleteTagValues(grandfatheredTagsByValue, redundantTagsByValue));
    }

    internal static global::System.Collections.Generic.Dictionary<
        string,
        global::System.Collections.Generic.List<int>> CreateRecordIndexLists()
    {
        return new global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<int>>(global::System.StringComparer.Ordinal);
    }

    internal static void AddRecordIndex(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<int>> destination,
        string key,
        int recordIndex)
    {
        if (!destination.ContainsKey(key))
        {
            destination.Add(key, new global::System.Collections.Generic.List<int>());
        }

        if (!destination[key].Contains(recordIndex))
        {
            destination[key].Add(recordIndex);
        }
    }

    internal static global::System.Collections.Generic.Dictionary<string, int[]>
        FreezeRecordIndexLists(
            global::System.Collections.Generic.Dictionary<
                string,
                global::System.Collections.Generic.List<int>> source,
            IanaSubtagRow[] records)
    {
        global::System.Collections.Generic.Dictionary<string, int[]> destination =
            new global::System.Collections.Generic.Dictionary<string, int[]>(
                global::System.StringComparer.Ordinal);
        foreach (
            global::System.Collections.Generic.KeyValuePair<
                string,
                global::System.Collections.Generic.List<int>> pair in source)
        {
            pair.Value.Sort(
                (left, right) =>
                {
                    int comparison = ((int)records[left].RecordType).CompareTo(
                        (int)records[right].RecordType);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    return string.CompareOrdinal(records[left].Value, records[right].Value);
                });
            destination.Add(pair.Key, pair.Value.ToArray());
        }

        return destination;
    }

    internal static string[] BuildSortedCompleteTagValues(
        global::System.Collections.Generic.Dictionary<string, int> grandfatheredTags,
        global::System.Collections.Generic.Dictionary<string, int> redundantTags)
    {
        string[] values = new string[grandfatheredTags.Count + redundantTags.Count];
        int index = 0;
        foreach (string value in grandfatheredTags.Keys)
        {
            values[index] = value;
            index++;
        }

        foreach (string value in redundantTags.Keys)
        {
            values[index] = value;
            index++;
        }

        global::System.Array.Sort(values, global::System.StringComparer.Ordinal);
        return values;
    }

    internal static string CreateRecordKey(LanguageSubtagType type, string value)
    {
        return ((int)type).ToString() + ":" + NormalizeValue(value);
    }

    internal static string NormalizeValue(string value)
    {
        char[] characters = value.ToCharArray();
        int index;
        for (index = 0; index < characters.Length; index++)
        {
            if (characters[index] >= 'A' && characters[index] <= 'Z')
            {
                characters[index] = (char)(characters[index] + ('a' - 'A'));
            }
        }

        return new string(characters);
    }

    internal static IanaSubtagRow[] CopyRecordsFromIndexes(
        IanaSubtagRow[] records,
        int[] indexes)
    {
        IanaSubtagRow[] destination = new IanaSubtagRow[indexes.Length];
        int index;
        for (index = 0; index < indexes.Length; index++)
        {
            destination[index] = CopyIanaRecord(records[indexes[index]]);
        }

        return destination;
    }

    internal static IanaSubtagRow CopyIanaRecord(IanaSubtagRow source)
    {
        return new IanaSubtagRow(
            source.RecordType,
            source.Value,
            LanguageTableMaterializer.CopyStrings(source.Descriptions),
            source.AddedDate,
            source.DeprecatedDate,
            source.PreferredValue,
            LanguageTableMaterializer.CopyStrings(source.Prefixes),
            source.SuppressScript,
            source.Macrolanguage,
            source.Scope,
            LanguageTableMaterializer.CopyStrings(source.Comments));
    }
}
