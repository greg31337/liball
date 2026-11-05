using System;
using System.Collections.Generic;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.Validation;

internal static class GeneratorIntegrityValidator
{
    private const int MinimumSet2Records = 450;
    private const int MinimumSet3Records = 7500;
    private const int MinimumSet3Names = 8000;
    private const int MinimumMacrolanguageMappings = 400;
    private const int MinimumRetirements = 350;
    private const int MinimumSet5Records = 100;
    private const int MinimumSet5Relationships = 75;
    private const int MinimumIanaRecords = 9000;
    private const int MinimumReconciledEntities = 8000;
    private const int MinimumAliases = 2000;

    internal static string[] FindViolations(
        Iso639Set2Data set2Data,
        Iso639Set3Record[] set3Records,
        Iso639Set3NameRecord[] set3Names,
        Iso639Set3MacrolanguageMapping[] macrolanguageMappings,
        Iso639Set3RetirementRecord[] retirementRecords,
        Iso639Set5Record[] set5Records,
        Iso639Set5HierarchyData set5Hierarchy,
        IanaLanguageSubtagRegistry ianaRegistry,
        ReconciledIso639Data reconciledData,
        ReconciledCodeAliasData aliasData)
    {
        ArgumentNullException.ThrowIfNull(set2Data);
        ArgumentNullException.ThrowIfNull(set3Records);
        ArgumentNullException.ThrowIfNull(set3Names);
        ArgumentNullException.ThrowIfNull(macrolanguageMappings);
        ArgumentNullException.ThrowIfNull(retirementRecords);
        ArgumentNullException.ThrowIfNull(set5Records);
        ArgumentNullException.ThrowIfNull(set5Hierarchy);
        ArgumentNullException.ThrowIfNull(ianaRegistry);
        ArgumentNullException.ThrowIfNull(reconciledData);
        ArgumentNullException.ThrowIfNull(aliasData);

        List<string> violations = new List<string>();
        AddCountFloorViolations(
            set2Data,
            set3Records,
            set3Names,
            macrolanguageMappings,
            retirementRecords,
            set5Records,
            set5Hierarchy,
            ianaRegistry,
            reconciledData,
            aliasData,
            violations);

        Dictionary<string, Iso639Set3Record> activeSet3 = IndexActiveSet3(set3Records, violations);
        Dictionary<string, Iso639Set3RetirementRecord> retiredSet3 =
            IndexRetiredSet3(retirementRecords, activeSet3, violations);
        HashSet<string> set5Codes = IndexSet5(set5Records, violations);

        AddEnumAndCodeViolations(
            set2Data,
            set3Records,
            macrolanguageMappings,
            retirementRecords,
            set5Hierarchy,
            ianaRegistry,
            aliasData,
            violations);
        AddDateViolations(retirementRecords, ianaRegistry, violations);
        AddReferenceViolations(
            set3Names,
            macrolanguageMappings,
            retirementRecords,
            set5Hierarchy,
            activeSet3,
            retiredSet3,
            set5Codes,
            violations);
        AddPrivateUseRangeViolations(set2Data, ianaRegistry, violations);
        AddReconciliationViolations(
            set2Data,
            set3Records,
            set5Records,
            reconciledData,
            violations);
        AddAliasViolations(reconciledData, aliasData, violations);
        return violations.ToArray();
    }

