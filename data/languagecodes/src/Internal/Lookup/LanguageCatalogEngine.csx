namespace languagecodes;

internal class LanguageCatalogData
{
    internal LanguageCodeEntry[] EntriesByEntityIndex;
    internal int[] SortedEntityIndexes;
    internal global::System.Collections.Generic.Dictionary<string, int> EntityIndexesByKey;
    internal global::System.Collections.Generic.Dictionary<string, LanguageCodeEntry> RetiredEntriesByCode;

    internal LanguageCatalogData(
        LanguageCodeEntry[] entriesByEntityIndex,
        int[] sortedEntityIndexes,
        global::System.Collections.Generic.Dictionary<string, int> entityIndexesByKey,
        global::System.Collections.Generic.Dictionary<string, LanguageCodeEntry> retiredEntriesByCode)
    {
        EntriesByEntityIndex = entriesByEntityIndex;
        SortedEntityIndexes = sortedEntityIndexes;
        EntityIndexesByKey = entityIndexesByKey;
        RetiredEntriesByCode = retiredEntriesByCode;
    }
}

internal class LanguageCatalogSortRow
{
    internal string Code;
    internal string EntityKey;
    internal int EntityIndex;

    internal LanguageCatalogSortRow(string code, string entityKey, int entityIndex)
    {
        Code = code;
        EntityKey = entityKey;
        EntityIndex = entityIndex;
    }
}

internal static class LanguageCatalogEngine
{
    internal static readonly LanguageCatalogData Data = LanguageCatalogBuilder.BuildCatalog(
        LanguageDataTables.LanguageRows,
        LanguageDataTables.NameRows,
        LanguageDataTables.AliasRows,
        LanguageDataTables.RelationshipRows,
        LanguageDataTables.RetirementRows);

    internal static LanguageCodeEntry[] GetAllEntries()
    {
        return LanguageCatalogBuilder.CopyActiveEntries(Data);
    }

    internal static LanguageCodeEntry[] GetEntriesBySet(Iso639Set set)
    {
        return LanguageCatalogBuilder.CopyEntriesInSet(Data, set);
    }

    internal static LanguageCodeEntry[] GetEntriesByScope(LanguageScope scope)
    {
        return LanguageCatalogBuilder.CopyEntriesInScope(Data, scope);
    }

    internal static LanguageCodeEntry[] GetEntriesByType(LanguageType languageType)
    {
        return LanguageCatalogBuilder.CopyEntriesOfType(Data, languageType);
    }

    internal static LanguageCodeEntry[] GetEntriesByStatus(LanguageCodeStatus status)
    {
        return LanguageCatalogBuilder.CopyEntriesWithStatus(Data, status);
    }

    internal static LanguageCodeEntry? GetEntryByEntityIndex(int entityIndex)
    {
        if (entityIndex < 0 || entityIndex >= Data.EntriesByEntityIndex.Length)
        {
            return null;
        }

        return PublicResultFactory.CopyLanguageCodeEntry(Data.EntriesByEntityIndex[entityIndex]);
    }
}

