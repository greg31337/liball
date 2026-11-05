using System;
using System.Collections.Generic;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.Reconciliation;

internal static class CodeAliasDeriver
{
    internal static ReconciledCodeAliasData Derive(
        ReconciledIso639Data isoData,
        Iso639Set3RetirementRecord[] retirementRecords,
        IanaLanguageSubtagRegistry ianaRegistry)
    {
        ArgumentNullException.ThrowIfNull(isoData);
        ArgumentNullException.ThrowIfNull(retirementRecords);
        ArgumentNullException.ThrowIfNull(ianaRegistry);

        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode = CreateCodeIndex(isoData.Entities);
        HashSet<string> extlangSubtags = CreateExtlangIndex(ianaRegistry.Records);
        Dictionary<string, AliasBuilder> aliases =
            new Dictionary<string, AliasBuilder>(StringComparer.Ordinal);
        List<AliasBuilder> orderedAliases = new List<AliasBuilder>();

        foreach (ReconciledIso639Entity entity in isoData.Entities)
        {
            AddIsoAliases(entity, aliases, orderedAliases);
        }

        foreach (Iso639Set3RetirementRecord retirement in retirementRecords)
        {
            AddRetiredAlias(retirement, entitiesByCode, aliases, orderedAliases);
        }

        foreach (IanaRegistryRecord record in ianaRegistry.Records)
        {
            AddIanaAliases(record, entitiesByCode, extlangSubtags, aliases, orderedAliases);
        }

        ReconciledCodeAlias[] results = new ReconciledCodeAlias[orderedAliases.Count];
        for (int index = 0; index < orderedAliases.Count; index++)
        {
            results[index] = orderedAliases[index].Build();
        }

        return new ReconciledCodeAliasData(results);
    }

    private static void AddIsoAliases(
        ReconciledIso639Entity entity,
        Dictionary<string, AliasBuilder> aliases,
        List<AliasBuilder> orderedAliases)
    {
        string canonicalCode = GetCanonicalIsoCode(entity);
        if (entity.Alpha2Code != null)
        {
            AddAlias(
                aliases,
                orderedAliases,
                entity.Alpha2Code,
                ReconciledCodeAliasKind.Alpha2,
                entity.Key,
                canonicalCode,
                false,
                new ReconciledCodeAliasProvenance(
                    ReconciledCodeAliasSource.Iso639Set1,
                    entity.Alpha2Code,
                    null,
                    null));
        }

        if (entity.Set2TerminologyCode != null && entity.Set2Record != null)
        {
            AddAlias(
                aliases,
                orderedAliases,
                entity.Set2TerminologyCode,
                ReconciledCodeAliasKind.Set2Terminology,
                entity.Key,
                canonicalCode,
                false,
                CreateSet2Provenance(entity.Set2Record));
        }

        if (entity.Set2BibliographicCode != null && entity.Set2Record != null)
        {
            AddAlias(
                aliases,
                orderedAliases,
                entity.Set2BibliographicCode,
                ReconciledCodeAliasKind.Set2Bibliographic,
                entity.Key,
                canonicalCode,
                false,
                CreateSet2Provenance(entity.Set2Record));
        }

        if (entity.Alpha2Code == null)
        {
            return;
        }

        AddOverlongAlias(
            entity,
            entity.Set3Code,
            ReconciledCodeAliasSource.Iso639Set3,
            entity.Set3Code,
            aliases,
            orderedAliases);
        AddOverlongAlias(
            entity,
            entity.Set2TerminologyCode,
            ReconciledCodeAliasSource.Iso639Set2,
            entity.Set2BibliographicCode,
            aliases,
            orderedAliases);
        AddOverlongAlias(
            entity,
            entity.Set2BibliographicCode,
            ReconciledCodeAliasSource.Iso639Set2,
            entity.Set2BibliographicCode,
            aliases,
            orderedAliases);
    }

