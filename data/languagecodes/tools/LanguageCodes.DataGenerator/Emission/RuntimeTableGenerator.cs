using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.Emission;

internal static class RuntimeTableGenerator
{
    internal static GeneratedSourceFile[] CreateFiles(
        ReconciledIso639Data isoData,
        Iso639Set3NameRecord[] set3Names,
        Iso639Set3MacrolanguageMapping[] macrolanguageMappings,
        Iso639Set3RetirementRecord[] retirements,
        Iso639Set5HierarchyData set5Hierarchy,
        IanaLanguageSubtagRegistry ianaRegistry,
        ReconciledCodeAliasData aliasData,
        DataManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(isoData);
        ArgumentNullException.ThrowIfNull(set3Names);
        ArgumentNullException.ThrowIfNull(macrolanguageMappings);
        ArgumentNullException.ThrowIfNull(retirements);
        ArgumentNullException.ThrowIfNull(set5Hierarchy);
        ArgumentNullException.ThrowIfNull(ianaRegistry);
        ArgumentNullException.ThrowIfNull(aliasData);
        ArgumentNullException.ThrowIfNull(manifest);

        ReconciledIso639Entity[] entities = SortEntities(isoData.Entities);
        Dictionary<string, int> entityIndexes = CreateEntityIndexes(entities);
        Dictionary<string, ReconciledIso639Entity> entitiesBySet3 = CreateSet3EntityIndex(entities);
        Dictionary<string, Iso639Set5HierarchyEntry> hierarchyByCode = CreateHierarchyIndex(set5Hierarchy);
        List<NameValue> names = CreateNames(
            entities,
            set3Names,
            entitiesBySet3,
            ianaRegistry.Records,
            aliasData.Aliases);

        return new GeneratedSourceFile[]
        {
            new GeneratedSourceFile(
                "Internal/Data/GeneratedLanguageEntries.csx",
                EmitLanguageEntries(entities, entityIndexes, hierarchyByCode)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedLanguageNames.csx",
                EmitNames(names)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedAliases.csx",
                EmitAliases(aliasData.Aliases)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedRetirements.csx",
                EmitRetirements(retirements)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedRelationships.csx",
                EmitRelationships(macrolanguageMappings, set5Hierarchy.Relationships)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedIanaRegistry.csx",
                EmitIanaRegistry(ianaRegistry.Records)),
            new GeneratedSourceFile(
                "Internal/Data/GeneratedDataMetadata.csx",
                EmitMetadata(
                    entities,
                    names.Count,
                    aliasData.Aliases,
                    retirements.Length,
                    macrolanguageMappings.Length + set5Hierarchy.Relationships.Length,
                    ianaRegistry,
                    manifest))
        };
    }

    private static ReconciledIso639Entity[] SortEntities(ReconciledIso639Entity[] source)
    {
        ReconciledIso639Entity[] entities = new ReconciledIso639Entity[source.Length];
        Array.Copy(source, entities, source.Length);
        Array.Sort(
            entities,
            delegate (ReconciledIso639Entity left, ReconciledIso639Entity right)
            {
                return string.CompareOrdinal(left.Key, right.Key);
            });
        return entities;
    }

    private static Dictionary<string, int> CreateEntityIndexes(ReconciledIso639Entity[] entities)
    {
        Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < entities.Length; index++)
        {
            indexes.Add(entities[index].Key, index);
        }