internal static class LanguageCatalogBuilder
{
    internal static LanguageCatalogData BuildCatalog(
        LanguageRow[] languageRows,
        NameRow[] nameRows,
        AliasRow[] aliasRows,
        RelationshipRow[] relationshipRows,
        RetirementRow[] retirementRows)
    {
        global::System.Collections.Generic.Dictionary<string, int> entityIndexes =
            BuildEntityIndexes(languageRows);
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<NameRow>> namesByEntity =
                BuildNameLists(nameRows);
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<AliasRow>> aliasesByEntity =
                BuildAliasLists(aliasRows);
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> macroRelationshipsByCode =
                BuildRelationshipLists(
                    relationshipRows,
                    LanguageRelationshipKind.MacrolanguageMember);
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> groupRelationshipsByCode =
                BuildRelationshipLists(
                    relationshipRows,
                    LanguageRelationshipKind.Set5ParentChild);
        global::System.Collections.Generic.Dictionary<string, RetirementRow> retirementsByCode =
            BuildRetirementRecords(retirementRows);
        LanguageCodeEntry[] entries = new LanguageCodeEntry[languageRows.Length];
        int index;
        for (index = 0; index < languageRows.Length; index++)
        {
            LanguageRow row = languageRows[index];
            LanguageName[] names = CreatePublicNames(row.Key, namesByEntity);
            LanguageAlias[] aliases = CreatePublicAliases(row.Key, aliasesByEntity);
            LanguageRelationship[] relationships = CreatePublicRelationships(
                row,
                macroRelationshipsByCode,
                groupRelationshipsByCode);
            LanguageRetirement? retirement = FindPublicRetirement(row, retirementsByCode);
            entries[index] = PublicResultFactory.CreateLanguageCodeEntry(
                row,
                FindReferenceName(names),
                names,
                aliases,
                relationships,
                retirement);
        }

        return new LanguageCatalogData(
            entries,
            BuildSortedEntityIndexes(languageRows),
            entityIndexes,
            BuildRetiredEntryIndex(
                languageRows,
                retirementRows,
                macroRelationshipsByCode));
    }

    internal static global::System.Collections.Generic.Dictionary<string, int> BuildEntityIndexes(
        LanguageRow[] source)
    {
        global::System.Collections.Generic.Dictionary<string, int> destination =
            new global::System.Collections.Generic.Dictionary<string, int>(
                global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            if (destination.ContainsKey(source[index].Key))
            {
                throw new global::System.InvalidOperationException(
                    "Duplicate language entity key: " + source[index].Key);
            }

            destination.Add(source[index].Key, index);
        }

        return destination;
    }

    internal static global::System.Collections.Generic.Dictionary<
        string,
        global::System.Collections.Generic.List<NameRow>> BuildNameLists(NameRow[] source)
    {
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<NameRow>> destination =
                new global::System.Collections.Generic.Dictionary<
                    string,
                    global::System.Collections.Generic.List<NameRow>>(
                        global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            NameRow row = source[index];
            if (!destination.ContainsKey(row.EntityKey))
            {
                destination.Add(row.EntityKey, new global::System.Collections.Generic.List<NameRow>());
            }

            destination[row.EntityKey].Add(row);
        }

        return destination;
    }

    internal static global::System.Collections.Generic.Dictionary<
        string,
        global::System.Collections.Generic.List<AliasRow>> BuildAliasLists(AliasRow[] source)
    {
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<AliasRow>> destination =
                new global::System.Collections.Generic.Dictionary<
                    string,
                    global::System.Collections.Generic.List<AliasRow>>(
                        global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            AliasRow row = source[index];
            if (row.EntityKey != null)
            {
                if (!destination.ContainsKey(row.EntityKey))
                {
                    destination.Add(
                        row.EntityKey,
                        new global::System.Collections.Generic.List<AliasRow>());
                }

                destination[row.EntityKey].Add(row);
            }
        }

        return destination;
    }

    internal static global::System.Collections.Generic.Dictionary<
        string,
        global::System.Collections.Generic.List<RelationshipRow>> BuildRelationshipLists(
            RelationshipRow[] source,
            LanguageRelationshipKind kind)
    {
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> destination =
                new global::System.Collections.Generic.Dictionary<
                    string,
                    global::System.Collections.Generic.List<RelationshipRow>>(
                        global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            RelationshipRow row = source[index];
            if (row.Kind == kind)
            {
                AddRelationshipRow(destination, row.ParentCode, row);
                if (row.ChildCode != row.ParentCode)
                {
                    AddRelationshipRow(destination, row.ChildCode, row);
                }
            }
        }

        return destination;
    }

    internal static void AddRelationshipRow(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> destination,
        string code,
        RelationshipRow row)
    {
        if (!destination.ContainsKey(code))
        {
            destination.Add(code, new global::System.Collections.Generic.List<RelationshipRow>());
        }

        destination[code].Add(row);
    }