    private static void AddOverlongAlias(
        ReconciledIso639Entity entity,
        string? value,
        ReconciledCodeAliasSource source,
        string? recordIdentifier,
        Dictionary<string, AliasBuilder> aliases,
        List<AliasBuilder> orderedAliases)
    {
        if (value == null || recordIdentifier == null)
        {
            return;
        }

        ReconciledCodeAliasProvenance provenance = new ReconciledCodeAliasProvenance(
            source,
            recordIdentifier,
            null,
            null);
        AddAlias(
            aliases,
            orderedAliases,
            value,
            ReconciledCodeAliasKind.Overlong,
            entity.Key,
            entity.Alpha2Code,
            false,
            provenance);
    }

    private static ReconciledCodeAliasProvenance CreateSet2Provenance(Iso639Set2Record record)
    {
        return new ReconciledCodeAliasProvenance(
            ReconciledCodeAliasSource.Iso639Set2,
            record.BibliographicCode,
            null,
            null);
    }

    private static void AddRetiredAlias(
        Iso639Set3RetirementRecord retirement,
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode,
        Dictionary<string, AliasBuilder> aliases,
        List<AliasBuilder> orderedAliases)
    {
        string? preferredValue = null;
        string? entityKey = null;
        if (retirement.ChangeToCode != null)
        {
            preferredValue = retirement.ChangeToCode;
            entityKey = ResolveCode(entitiesByCode, preferredValue)?.Key;
        }

        AddAlias(
            aliases,
            orderedAliases,
            retirement.Id,
            ReconciledCodeAliasKind.RetiredSet3,
            entityKey,
            preferredValue,
            true,
            new ReconciledCodeAliasProvenance(
                ReconciledCodeAliasSource.Iso639Set3,
                retirement.Id,
                retirement,
                null));
    }

    private static void AddIanaAliases(
        IanaRegistryRecord record,
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode,
        HashSet<string> extlangSubtags,
        Dictionary<string, AliasBuilder> aliases,
        List<AliasBuilder> orderedAliases)
    {
        if (record.RecordType != IanaRegistryRecordType.Language &&
            record.RecordType != IanaRegistryRecordType.Extlang &&
            record.RecordType != IanaRegistryRecordType.Grandfathered &&
            record.RecordType != IanaRegistryRecordType.Redundant)
        {
            return;
        }

        string value = record.Subtag ?? record.Tag ?? "";
        string? targetValue = record.PreferredValue ?? value;
        ReconciledIso639Entity? target = ResolveLanguageValue(
            entitiesByCode,
            extlangSubtags,
            targetValue);
        ReconciledCodeAliasProvenance provenance = new ReconciledCodeAliasProvenance(
            ReconciledCodeAliasSource.Iana,
            FormatIanaIdentifier(record, value),
            null,
            record);

        if (record.DeprecatedDate != null)
        {
            AddAlias(
                aliases,
                orderedAliases,
                value,
                ReconciledCodeAliasKind.IanaDeprecated,
                target?.Key,
                record.PreferredValue,
                true,
                provenance);
        }

        if (record.RecordType == IanaRegistryRecordType.Grandfathered)
        {
            AddAlias(
                aliases,
                orderedAliases,
                value,
                ReconciledCodeAliasKind.Grandfathered,
                target?.Key,
                record.PreferredValue,
                record.DeprecatedDate != null,
                provenance);
        }

        if (record.PreferredValue != null)
        {
            AddAlias(
                aliases,
                orderedAliases,
                record.PreferredValue,
                ReconciledCodeAliasKind.IanaPreferred,
                target?.Key,
                record.PreferredValue,
                false,
                provenance);
        }
    }

    private static string FormatIanaIdentifier(IanaRegistryRecord record, string value)
    {
        return record.RecordType.ToString().ToLowerInvariant() + ":" + value;
    }