    private static void AddCountFloorViolations(
        Iso639Set2Data set2Data,
        Iso639Set3Record[] set3Records,
        Iso639Set3NameRecord[] set3Names,
        Iso639Set3MacrolanguageMapping[] macrolanguageMappings,
        Iso639Set3RetirementRecord[] retirementRecords,
        Iso639Set5Record[] set5Records,
        Iso639Set5HierarchyData set5Hierarchy,
        IanaLanguageSubtagRegistry ianaRegistry,
        ReconciledIso639Data reconciledData,
        ReconciledCodeAliasData aliasData,
        List<string> violations)
    {
        AddCountFloor("ISO 639 Set 2 records", set2Data.Records.Length, MinimumSet2Records, violations);
        AddCountFloor("ISO 639 Set 2 reserved ranges", set2Data.ReservedRanges.Length, 1, violations);
        AddCountFloor("ISO 639 Set 3 records", set3Records.Length, MinimumSet3Records, violations);
        AddCountFloor("ISO 639 Set 3 names", set3Names.Length, MinimumSet3Names, violations);
        AddCountFloor(
            "ISO 639 Set 3 macrolanguage mappings",
            macrolanguageMappings.Length,
            MinimumMacrolanguageMappings,
            violations);
        AddCountFloor(
            "ISO 639 Set 3 retirements",
            retirementRecords.Length,
            MinimumRetirements,
            violations);
        AddCountFloor("ISO 639 Set 5 records", set5Records.Length, MinimumSet5Records, violations);
        AddCountFloor(
            "ISO 639 Set 5 hierarchy entries",
            set5Hierarchy.Entries.Length,
            MinimumSet5Records,
            violations);
        AddCountFloor(
            "ISO 639 Set 5 relationships",
            set5Hierarchy.Relationships.Length,
            MinimumSet5Relationships,
            violations);
        AddCountFloor("IANA registry records", ianaRegistry.Records.Length, MinimumIanaRecords, violations);
        AddCountFloor(
            "reconciled entities",
            reconciledData.Entities.Length,
            MinimumReconciledEntities,
            violations);
        AddCountFloor("typed aliases", aliasData.Aliases.Length, MinimumAliases, violations);
    }

    private static void AddCountFloor(
        string name,
        int actualCount,
        int minimumCount,
        List<string> violations)
    {
        if (actualCount < minimumCount)
        {
            violations.Add($"{name} collapsed to {actualCount}; expected at least {minimumCount}.");
        }
    }

    private static Dictionary<string, Iso639Set3Record> IndexActiveSet3(
        Iso639Set3Record[] records,
        List<string> violations)
    {
        Dictionary<string, Iso639Set3Record> index =
            new Dictionary<string, Iso639Set3Record>(StringComparer.Ordinal);
        foreach (Iso639Set3Record record in records)
        {
            if (!index.TryAdd(record.Id, record))
            {
                violations.Add($"Active ISO 639 Set 3 key '{record.Id}' is duplicated.");
            }
        }

        return index;
    }

    private static Dictionary<string, Iso639Set3RetirementRecord> IndexRetiredSet3(
        Iso639Set3RetirementRecord[] records,
        Dictionary<string, Iso639Set3Record> activeRecords,
        List<string> violations)
    {
        Dictionary<string, Iso639Set3RetirementRecord> index =
            new Dictionary<string, Iso639Set3RetirementRecord>(StringComparer.Ordinal);
        foreach (Iso639Set3RetirementRecord record in records)
        {
            if (activeRecords.ContainsKey(record.Id))
            {
                violations.Add($"ISO 639 Set 3 key '{record.Id}' is both active and retired.");
            }

            if (!index.TryAdd(record.Id, record))
            {
                violations.Add($"Retired ISO 639 Set 3 key '{record.Id}' is duplicated.");
            }
        }

        return index;
    }

    private static HashSet<string> IndexSet5(Iso639Set5Record[] records, List<string> violations)
    {
        HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Iso639Set5Record record in records)
        {
            if (!codes.Add(record.Code))
            {
                violations.Add($"ISO 639 Set 5 key '{record.Code}' is duplicated.");
            }
        }