    internal static global::System.Collections.Generic.Dictionary<string, RetirementRow>
        BuildRetirementRecords(RetirementRow[] source)
    {
        global::System.Collections.Generic.Dictionary<string, RetirementRow> destination =
            new global::System.Collections.Generic.Dictionary<string, RetirementRow>(
                global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < source.Length; index++)
        {
            if (destination.ContainsKey(source[index].RetiredCode))
            {
                throw new global::System.InvalidOperationException(
                    "Duplicate retirement code: " + source[index].RetiredCode);
            }

            destination.Add(source[index].RetiredCode, source[index]);
        }

        return destination;
    }

    internal static global::System.Collections.Generic.Dictionary<string, LanguageCodeEntry>
        BuildRetiredEntryIndex(
            LanguageRow[] languageRows,
            RetirementRow[] retirementRows,
            global::System.Collections.Generic.Dictionary<
                string,
                global::System.Collections.Generic.List<RelationshipRow>> macroRelationships)
    {
        global::System.Collections.Generic.HashSet<string> existingCodes =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        int index;
        for (index = 0; index < languageRows.Length; index++)
        {
            string? code = languageRows[index].Set3Code;
            if (code != null)
            {
                existingCodes.Add(code);
            }
        }

        global::System.Collections.Generic.Dictionary<string, LanguageCodeEntry> destination =
            new global::System.Collections.Generic.Dictionary<string, LanguageCodeEntry>(
                global::System.StringComparer.Ordinal);
        for (index = 0; index < retirementRows.Length; index++)
        {
            RetirementRow retirement = retirementRows[index];
            if (!existingCodes.Contains(retirement.RetiredCode))
            {
                if (destination.ContainsKey(retirement.RetiredCode))
                {
                    throw new global::System.InvalidOperationException(
                        "Duplicate retirement code: " + retirement.RetiredCode);
                }

                destination.Add(
                    retirement.RetiredCode,
                    CreateRetiredEntry(retirement, macroRelationships));
            }
        }

        return destination;
    }

    internal static LanguageCodeEntry CreateRetiredEntry(
        RetirementRow retirement,
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> macroRelationships)
    {
        LanguageName[] names = new LanguageName[]
        {
            new LanguageName
            {
                Text = retirement.FormerReferenceName,
                NameLanguage = "en",
                Kind = LanguageNameKind.Reference,
                SourceSet = Iso639Set.Set3,
                IsReferenceName = true
            }
        };
        global::System.Collections.Generic.List<LanguageRelationship> relationships =
            new global::System.Collections.Generic.List<LanguageRelationship>();
        if (macroRelationships.ContainsKey(retirement.RetiredCode))
        {
            AppendPublicRelationships(
                relationships,
                macroRelationships[retirement.RetiredCode]);
        }

        return new LanguageCodeEntry
        {
            Set3Code = retirement.RetiredCode,
            InSet3 = true,
            Scope = LanguageScope.Unknown,
            LanguageType = LanguageType.Unknown,
            Status = LanguageCodeStatus.Retired,
            ReferenceName = retirement.FormerReferenceName,
            Names = PublicResultFactory.CopyLanguageNames(names),
            Aliases = new LanguageAlias[0],
            Relationships = PublicResultFactory.CopyLanguageRelationships(
                relationships.ToArray()),
            Retirement = PublicResultFactory.CreateLanguageRetirement(retirement)
        };
    }

    internal static LanguageName[] CreatePublicNames(
        string entityKey,
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<NameRow>> source)
    {
        if (!source.ContainsKey(entityKey))
        {
            return new LanguageName[0];
        }

        global::System.Collections.Generic.List<NameRow> rows = source[entityKey];
        LanguageName[] destination = new LanguageName[rows.Count];
        int index;
        for (index = 0; index < rows.Count; index++)
        {
            destination[index] = PublicResultFactory.CreateLanguageName(rows[index]);
        }

        return destination;
    }