    private static ReconciledIso639Entity? ResolveLanguageValue(
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode,
        HashSet<string> extlangSubtags,
        string value)
    {
        if (value.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        string[] parts = value.Split('-');
        if (parts.Length == 1)
        {
            return ResolveCode(entitiesByCode, value);
        }

        if (parts.Length > 1 && extlangSubtags.Contains(parts[1]))
        {
            ReconciledIso639Entity? extlangEntity = ResolveCode(entitiesByCode, parts[1]);
            if (extlangEntity != null)
            {
                return extlangEntity;
            }
        }

        return ResolveCode(entitiesByCode, parts[0]);
    }

    private static Dictionary<string, List<ReconciledIso639Entity>> CreateCodeIndex(
        ReconciledIso639Entity[] entities)
    {
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode =
            new Dictionary<string, List<ReconciledIso639Entity>>(StringComparer.Ordinal);
        foreach (ReconciledIso639Entity entity in entities)
        {
            AddCodeOwner(entitiesByCode, entity.Alpha2Code, entity);
            AddCodeOwner(entitiesByCode, entity.Set2TerminologyCode, entity);
            AddCodeOwner(entitiesByCode, entity.Set2BibliographicCode, entity);
            AddCodeOwner(entitiesByCode, entity.Set3Code, entity);
        }

        return entitiesByCode;
    }

    private static void AddCodeOwner(
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode,
        string? code,
        ReconciledIso639Entity entity)
    {
        if (code == null)
        {
            return;
        }

        if (!entitiesByCode.TryGetValue(code, out List<ReconciledIso639Entity>? owners))
        {
            owners = new List<ReconciledIso639Entity>();
            entitiesByCode.Add(code, owners);
        }

        if (!owners.Contains(entity))
        {
            owners.Add(entity);
        }
    }

    private static ReconciledIso639Entity? ResolveCode(
        Dictionary<string, List<ReconciledIso639Entity>> entitiesByCode,
        string code)
    {
        if (!entitiesByCode.TryGetValue(code, out List<ReconciledIso639Entity>? owners) || owners.Count != 1)
        {
            return null;
        }

        return owners[0];
    }

    private static HashSet<string> CreateExtlangIndex(IanaRegistryRecord[] records)
    {
        HashSet<string> extlangs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IanaRegistryRecord record in records)
        {
            if (record.RecordType == IanaRegistryRecordType.Extlang && record.Subtag != null)
            {
                extlangs.Add(record.Subtag);
            }
        }

        return extlangs;
    }

    private static string GetCanonicalIsoCode(ReconciledIso639Entity entity)
    {
        return entity.Set3Code ?? entity.Set2TerminologyCode ?? entity.Set2BibliographicCode ??
            entity.Set5Code ?? entity.Alpha2Code ?? throw new InvalidOperationException(
                $"Reconciled entity '{entity.Key}' has no code.");
    }

    private static void AddAlias(
        Dictionary<string, AliasBuilder> aliases,
        List<AliasBuilder> orderedAliases,
        string value,
        ReconciledCodeAliasKind kind,
        string? entityKey,
        string? preferredValue,
        bool isDeprecated,
        ReconciledCodeAliasProvenance provenance)
    {
        string key = ((int)kind) + "\0" + value + "\0" + entityKey + "\0" + preferredValue + "\0" +
            (isDeprecated ? "1" : "0");
        if (!aliases.TryGetValue(key, out AliasBuilder? builder))
        {
            builder = new AliasBuilder(value, kind, entityKey, preferredValue, isDeprecated);
            aliases.Add(key, builder);
            orderedAliases.Add(builder);
        }

        builder.AddProvenance(provenance);
    }

    private sealed class AliasBuilder
    {
        private readonly List<ReconciledCodeAliasProvenance> provenance =
            new List<ReconciledCodeAliasProvenance>();
        private readonly HashSet<string> provenanceKeys = new HashSet<string>(StringComparer.Ordinal);

        internal AliasBuilder(
            string value,
            ReconciledCodeAliasKind kind,
            string? entityKey,
            string? preferredValue,
            bool isDeprecated)
        {
            Value = value;
            Kind = kind;
            EntityKey = entityKey;
            PreferredValue = preferredValue;
            IsDeprecated = isDeprecated;
        }

        internal string Value { get; }

        internal ReconciledCodeAliasKind Kind { get; }

        internal string? EntityKey { get; }

        internal string? PreferredValue { get; }

        internal bool IsDeprecated { get; }

        internal void AddProvenance(ReconciledCodeAliasProvenance item)
        {
            string key = ((int)item.Source) + ":" + item.RecordIdentifier;
            if (provenanceKeys.Add(key))
            {
                provenance.Add(item);
            }
        }

        internal ReconciledCodeAlias Build()
        {
            return new ReconciledCodeAlias(
                Value,
                Kind,
                EntityKey,
                PreferredValue,
                IsDeprecated,
                provenance.ToArray());
        }
    }
}