        return codes;
    }

    private static void AddEnumAndCodeViolations(
        Iso639Set2Data set2Data,
        Iso639Set3Record[] set3Records,
        Iso639Set3MacrolanguageMapping[] macrolanguageMappings,
        Iso639Set3RetirementRecord[] retirementRecords,
        Iso639Set5HierarchyData set5Hierarchy,
        IanaLanguageSubtagRegistry ianaRegistry,
        ReconciledCodeAliasData aliasData,
        List<string> violations)
    {
        foreach (Iso639Set2Record record in set2Data.Records)
        {
            AddCodeShape(record.BibliographicCode, 3, "Set 2 bibliographic", violations);
            AddOptionalCodeShape(record.TerminologyCode, 3, "Set 2 terminology", violations);
            AddOptionalCodeShape(record.Alpha2Code, 2, "Set 1 alpha-2", violations);
        }

        foreach (Iso639Set3Record record in set3Records)
        {
            AddCodeShape(record.Id, 3, "Set 3", violations);
            AddOptionalCodeShape(record.Part2BibliographicCode, 3, "Set 3 Part 2B", violations);
            AddOptionalCodeShape(record.Part2TerminologyCode, 3, "Set 3 Part 2T", violations);
            AddOptionalCodeShape(record.Alpha2Code, 2, "Set 3 Part 1", violations);
            if (!Enum.IsDefined(record.Scope))
            {
                violations.Add($"Set 3 record '{record.Id}' has an unknown scope marker.");
            }

            if (!Enum.IsDefined(record.LanguageType))
            {
                violations.Add($"Set 3 record '{record.Id}' has an unknown language-type marker.");
            }
        }

        foreach (Iso639Set3MacrolanguageMapping mapping in macrolanguageMappings)
        {
            AddCodeShape(mapping.MacrolanguageId, 3, "macrolanguage", violations);
            AddCodeShape(mapping.IndividualLanguageId, 3, "macrolanguage member", violations);
            if (!Enum.IsDefined(mapping.Status))
            {
                violations.Add(
                    $"Macrolanguage mapping '{mapping.MacrolanguageId}' to '{mapping.IndividualLanguageId}' has an unknown status marker.");
            }
        }

        foreach (Iso639Set3RetirementRecord retirement in retirementRecords)
        {
            AddCodeShape(retirement.Id, 3, "retired Set 3", violations);
            if (!Enum.IsDefined(retirement.Reason))
            {
                violations.Add($"Retirement '{retirement.Id}' has an unknown reason marker.");
            }
        }

        foreach (Iso639Set5HierarchyEntry entry in set5Hierarchy.Entries)
        {
            AddCodeShape(entry.Code, 3, "Set 5 hierarchy", violations);
            if (!Enum.IsDefined(entry.GroupKind))
            {
                violations.Add($"Set 5 hierarchy entry '{entry.Code}' has an unknown group-kind marker.");
            }
        }

        foreach (IanaRegistryRecord record in ianaRegistry.Records)
        {
            if (!Enum.IsDefined(record.RecordType))
            {
                violations.Add("An IANA registry record has an unknown type marker.");
            }
        }

        foreach (ReconciledCodeAlias alias in aliasData.Aliases)
        {
            if (!Enum.IsDefined(alias.Kind))
            {
                violations.Add($"Alias '{alias.Value}' has an unknown kind marker.");
            }

            foreach (ReconciledCodeAliasProvenance provenance in alias.Provenance)
            {
                if (!Enum.IsDefined(provenance.Source))
                {
                    violations.Add($"Alias '{alias.Value}' has an unknown provenance source marker.");
                }
            }
        }
    }

    private static void AddCodeShape(
        string value,
        int expectedLength,
        string codeKind,
        List<string> violations)
    {
        if (value.Length != expectedLength)
        {
            violations.Add($"{codeKind} code '{value}' does not contain exactly {expectedLength} letters.");
            return;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] < 'a' || value[index] > 'z')
            {
                violations.Add($"{codeKind} code '{value}' is not lowercase ASCII.");
                return;
            }
        }
    }

    private static void AddOptionalCodeShape(
        string? value,
        int expectedLength,
        string codeKind,
        List<string> violations)
    {
        if (value != null)
        {
            AddCodeShape(value, expectedLength, codeKind, violations);
        }
    }

    private static void AddDateViolations(
        Iso639Set3RetirementRecord[] retirementRecords,
        IanaLanguageSubtagRegistry registry,
        List<string> violations)
    {
        if (registry.FileDate == default(DateOnly))
        {
            violations.Add("The IANA registry has no file date.");
        }

        foreach (Iso639Set3RetirementRecord retirement in retirementRecords)
        {
            if (retirement.EffectiveDate == default(DateOnly))
            {
                violations.Add($"Retirement '{retirement.Id}' has no effective date.");
            }
        }

        foreach (IanaRegistryRecord record in registry.Records)
        {
            string value = record.Subtag ?? record.Tag ?? "";
            if (record.AddedDate == default(DateOnly))
            {
                violations.Add($"IANA record '{value}' has no added date.");
            }
            else if (record.AddedDate > registry.FileDate)
            {
                violations.Add($"IANA record '{value}' was added after the registry file date.");
            }

            if (record.DeprecatedDate != null && record.DeprecatedDate.Value > registry.FileDate)
            {
                violations.Add($"IANA record '{value}' was deprecated after the registry file date.");
            }
        }
    }

    private static void AddReferenceViolations(
        Iso639Set3NameRecord[] names,
        Iso639Set3MacrolanguageMapping[] mappings,
        Iso639Set3RetirementRecord[] retirements,
        Iso639Set5HierarchyData hierarchy,
        Dictionary<string, Iso639Set3Record> activeSet3,
        Dictionary<string, Iso639Set3RetirementRecord> retiredSet3,
        HashSet<string> set5Codes,
        List<string> violations)
    {
        foreach (Iso639Set3NameRecord name in names)
        {
            if (!activeSet3.ContainsKey(name.Id))
            {
                violations.Add($"Set 3 name '{name.PrintName}' points to unknown identifier '{name.Id}'.");
            }
        }

        foreach (Iso639Set3MacrolanguageMapping mapping in mappings)
        {
            if (!activeSet3.TryGetValue(mapping.MacrolanguageId, out Iso639Set3Record? macrolanguage) ||
                macrolanguage.Scope != Iso639Set3Scope.Macrolanguage)
            {
                violations.Add($"Macrolanguage mapping has invalid parent '{mapping.MacrolanguageId}'.");
            }

            bool memberExists = mapping.Status == Iso639Set3MappingStatus.Active
                ? activeSet3.ContainsKey(mapping.IndividualLanguageId)
                : retiredSet3.ContainsKey(mapping.IndividualLanguageId);
            if (!memberExists)
            {
                violations.Add(
                    $"Macrolanguage mapping '{mapping.MacrolanguageId}' has dangling member '{mapping.IndividualLanguageId}'.");
            }
        }

        foreach (Iso639Set3RetirementRecord retirement in retirements)
        {
            foreach (string replacement in retirement.ReplacementCodes)
            {
                if (!activeSet3.ContainsKey(replacement) && !retiredSet3.ContainsKey(replacement))
                {
                    violations.Add($"Retirement '{retirement.Id}' has dangling replacement '{replacement}'.");
                }
            }
        }

        HashSet<string> hierarchyEntries = new HashSet<string>(StringComparer.Ordinal);
        foreach (Iso639Set5HierarchyEntry entry in hierarchy.Entries)
        {
            if (!set5Codes.Contains(entry.Code))
            {
                violations.Add($"Set 5 hierarchy entry '{entry.Code}' has no Set 5 row.");
            }

            if (!hierarchyEntries.Add(entry.Code))
            {
                violations.Add($"Set 5 hierarchy entry '{entry.Code}' is duplicated.");
            }
        }

        foreach (string code in set5Codes)
        {
            if (!hierarchyEntries.Contains(code))
            {
                violations.Add($"Set 5 row '{code}' has no hierarchy entry.");
            }
        }

        HashSet<string> relationships = new HashSet<string>(StringComparer.Ordinal);
        foreach (Iso639Set5HierarchyRelationship relationship in hierarchy.Relationships)
        {
            string key = relationship.ParentCode + "\0" + relationship.ChildCode;
            if (!relationships.Add(key))
            {
                violations.Add(
                    $"Set 5 relationship '{relationship.ParentCode}' to '{relationship.ChildCode}' is duplicated.");
            }

            if (!set5Codes.Contains(relationship.ParentCode) || !set5Codes.Contains(relationship.ChildCode))
            {
                violations.Add(
                    $"Set 5 relationship '{relationship.ParentCode}' to '{relationship.ChildCode}' has a dangling endpoint.");
            }
        }
    }

    private static void AddPrivateUseRangeViolations(
        Iso639Set2Data set2Data,
        IanaLanguageSubtagRegistry registry,
        List<string> violations)
    {
        bool hasSet2LocalUseRange = false;
        foreach (Iso639Set2ReservedRange range in set2Data.ReservedRanges)
        {
            if (string.CompareOrdinal(range.FirstCode, range.LastCode) > 0)
            {
                violations.Add($"Set 2 reserved range '{range.FirstCode}-{range.LastCode}' is reversed.");
            }

            if (range.FirstCode == "qaa" && range.LastCode == "qtz")
            {
                hasSet2LocalUseRange = true;
            }
        }

        if (!hasSet2LocalUseRange)
        {
            violations.Add("The ISO 639 Set 2 qaa-qtz local-use range is missing.");
        }

        HashSet<string> expectedIanaRanges = new HashSet<string>(StringComparer.Ordinal)
        {
            "language:qaa..qtz",
            "script:Qaaa..Qabx",
            "region:QM..QZ",
            "region:XA..XZ"
        };
        foreach (IanaRegistryRecord record in registry.Records)
        {
            string? subtag = record.Subtag;
            if (subtag == null || !subtag.Contains("..", StringComparison.Ordinal))
            {
                continue;
            }

            string key = record.RecordType.ToString().ToLowerInvariant() + ":" + subtag;
            if (!expectedIanaRanges.Remove(key))
            {
                violations.Add($"Unexpected or duplicate IANA private-use range '{key}'.");
            }
        }

        foreach (string missingRange in expectedIanaRanges)
        {
            violations.Add($"Expected IANA private-use range '{missingRange}' is missing.");
        }
    }

    private static void AddReconciliationViolations(
        Iso639Set2Data set2Data,
        Iso639Set3Record[] set3Records,
        Iso639Set5Record[] set5Records,
        ReconciledIso639Data reconciledData,
        List<string> violations)
    {
        HashSet<string> entityKeys = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, ReconciledIso639Entity> set2Owners =
            new Dictionary<string, ReconciledIso639Entity>(StringComparer.Ordinal);
        Dictionary<string, ReconciledIso639Entity> set3Owners =
            new Dictionary<string, ReconciledIso639Entity>(StringComparer.Ordinal);
        Dictionary<string, ReconciledIso639Entity> set5Owners =
            new Dictionary<string, ReconciledIso639Entity>(StringComparer.Ordinal);

        foreach (ReconciledIso639Entity entity in reconciledData.Entities)
        {
            if (!entityKeys.Add(entity.Key))
            {
                violations.Add($"Reconciled entity key '{entity.Key}' is duplicated.");
            }

            if (entity.Set2Record != null)
            {
                AddOwner(set2Owners, entity.Set2Record.BibliographicCode, entity, "Set 2", violations);
                string terminologyCode = entity.Set2Record.TerminologyCode ??
                    entity.Set2Record.BibliographicCode;
                if (entity.Set2BibliographicCode != entity.Set2Record.BibliographicCode ||
                    entity.Set2TerminologyCode != terminologyCode ||
                    entity.Alpha2Code != entity.Set2Record.Alpha2Code)
                {
                    violations.Add($"Reconciled entity '{entity.Key}' does not preserve its Set 2 code forms.");
                }
            }

            if (entity.Set3Record != null)
            {
                AddOwner(set3Owners, entity.Set3Record.Id, entity, "Set 3", violations);
                if (entity.Set3Code != entity.Set3Record.Id ||
                    entity.Alpha2Code != entity.Set3Record.Alpha2Code)
                {
                    violations.Add($"Reconciled entity '{entity.Key}' does not preserve its Set 3 code forms.");
                }
            }

            if (entity.Set5Record != null)
            {
                AddOwner(set5Owners, entity.Set5Record.Code, entity, "Set 5", violations);
                if (entity.Set5Code != entity.Set5Record.Code)
                {
                    violations.Add($"Reconciled entity '{entity.Key}' does not preserve its Set 5 code.");
                }
            }

            if (entity.Set2Record != null && entity.Set3Record != null)
            {
                string terminologyCode = entity.Set2Record.TerminologyCode ??
                    entity.Set2Record.BibliographicCode;
                if (entity.Set3Record.Part2BibliographicCode != entity.Set2Record.BibliographicCode ||
                    entity.Set3Record.Part2TerminologyCode != terminologyCode ||
                    entity.Set3Record.Alpha2Code != entity.Set2Record.Alpha2Code)
                {
                    violations.Add($"Reconciled entity '{entity.Key}' has conflicting Set 2 and Set 3 mappings.");
                }
            }

            if (entity.Set2Record != null && entity.Set5Record != null &&
                entity.Set5Record.Code != entity.Set2Record.BibliographicCode &&
                entity.Set5Record.Code != entity.Set2Record.TerminologyCode)
            {
                violations.Add($"Reconciled entity '{entity.Key}' has conflicting Set 2 and Set 5 codes.");
            }
        }

        AddMissingOwners(set2Data.Records, set2Owners, violations);
        AddMissingOwners(set3Records, set3Owners, violations);
        AddMissingOwners(set5Records, set5Owners, violations);
    }

    private static void AddOwner(
        Dictionary<string, ReconciledIso639Entity> owners,
        string code,
        ReconciledIso639Entity entity,
        string setName,
        List<string> violations)
    {
        if (!owners.TryAdd(code, entity))
        {
            violations.Add($"{setName} row '{code}' belongs to more than one reconciled entity.");
        }
    }

    private static void AddMissingOwners(
        Iso639Set2Record[] records,
        Dictionary<string, ReconciledIso639Entity> owners,
        List<string> violations)
    {
        foreach (Iso639Set2Record record in records)
        {
            if (!owners.ContainsKey(record.BibliographicCode))
            {
                violations.Add($"Set 2 row '{record.BibliographicCode}' has no reconciled entity.");
            }
        }
    }

    private static void AddMissingOwners(
        Iso639Set3Record[] records,
        Dictionary<string, ReconciledIso639Entity> owners,
        List<string> violations)
    {
        foreach (Iso639Set3Record record in records)
        {
            if (!owners.ContainsKey(record.Id))
            {
                violations.Add($"Set 3 row '{record.Id}' has no reconciled entity.");
            }
        }
    }

    private static void AddMissingOwners(
        Iso639Set5Record[] records,
        Dictionary<string, ReconciledIso639Entity> owners,
        List<string> violations)
    {
        foreach (Iso639Set5Record record in records)
        {
            if (!owners.ContainsKey(record.Code))
            {
                violations.Add($"Set 5 row '{record.Code}' has no reconciled entity.");
            }
        }
    }

    private static void AddAliasViolations(
        ReconciledIso639Data reconciledData,
        ReconciledCodeAliasData aliasData,
        List<string> violations)
    {
        HashSet<string> entityKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledIso639Entity entity in reconciledData.Entities)
        {
            entityKeys.Add(entity.Key);
        }

        Dictionary<string, ReconciledCodeAlias> aliasesByKey =
            new Dictionary<string, ReconciledCodeAlias>(StringComparer.OrdinalIgnoreCase);
        foreach (ReconciledCodeAlias alias in aliasData.Aliases)
        {
            AddAliasTargetViolations(alias, aliasesByKey, violations);

            if (alias.EntityKey != null && !entityKeys.Contains(alias.EntityKey))
            {
                violations.Add($"Alias '{alias.Value}' points to unknown entity '{alias.EntityKey}'.");
            }

            if (alias.Provenance.Length == 0)
            {
                violations.Add($"Alias '{alias.Value}' has no provenance.");
            }

            HashSet<string> provenanceKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (ReconciledCodeAliasProvenance provenance in alias.Provenance)
            {
                string provenanceKey = ((int)provenance.Source) + ":" + provenance.RecordIdentifier;
                if (!provenanceKeys.Add(provenanceKey))
                {
                    violations.Add($"Alias '{alias.Value}' repeats provenance '{provenance.RecordIdentifier}'.");
                }

                if (string.IsNullOrWhiteSpace(provenance.RecordIdentifier))
                {
                    violations.Add($"Alias '{alias.Value}' has empty provenance.");
                }

                if (provenance.Source == ReconciledCodeAliasSource.Iana && provenance.IanaRecord == null)
                {
                    violations.Add($"IANA alias '{alias.Value}' does not retain its registry record.");
                }

                if (alias.Kind == ReconciledCodeAliasKind.RetiredSet3 &&
                    provenance.Source == ReconciledCodeAliasSource.Iso639Set3 &&
                    provenance.RetirementRecord == null)
                {
                    violations.Add($"Retired alias '{alias.Value}' does not retain its retirement record.");
                }
            }
        }
    }

    private static void AddAliasTargetViolations(
        ReconciledCodeAlias alias,
        Dictionary<string, ReconciledCodeAlias> aliasesByKey,
        List<string> violations)
    {
        HashSet<string> contexts = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledCodeAliasProvenance provenance in alias.Provenance)
        {
            if (provenance.IanaRecord != null)
            {
                contexts.Add("iana:" + (int)provenance.IanaRecord.RecordType);
            }
        }

        if (contexts.Count == 0)
        {
            contexts.Add("iso");
        }

        foreach (string context in contexts)
        {
            string aliasKey = ((int)alias.Kind) + ":" + context + ":" + alias.Value;
            if (aliasesByKey.TryGetValue(aliasKey, out ReconciledCodeAlias? previous))
            {
                if (previous.EntityKey != alias.EntityKey || previous.PreferredValue != alias.PreferredValue)
                {
                    violations.Add(
                        $"Alias '{alias.Value}' has conflicting canonical targets for kind '{alias.Kind}' in context '{context}'.");
                }
                else
                {
                    violations.Add($"Alias key '{alias.Kind}:{context}:{alias.Value}' is duplicated.");
                }
            }
            else
            {
                aliasesByKey.Add(aliasKey, alias);
            }
        }
    }
}