    internal static LanguageAlias[] CreatePublicAliases(
        string entityKey,
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<AliasRow>> source)
    {
        if (!source.ContainsKey(entityKey))
        {
            return new LanguageAlias[0];
        }

        global::System.Collections.Generic.List<AliasRow> rows = source[entityKey];
        LanguageAlias[] destination = new LanguageAlias[rows.Count];
        int index;
        for (index = 0; index < rows.Count; index++)
        {
            destination[index] = PublicResultFactory.CreateLanguageAlias(rows[index]);
        }

        return destination;
    }

    internal static LanguageRelationship[] CreatePublicRelationships(
        LanguageRow language,
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> macroRelationships,
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipRow>> groupRelationships)
    {
        global::System.Collections.Generic.List<LanguageRelationship> destination =
            new global::System.Collections.Generic.List<LanguageRelationship>();
        if (language.Set3Code != null && macroRelationships.ContainsKey(language.Set3Code))
        {
            AppendPublicRelationships(destination, macroRelationships[language.Set3Code]);
        }

        if (language.Set5Code != null && groupRelationships.ContainsKey(language.Set5Code))
        {
            AppendPublicRelationships(destination, groupRelationships[language.Set5Code]);
        }

        return destination.ToArray();
    }

    internal static void AppendPublicRelationships(
        global::System.Collections.Generic.List<LanguageRelationship> destination,
        global::System.Collections.Generic.List<RelationshipRow> source)
    {
        int index;
        for (index = 0; index < source.Count; index++)
        {
            destination.Add(PublicResultFactory.CreateLanguageRelationship(source[index]));
        }
    }

    internal static LanguageRetirement? FindPublicRetirement(
        LanguageRow language,
        global::System.Collections.Generic.Dictionary<string, RetirementRow> retirements)
    {
        if (language.Set3Code == null || !retirements.ContainsKey(language.Set3Code))
        {
            return null;
        }

        return PublicResultFactory.CreateLanguageRetirement(retirements[language.Set3Code]);
    }

    internal static string? FindReferenceName(LanguageName[] names)
    {
        int index;
        for (index = 0; index < names.Length; index++)
        {
            if (names[index].IsReferenceName)
            {
                return names[index].Text;
            }
        }

        return null;
    }

    internal static int[] BuildSortedEntityIndexes(LanguageRow[] source)
    {
        global::System.Collections.Generic.List<LanguageCatalogSortRow> sortRows =
            new global::System.Collections.Generic.List<LanguageCatalogSortRow>();
        int index;
        for (index = 0; index < source.Length; index++)
        {
            sortRows.Add(new LanguageCatalogSortRow(
                FindStableCode(source[index]),
                source[index].Key,
                index));
        }

        sortRows.Sort(
            (left, right) =>
            {
                int comparison = string.CompareOrdinal(left.Code, right.Code);
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.CompareOrdinal(left.EntityKey, right.EntityKey);
            });
        int[] destination = new int[sortRows.Count];
        for (index = 0; index < sortRows.Count; index++)
        {
            destination[index] = sortRows[index].EntityIndex;
        }

        return destination;
    }

    internal static string FindStableCode(LanguageRow source)
    {
        if (source.Set3Code != null)
        {
            return source.Set3Code;
        }

        if (source.Set5Code != null)
        {
            return source.Set5Code;
        }

        if (source.Set2TerminologyCode != null)
        {
            return source.Set2TerminologyCode;
        }

        if (source.Set2BibliographicCode != null)
        {
            return source.Set2BibliographicCode;
        }

        if (source.Alpha2Code != null)
        {
            return source.Alpha2Code;
        }

        return source.Key;
    }

    internal static LanguageCodeEntry[] CopyActiveEntries(LanguageCatalogData source)
    {
        return CopyEntriesWithStatus(source, LanguageCodeStatus.Active);
    }