        return indexes;
    }

    private static Dictionary<string, ReconciledIso639Entity> CreateSet3EntityIndex(
        ReconciledIso639Entity[] entities)
    {
        Dictionary<string, ReconciledIso639Entity> index =
            new Dictionary<string, ReconciledIso639Entity>(StringComparer.Ordinal);
        foreach (ReconciledIso639Entity entity in entities)
        {
            if (entity.Set3Code != null)
            {
                index.Add(entity.Set3Code, entity);
            }
        }

        return index;
    }

    private static Dictionary<string, Iso639Set5HierarchyEntry> CreateHierarchyIndex(
        Iso639Set5HierarchyData hierarchy)
    {
        Dictionary<string, Iso639Set5HierarchyEntry> index =
            new Dictionary<string, Iso639Set5HierarchyEntry>(StringComparer.Ordinal);
        foreach (Iso639Set5HierarchyEntry entry in hierarchy.Entries)
        {
            index.Add(entry.Code, entry);
        }

        return index;
    }

    private static string EmitLanguageEntries(
        ReconciledIso639Entity[] entities,
        Dictionary<string, int> entityIndexes,
        Dictionary<string, Iso639Set5HierarchyEntry> hierarchyByCode)
    {
        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedLanguageEntries");
        builder.AppendLine("    internal static readonly LanguageRow[] Rows = new LanguageRow[]");
        builder.AppendLine("    {");
        foreach (ReconciledIso639Entity entity in entities)
        {
            builder.AppendLine(
                "        new LanguageRow(" + StringValue(entity.Key) + ", " +
                StringValue(entity.Alpha2Code) + ", " + StringValue(entity.Set2TerminologyCode) + ", " +
                StringValue(entity.Set2BibliographicCode) + ", " + StringValue(entity.Set3Code) + ", " +
                StringValue(entity.Set5Code) + ", " + ScopeValue(entity) + ", " + TypeValue(entity) + ", " +
                "LanguageCodeStatus.Active, " + GroupValue(entity, hierarchyByCode) + ", " +
                StringValue(entity.Set3Record?.Comment) + "),");
        }

        builder.AppendLine("    };");
        builder.AppendLine("");
        List<CodeSeedValue> seeds = CreateCodeSeeds(entities, entityIndexes);
        builder.AppendLine("    internal static readonly CodeIndexSeedRow[] CodeIndexSeeds = new CodeIndexSeedRow[]");
        builder.AppendLine("    {");
        foreach (CodeSeedValue seed in seeds)
        {
            builder.AppendLine(
                "        new CodeIndexSeedRow(" + StringValue(seed.Value) + ", " +
                seed.EntityIndex.ToString(CultureInfo.InvariantCulture) + ", " + CodeFormValue(seed.Form) + ", " +
                SetValue(seed.Form) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static List<CodeSeedValue> CreateCodeSeeds(
        ReconciledIso639Entity[] entities,
        Dictionary<string, int> entityIndexes)
    {
        List<CodeSeedValue> seeds = new List<CodeSeedValue>();
        foreach (ReconciledIso639Entity entity in entities)
        {
            int entityIndex = entityIndexes[entity.Key];
            AddCodeSeed(seeds, entity.Alpha2Code, entityIndex, 0);
            AddCodeSeed(seeds, entity.Set2TerminologyCode, entityIndex, 1);
            AddCodeSeed(seeds, entity.Set2BibliographicCode, entityIndex, 2);
            AddCodeSeed(seeds, entity.Set3Code, entityIndex, 3);
            AddCodeSeed(seeds, entity.Set5Code, entityIndex, 4);
        }

        seeds.Sort(
            delegate (CodeSeedValue left, CodeSeedValue right)
            {
                int comparison = string.CompareOrdinal(left.Value, right.Value);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = left.Form.CompareTo(right.Form);
                return comparison != 0 ? comparison : left.EntityIndex.CompareTo(right.EntityIndex);
            });
        return seeds;
    }

    private static void AddCodeSeed(List<CodeSeedValue> seeds, string? value, int entityIndex, int form)
    {
        if (value != null)
        {
            seeds.Add(new CodeSeedValue(value, entityIndex, form));
        }
    }

    private static List<NameValue> CreateNames(
        ReconciledIso639Entity[] entities,
        Iso639Set3NameRecord[] set3Names,
        Dictionary<string, ReconciledIso639Entity> entitiesBySet3,
        IanaRegistryRecord[] ianaRecords,
        ReconciledCodeAlias[] aliases)
    {
        List<NameValue> names = new List<NameValue>();
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledIso639Entity entity in entities)
        {
            if (entity.Set3Record != null)
            {
                AddName(names, keys, entity.Key, entity.Set3Record.ReferenceName, "en", 0, 3, true);
            }
            else if (entity.Set2Record != null)
            {
                AddName(names, keys, entity.Key, entity.Set2Record.EnglishNames[0], "en", 0, 2, true);
            }
            else if (entity.Set5Record != null)
            {
                AddName(names, keys, entity.Key, entity.Set5Record.EnglishNames[0], "en", 0, 4, true);
            }

            if (entity.Set2Record != null)
            {
                foreach (string name in entity.Set2Record.EnglishNames)
                {
                    AddName(names, keys, entity.Key, name, "en", 3, 2, false);
                }

                foreach (string name in entity.Set2Record.FrenchNames)
                {
                    AddName(names, keys, entity.Key, name, "fr", 4, 2, false);
                }
            }

            if (entity.Set5Record != null)
            {
                foreach (string name in entity.Set5Record.EnglishNames)
                {
                    AddName(names, keys, entity.Key, name, "en", 3, 4, false);
                }

                foreach (string name in entity.Set5Record.FrenchNames)
                {
                    AddName(names, keys, entity.Key, name, "fr", 4, 4, false);
                }
            }
        }

        foreach (Iso639Set3NameRecord sourceName in set3Names)
        {
            ReconciledIso639Entity entity = entitiesBySet3[sourceName.Id];
            AddName(names, keys, entity.Key, sourceName.PrintName, "en", 1, 3, false);
            AddName(names, keys, entity.Key, sourceName.InvertedName, "en", 2, 3, false);
        }

        Dictionary<IanaRegistryRecord, string> ianaEntityKeys = CreateIanaEntityKeys(aliases);
        Dictionary<string, string?> entityKeysByCode = CreateEntityKeysByCode(entities);
        foreach (IanaRegistryRecord record in ianaRecords)
        {
            if (record.RecordType != IanaRegistryRecordType.Language &&
                record.RecordType != IanaRegistryRecordType.Extlang &&
                record.RecordType != IanaRegistryRecordType.Grandfathered &&
                record.RecordType != IanaRegistryRecordType.Redundant)
            {
                continue;
            }

            string? entityKey = null;
            if (!ianaEntityKeys.TryGetValue(record, out entityKey))
            {
                string value = record.Subtag ?? record.Tag ?? "";
                string primaryCode = value.Split('-')[0];
                entityKeysByCode.TryGetValue(primaryCode, out entityKey);
            }

            if (entityKey != null)
            {
                foreach (string description in record.Descriptions)
                {
                    AddName(names, keys, entityKey, description, "en", 5, 5, false);
                }
            }
        }

        names.Sort(
            delegate (NameValue left, NameValue right)
            {
                int comparison = string.CompareOrdinal(left.EntityKey, right.EntityKey);
                if (comparison == 0)
                {
                    comparison = left.Kind.CompareTo(right.Kind);
                }

                if (comparison == 0)
                {
                    comparison = left.Source.CompareTo(right.Source);
                }

                if (comparison == 0)
                {
                    comparison = string.CompareOrdinal(left.NameLanguage, right.NameLanguage);
                }

                return comparison != 0 ? comparison : string.CompareOrdinal(left.Text, right.Text);
            });
        return names;
    }

    private static Dictionary<IanaRegistryRecord, string> CreateIanaEntityKeys(
        ReconciledCodeAlias[] aliases)
    {
        Dictionary<IanaRegistryRecord, string> keys = new Dictionary<IanaRegistryRecord, string>();
        foreach (ReconciledCodeAlias alias in aliases)
        {
            if (alias.EntityKey == null)
            {
                continue;
            }

            foreach (ReconciledCodeAliasProvenance provenance in alias.Provenance)
            {
                if (provenance.IanaRecord != null)
                {
                    keys[provenance.IanaRecord] = alias.EntityKey;
                }
            }
        }

        return keys;
    }

    private static Dictionary<string, string?> CreateEntityKeysByCode(ReconciledIso639Entity[] entities)
    {
        Dictionary<string, string?> keys = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (ReconciledIso639Entity entity in entities)
        {
            AddEntityCode(keys, entity.Alpha2Code, entity.Key);
            AddEntityCode(keys, entity.Set2TerminologyCode, entity.Key);
            AddEntityCode(keys, entity.Set2BibliographicCode, entity.Key);
            AddEntityCode(keys, entity.Set3Code, entity.Key);
        }

        return keys;
    }

    private static void AddEntityCode(Dictionary<string, string?> keys, string? code, string entityKey)
    {
        if (code == null)
        {
            return;
        }

        if (keys.TryGetValue(code, out string? previous) && previous != entityKey)
        {
            keys[code] = null;
        }
        else if (!keys.ContainsKey(code))
        {
            keys.Add(code, entityKey);
        }
    }

    private static void AddName(
        List<NameValue> names,
        HashSet<string> keys,
        string entityKey,
        string text,
        string language,
        int kind,
        int source,
        bool isReference)
    {
        string key = entityKey + "\0" + kind + "\0" + source + "\0" + language + "\0" + text;
        if (keys.Add(key))
        {
            names.Add(new NameValue(entityKey, text, language, kind, source, isReference));
        }
    }

    private static string EmitNames(List<NameValue> names)
    {
        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedLanguageNames");
        builder.AppendLine("    internal static readonly NameRow[] Rows = new NameRow[]");
        builder.AppendLine("    {");
        foreach (NameValue name in names)
        {
            builder.AppendLine(
                "        new NameRow(" + StringValue(name.EntityKey) + ", " + StringValue(name.Text) + ", " +
                StringValue(name.NameLanguage) + ", " + NameKindValue(name.Kind) + ", " +
                SourceValue(name.Source) + ", " + BoolValue(name.IsReference) + "),");
        }

        builder.AppendLine("    };");
        builder.AppendLine("");
        List<NameSeedValue> seeds = new List<NameSeedValue>(names.Count);
        for (int index = 0; index < names.Count; index++)
        {
            seeds.Add(new NameSeedValue(names[index].Text, index));
        }

        seeds.Sort(
            delegate (NameSeedValue left, NameSeedValue right)
            {
                int comparison = string.CompareOrdinal(left.Value, right.Value);
                return comparison != 0 ? comparison : left.NameIndex.CompareTo(right.NameIndex);
            });
        builder.AppendLine("    internal static readonly NameIndexSeedRow[] NameIndexSeeds = new NameIndexSeedRow[]");
        builder.AppendLine("    {");
        foreach (NameSeedValue seed in seeds)
        {
            builder.AppendLine(
                "        new NameIndexSeedRow(" + StringValue(seed.Value) + ", " +
                seed.NameIndex.ToString(CultureInfo.InvariantCulture) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string EmitAliases(ReconciledCodeAlias[] sourceAliases)
    {
        List<AliasValue> aliases = new List<AliasValue>();
        foreach (ReconciledCodeAlias alias in sourceAliases)
        {
            foreach (ReconciledCodeAliasProvenance provenance in alias.Provenance)
            {
                aliases.Add(new AliasValue(alias, provenance));
            }
        }

        aliases.Sort(
            delegate (AliasValue left, AliasValue right)
            {
                int comparison = string.CompareOrdinal(left.Alias.Value, right.Alias.Value);
                if (comparison == 0)
                {
                    comparison = left.Alias.Kind.CompareTo(right.Alias.Kind);
                }

                if (comparison == 0)
                {
                    comparison = string.CompareOrdinal(left.Alias.EntityKey, right.Alias.EntityKey);
                }

                if (comparison == 0)
                {
                    comparison = left.Provenance.Source.CompareTo(right.Provenance.Source);
                }

                return comparison != 0
                    ? comparison
                    : string.CompareOrdinal(left.Provenance.RecordIdentifier, right.Provenance.RecordIdentifier);
            });

        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedAliases");
        builder.AppendLine("    internal static readonly AliasRow[] Rows = new AliasRow[]");
        builder.AppendLine("    {");
        foreach (AliasValue value in aliases)
        {
            builder.AppendLine(
                "        new AliasRow(" + StringValue(value.Alias.EntityKey) + ", " +
                StringValue(value.Alias.Value) + ", " + AliasKindValue(value.Alias.Kind) + ", " +
                StringValue(value.Alias.PreferredValue) + ", " + SourceValue(value.Provenance.Source) + ", " +
                StringValue(value.Provenance.RecordIdentifier) + ", " + BoolValue(value.Alias.IsDeprecated) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string EmitRetirements(Iso639Set3RetirementRecord[] source)
    {
        Iso639Set3RetirementRecord[] records = new Iso639Set3RetirementRecord[source.Length];
        Array.Copy(source, records, source.Length);
        Array.Sort(records, delegate (Iso639Set3RetirementRecord left, Iso639Set3RetirementRecord right)
        {
            return string.CompareOrdinal(left.Id, right.Id);
        });

        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedRetirements");
        builder.AppendLine("    internal static readonly RetirementRow[] Rows = new RetirementRow[]");
        builder.AppendLine("    {");
        foreach (Iso639Set3RetirementRecord record in records)
        {
            builder.AppendLine(
                "        new RetirementRow(" + StringValue(record.Id) + ", " + StringValue(record.ReferenceName) +
                ", " + RetirementReasonValue(record.Reason) + ", " + StringArray(record.ReplacementCodes) + ", " +
                StringValue(record.Remedy) + ", " + StringValue(FormatDate(record.EffectiveDate)) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string EmitRelationships(
        Iso639Set3MacrolanguageMapping[] macrolanguages,
        Iso639Set5HierarchyRelationship[] hierarchy)
    {
        List<RelationshipValue> relationships = new List<RelationshipValue>();
        foreach (Iso639Set3MacrolanguageMapping mapping in macrolanguages)
        {
            relationships.Add(new RelationshipValue(
                mapping.MacrolanguageId,
                mapping.IndividualLanguageId,
                0,
                mapping.Status == Iso639Set3MappingStatus.Active ? 0 : 1,
                3));
        }

        foreach (Iso639Set5HierarchyRelationship relationship in hierarchy)
        {
            relationships.Add(new RelationshipValue(
                relationship.ParentCode,
                relationship.ChildCode,
                1,
                0,
                4));
        }

        relationships.Sort(
            delegate (RelationshipValue left, RelationshipValue right)
            {
                int comparison = left.Kind.CompareTo(right.Kind);
                if (comparison == 0)
                {
                    comparison = string.CompareOrdinal(left.ParentCode, right.ParentCode);
                }

                if (comparison == 0)
                {
                    comparison = string.CompareOrdinal(left.ChildCode, right.ChildCode);
                }

                return comparison != 0 ? comparison : left.Status.CompareTo(right.Status);
            });

        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedRelationships");
        builder.AppendLine("    internal static readonly RelationshipRow[] Rows = new RelationshipRow[]");
        builder.AppendLine("    {");
        foreach (RelationshipValue relationship in relationships)
        {
            builder.AppendLine(
                "        new RelationshipRow(" + StringValue(relationship.ParentCode) + ", " +
                StringValue(relationship.ChildCode) + ", " + RelationshipKindValue(relationship.Kind) + ", " +
                RelationshipStatusValue(relationship.Status) + ", " + SourceValue(relationship.Source) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string EmitIanaRegistry(IanaRegistryRecord[] source)
    {
        IanaRegistryRecord[] records = new IanaRegistryRecord[source.Length];
        Array.Copy(source, records, source.Length);
        Array.Sort(
            records,
            delegate (IanaRegistryRecord left, IanaRegistryRecord right)
            {
                int comparison = left.RecordType.CompareTo(right.RecordType);
                string leftValue = left.Subtag ?? left.Tag ?? "";
                string rightValue = right.Subtag ?? right.Tag ?? "";
                return comparison != 0 ? comparison : string.CompareOrdinal(leftValue, rightValue);
            });

        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedIanaRegistry");
        builder.AppendLine("    internal static readonly IanaSubtagRow[] Rows = new IanaSubtagRow[]");
        builder.AppendLine("    {");
        foreach (IanaRegistryRecord record in records)
        {
            string value = record.Subtag ?? record.Tag ?? "";
            builder.AppendLine(
                "        new IanaSubtagRow(" + IanaTypeValue(record.RecordType) + ", " + StringValue(value) + ", " +
                StringArray(record.Descriptions) + ", " + StringValue(FormatDate(record.AddedDate)) + ", " +
                StringValue(FormatDate(record.DeprecatedDate)) + ", " + StringValue(record.PreferredValue) + ", " +
                StringArray(record.Prefixes) + ", " + StringValue(record.SuppressScript) + ", " +
                StringValue(record.Macrolanguage) + ", " + StringValue(record.Scope) + ", " +
                StringArray(record.Comments) + "),");
        }

        builder.AppendLine("    };");
        builder.AppendLine("");
        List<IanaSeedValue> seeds = new List<IanaSeedValue>(records.Length);
        for (int index = 0; index < records.Length; index++)
        {
            IanaRegistryRecord record = records[index];
            seeds.Add(new IanaSeedValue(record.RecordType, record.Subtag ?? record.Tag ?? "", index));
        }

        seeds.Sort(
            delegate (IanaSeedValue left, IanaSeedValue right)
            {
                int comparison = string.CompareOrdinal(left.Value, right.Value);
                return comparison != 0 ? comparison : left.RecordType.CompareTo(right.RecordType);
            });
        builder.AppendLine("    internal static readonly IanaIndexSeedRow[] IndexSeeds = new IanaIndexSeedRow[]");
        builder.AppendLine("    {");
        foreach (IanaSeedValue seed in seeds)
        {
            builder.AppendLine(
                "        new IanaIndexSeedRow(" + IanaTypeValue(seed.RecordType) + ", " + StringValue(seed.Value) +
                ", " + seed.RecordIndex.ToString(CultureInfo.InvariantCulture) + "),");
        }

        builder.AppendLine("    };");
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string EmitMetadata(
        ReconciledIso639Entity[] entities,
        int nameCount,
        ReconciledCodeAlias[] aliases,
        int retirementCount,
        int relationshipCount,
        IanaLanguageSubtagRegistry registry,
        DataManifest manifest)
    {
        int set1Count = 0;
        int set2Count = 0;
        int set3Count = 0;
        int set5Count = 0;
        foreach (ReconciledIso639Entity entity in entities)
        {
            if (entity.InSet1)
            {
                set1Count++;
            }

            if (entity.InSet2)
            {
                set2Count++;
            }

            if (entity.InSet3)
            {
                set3Count++;
            }

            if (entity.InSet5)
            {
                set5Count++;
            }
        }

        int aliasRowCount = 0;
        foreach (ReconciledCodeAlias alias in aliases)
        {
            aliasRowCount += alias.Provenance.Length;
        }

        int[] ianaCounts = new int[7];
        foreach (IanaRegistryRecord record in registry.Records)
        {
            ianaCounts[(int)record.RecordType]++;
        }

        string set2Date = GetUpstreamDate(manifest, "iso-639-2-code-table");
        string set3Date = GetUpstreamDate(manifest, "iso-639-3-code-table");
        string set3NamesDate = GetUpstreamDate(manifest, "iso-639-3-name-index");
        string macrolanguageDate = GetUpstreamDate(manifest, "iso-639-3-macrolanguage-mappings");
        string retirementDate = GetUpstreamDate(manifest, "iso-639-3-retirements");
        string set5Date = GetUpstreamDate(manifest, "iso-639-5-code-table");
        string set5HierarchyDate = GetUpstreamDate(manifest, "iso-639-5-hierarchy");
        string ianaDate = GetUpstreamDate(manifest, "iana-language-subtag-registry");
        if (ianaDate != FormatDate(registry.FileDate))
        {
            throw new InvalidOperationException("The manifest and IANA registry file dates do not agree.");
        }

        CSharpSourceBuilder builder = CreateTableBuilder("GeneratedDataMetadata");
        AppendStringConstant(builder, "IsoEdition", "ISO 639:2023");
        AppendStringConstant(builder, "DataVersion", "iso6393-" + set3Date + "+iana-" + ianaDate);
        AppendIntegerConstant(builder, "ManifestVersion", manifest.ManifestVersion);
        AppendIntegerConstant(builder, "GeneratorSchemaVersion", manifest.GeneratorSchemaVersion);
        AppendStringConstant(builder, "RetrievalDate", FormatDate(manifest.RetrievalDate));
        AppendStringConstant(builder, "Iso639Set2UpstreamDate", set2Date);
        AppendStringConstant(builder, "Iso639Set3CoreUpstreamDate", set3Date);
        AppendStringConstant(builder, "Iso639Set3NamesUpstreamDate", set3NamesDate);
        AppendStringConstant(builder, "Iso639Set3MacrolanguagesUpstreamDate", macrolanguageDate);
        AppendStringConstant(builder, "Iso639Set3RetirementsUpstreamDate", retirementDate);
        AppendStringConstant(builder, "Iso639Set5UpstreamDate", set5Date);
        AppendStringConstant(builder, "Iso639Set5HierarchyUpstreamDate", set5HierarchyDate);
        AppendStringConstant(builder, "IanaRegistryUpstreamDate", ianaDate);
        AppendStringConstant(builder, "Rfc4647Version", GetUpstreamDate(manifest, "ietf-rfc-4647"));
        AppendStringConstant(builder, "Rfc5646Version", GetUpstreamDate(manifest, "ietf-rfc-5646"));
        AppendIntegerConstant(builder, "ActiveEntityCount", entities.Length);
        AppendIntegerConstant(builder, "Set1RecordCount", set1Count);
        AppendIntegerConstant(builder, "Set2RecordCount", set2Count);
        AppendIntegerConstant(builder, "Set3RecordCount", set3Count);
        AppendIntegerConstant(builder, "Set5RecordCount", set5Count);
        AppendIntegerConstant(builder, "RetiredCodeCount", retirementCount);
        AppendIntegerConstant(builder, "NameCount", nameCount);
        AppendIntegerConstant(builder, "AliasCount", aliasRowCount);
        AppendIntegerConstant(builder, "RelationshipCount", relationshipCount);
        AppendIntegerConstant(builder, "IanaRecordCount", registry.Records.Length);
        AppendIntegerConstant(builder, "LanguageSubtagCount", ianaCounts[0]);
        AppendIntegerConstant(builder, "ExtlangSubtagCount", ianaCounts[1]);
        AppendIntegerConstant(builder, "ScriptSubtagCount", ianaCounts[2]);
        AppendIntegerConstant(builder, "RegionSubtagCount", ianaCounts[3]);
        AppendIntegerConstant(builder, "VariantSubtagCount", ianaCounts[4]);
        AppendIntegerConstant(builder, "GrandfatheredTagCount", ianaCounts[5]);
        AppendIntegerConstant(builder, "RedundantTagCount", ianaCounts[6]);
        AppendIntegerConstant(
            builder,
            "RegisteredSubtagCount",
            ianaCounts[0] + ianaCounts[1] + ianaCounts[2] + ianaCounts[3] + ianaCounts[4]);
        FinishTableBuilder(builder);
        return builder.Build();
    }

    private static string GetUpstreamDate(DataManifest manifest, string sourceId)
    {
        string? value = manifest.GetSource(sourceId).UpstreamDate;
        return value ?? throw new InvalidOperationException(
            $"Data manifest source '{sourceId}' has no upstream date.");
    }

    private static void AppendStringConstant(CSharpSourceBuilder builder, string name, string value)
    {
        builder.AppendLine("    internal const string " + name + " = " + StringValue(value) + ";");
    }

    private static void AppendIntegerConstant(CSharpSourceBuilder builder, string name, int value)
    {
        builder.AppendLine(
            "    internal const int " + name + " = " + value.ToString(CultureInfo.InvariantCulture) + ";");
    }

    private static CSharpSourceBuilder CreateTableBuilder(string className)
    {
        CSharpSourceBuilder builder = new CSharpSourceBuilder();
        builder.AppendLine("namespace languagecodes;");
        builder.AppendLine("");
        builder.AppendLine("internal static class " + className);
        builder.AppendLine("{");
        return builder;
    }

    private static void FinishTableBuilder(CSharpSourceBuilder builder)
    {
        builder.AppendLine("}");
    }

    private static string StringValue(string? value)
    {
        return value == null ? "null" : DeterministicCSharp.StringLiteral(value);
    }

    private static string StringArray(string[] values)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("new string[] {");
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(' ');
            builder.Append(DeterministicCSharp.StringLiteral(values[index]));
        }

        if (values.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string? FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string BoolValue(bool value)
    {
        return value ? "true" : "false";
    }

    private static string ScopeValue(ReconciledIso639Entity entity)
    {
        if (entity.Set3Record != null)
        {
            switch (entity.Set3Record.Scope)
            {
                case Iso639Set3Scope.Individual:
                    return "LanguageScope.Individual";
                case Iso639Set3Scope.Macrolanguage:
                    return "LanguageScope.Macrolanguage";
                default:
                    return "LanguageScope.Special";
            }
        }

        return "LanguageScope.Collection";
    }

    private static string TypeValue(ReconciledIso639Entity entity)
    {
        if (entity.Set3Record == null)
        {
            return "LanguageType.Unknown";
        }

        return "LanguageType." + entity.Set3Record.LanguageType;
    }

    private static string GroupValue(
        ReconciledIso639Entity entity,
        Dictionary<string, Iso639Set5HierarchyEntry> hierarchyByCode)
    {
        if (entity.Set5Code == null)
        {
            return "ReconciledGroupKind.None";
        }

        return "ReconciledGroupKind." + hierarchyByCode[entity.Set5Code].GroupKind;
    }

    private static string CodeFormValue(int value)
    {
        string[] names = { "Alpha2", "Set2Terminology", "Set2Bibliographic", "Set3", "Set5" };
        return "LanguageCodeForm." + names[value];
    }

    private static string SetValue(int value)
    {
        string[] names = { "Set1", "Set2", "Set2", "Set3", "Set5" };
        return "Iso639Set." + names[value];
    }

    private static string NameKindValue(int value)
    {
        string[] names = { "Reference", "Print", "Inverted", "English", "French", "IanaDescription" };
        return "LanguageNameKind." + names[value];
    }

    private static string AliasKindValue(ReconciledCodeAliasKind value)
    {
        switch (value)
        {
            case ReconciledCodeAliasKind.Alpha2:
                return "LanguageAliasKind.Alpha2";
            case ReconciledCodeAliasKind.Set2Terminology:
                return "LanguageAliasKind.Set2Terminology";
            case ReconciledCodeAliasKind.Set2Bibliographic:
                return "LanguageAliasKind.Set2Bibliographic";
            case ReconciledCodeAliasKind.Overlong:
                return "LanguageAliasKind.Overlong";
            case ReconciledCodeAliasKind.IanaDeprecated:
                return "LanguageAliasKind.IanaDeprecated";
            case ReconciledCodeAliasKind.RetiredSet3:
                return "LanguageAliasKind.RetiredSet3";
            case ReconciledCodeAliasKind.Grandfathered:
                return "LanguageAliasKind.Grandfathered";
            default:
                return "LanguageAliasKind.IanaPreferred";
        }
    }

    private static string RetirementReasonValue(Iso639Set3RetirementReason value)
    {
        return "LanguageRetirementReason." + value;
    }

    private static string RelationshipKindValue(int value)
    {
        return value == 0
            ? "LanguageRelationshipKind.MacrolanguageMember"
            : "LanguageRelationshipKind.Set5ParentChild";
    }

    private static string RelationshipStatusValue(int value)
    {
        return value == 0
            ? "ReconciledRelationshipStatus.Active"
            : "ReconciledRelationshipStatus.Retired";
    }

    private static string SourceValue(int value)
    {
        string[] names = { "Unknown", "Iso639Set1", "Iso639Set2", "Iso639Set3", "Iso639Set5", "Iana" };
        return "ReconciledSource." + names[value];
    }

    private static string SourceValue(ReconciledCodeAliasSource value)
    {
        switch (value)
        {
            case ReconciledCodeAliasSource.Iso639Set1:
                return "ReconciledSource.Iso639Set1";
            case ReconciledCodeAliasSource.Iso639Set2:
                return "ReconciledSource.Iso639Set2";
            case ReconciledCodeAliasSource.Iso639Set3:
                return "ReconciledSource.Iso639Set3";
            default:
                return "ReconciledSource.Iana";
        }
    }

    private static string IanaTypeValue(IanaRegistryRecordType value)
    {
        return "LanguageSubtagType." + value;
    }

    private sealed class CodeSeedValue
    {
        internal CodeSeedValue(string value, int entityIndex, int form)
        {
            Value = value;
            EntityIndex = entityIndex;
            Form = form;
        }

        internal string Value { get; }
        internal int EntityIndex { get; }
        internal int Form { get; }
    }

    private sealed class NameValue
    {
        internal NameValue(string entityKey, string text, string nameLanguage, int kind, int source, bool reference)
        {
            EntityKey = entityKey;
            Text = text;
            NameLanguage = nameLanguage;
            Kind = kind;
            Source = source;
            IsReference = reference;
        }

        internal string EntityKey { get; }
        internal string Text { get; }
        internal string NameLanguage { get; }
        internal int Kind { get; }
        internal int Source { get; }
        internal bool IsReference { get; }
    }

    private sealed class NameSeedValue
    {
        internal NameSeedValue(string value, int nameIndex)
        {
            Value = value;
            NameIndex = nameIndex;
        }

        internal string Value { get; }
        internal int NameIndex { get; }
    }

    private sealed class AliasValue
    {
        internal AliasValue(ReconciledCodeAlias alias, ReconciledCodeAliasProvenance provenance)
        {
            Alias = alias;
            Provenance = provenance;
        }

        internal ReconciledCodeAlias Alias { get; }
        internal ReconciledCodeAliasProvenance Provenance { get; }
    }

    private sealed class RelationshipValue
    {
        internal RelationshipValue(string parentCode, string childCode, int kind, int status, int source)
        {
            ParentCode = parentCode;
            ChildCode = childCode;
            Kind = kind;
            Status = status;
            Source = source;
        }

        internal string ParentCode { get; }
        internal string ChildCode { get; }
        internal int Kind { get; }
        internal int Status { get; }
        internal int Source { get; }
    }

    private sealed class IanaSeedValue
    {
        internal IanaSeedValue(IanaRegistryRecordType recordType, string value, int recordIndex)
        {
            RecordType = recordType;
            Value = value;
            RecordIndex = recordIndex;
        }

        internal IanaRegistryRecordType RecordType { get; }
        internal string Value { get; }
        internal int RecordIndex { get; }
    }
}