    internal static LanguageCodeEntry[] CopyEntriesInSet(
        LanguageCatalogData source,
        Iso639Set set)
    {
        global::System.Collections.Generic.List<LanguageCodeEntry> destination =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        int index;
        for (index = 0; index < source.SortedEntityIndexes.Length; index++)
        {
            LanguageCodeEntry entry = source.EntriesByEntityIndex[source.SortedEntityIndexes[index]];
            if (entry.Status == LanguageCodeStatus.Active && EntryBelongsToSet(entry, set))
            {
                destination.Add(PublicResultFactory.CopyLanguageCodeEntry(entry));
            }
        }

        return destination.ToArray();
    }

    internal static LanguageCodeEntry[] CopyEntriesInScope(
        LanguageCatalogData source,
        LanguageScope scope)
    {
        global::System.Collections.Generic.List<LanguageCodeEntry> destination =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        int index;
        for (index = 0; index < source.SortedEntityIndexes.Length; index++)
        {
            LanguageCodeEntry entry = source.EntriesByEntityIndex[source.SortedEntityIndexes[index]];
            if (entry.Status == LanguageCodeStatus.Active && entry.Scope == scope)
            {
                destination.Add(PublicResultFactory.CopyLanguageCodeEntry(entry));
            }
        }

        return destination.ToArray();
    }

    internal static LanguageCodeEntry[] CopyEntriesOfType(
        LanguageCatalogData source,
        LanguageType languageType)
    {
        global::System.Collections.Generic.List<LanguageCodeEntry> destination =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        int index;
        for (index = 0; index < source.SortedEntityIndexes.Length; index++)
        {
            LanguageCodeEntry entry = source.EntriesByEntityIndex[source.SortedEntityIndexes[index]];
            if (entry.Status == LanguageCodeStatus.Active && entry.LanguageType == languageType)
            {
                destination.Add(PublicResultFactory.CopyLanguageCodeEntry(entry));
            }
        }

        return destination.ToArray();
    }

    internal static LanguageCodeEntry[] CopyEntriesWithStatus(
        LanguageCatalogData source,
        LanguageCodeStatus status)
    {
        global::System.Collections.Generic.List<LanguageCodeEntry> destination =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        int index;
        for (index = 0; index < source.SortedEntityIndexes.Length; index++)
        {
            LanguageCodeEntry entry = source.EntriesByEntityIndex[source.SortedEntityIndexes[index]];
            if (entry.Status == status)
            {
                destination.Add(PublicResultFactory.CopyLanguageCodeEntry(entry));
            }
        }

        if (status == LanguageCodeStatus.Retired)
        {
            foreach (LanguageCodeEntry entry in source.RetiredEntriesByCode.Values)
            {
                destination.Add(PublicResultFactory.CopyLanguageCodeEntry(entry));
            }
        }

        destination.Sort(
            (left, right) =>
            {
                int comparison = string.CompareOrdinal(
                    FindStablePublicCode(left),
                    FindStablePublicCode(right));
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.CompareOrdinal(left.ReferenceName, right.ReferenceName);
            });

        return destination.ToArray();
    }

    internal static string FindStablePublicCode(LanguageCodeEntry source)
    {
        if (source.Set3Code != null)
        {
            return source.Set3Code;
        }

        if (source.Set5Code != null)
        {
            return source.Set5Code;
        }

        if (source.Set2TerminologyCode != null)
        {
            return source.Set2TerminologyCode;
        }

        if (source.Set2BibliographicCode != null)
        {
            return source.Set2BibliographicCode;
        }

        if (source.Alpha2Code != null)
        {
            return source.Alpha2Code;
        }

        return string.Empty;
    }

    internal static bool EntryBelongsToSet(LanguageCodeEntry entry, Iso639Set set)
    {
        if (set == Iso639Set.Set1)
        {
            return entry.InSet1;
        }

        if (set == Iso639Set.Set2)
        {
            return entry.InSet2;
        }

        if (set == Iso639Set.Set3)
        {
            return entry.InSet3;
        }

        if (set == Iso639Set.Set5)
        {
            return entry.InSet5;
        }

        return false;
    }
}
