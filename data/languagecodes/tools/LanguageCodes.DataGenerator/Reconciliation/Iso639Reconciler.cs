using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.Reconciliation;

internal static class Iso639Reconciler
{
    internal static ReconciledIso639Data Reconcile(
        Iso639Set2Data set2Data,
        Iso639Set3Record[] set3Records,
        Iso639Set5Record[] set5Records)
    {
        ArgumentNullException.ThrowIfNull(set2Data);
        ArgumentNullException.ThrowIfNull(set3Records);
        ArgumentNullException.ThrowIfNull(set5Records);

        List<EntityBuilder> entities = new List<EntityBuilder>();
        Dictionary<string, EntityBuilder> set3ByBibliographicCode =
            new Dictionary<string, EntityBuilder>(StringComparer.Ordinal);
        Dictionary<string, EntityBuilder> set3ByTerminologyCode =
            new Dictionary<string, EntityBuilder>(StringComparer.Ordinal);
        Dictionary<string, EntityBuilder> set3ByAlpha2Code =
            new Dictionary<string, EntityBuilder>(StringComparer.Ordinal);
        HashSet<string> entityKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (Iso639Set3Record set3Record in set3Records)
        {
            EntityBuilder entity = new EntityBuilder("set3:" + set3Record.Id);
            entity.Set3Record = set3Record;
            AddEntity(entity, entities, entityKeys);
            AddOptionalMapping(
                set3ByBibliographicCode,
                set3Record.Part2BibliographicCode,
                entity,
                "Set 3 bibliographic");
            AddOptionalMapping(
                set3ByTerminologyCode,
                set3Record.Part2TerminologyCode,
                entity,
                "Set 3 terminology");
            AddOptionalMapping(set3ByAlpha2Code, set3Record.Alpha2Code, entity, "Set 3 alpha-2");
        }

        Dictionary<string, EntityBuilder> set2ByCode =
            new Dictionary<string, EntityBuilder>(StringComparer.Ordinal);
        foreach (Iso639Set2Record set2Record in set2Data.Records)
        {
            string terminologyCode = set2Record.TerminologyCode ?? set2Record.BibliographicCode;
            EntityBuilder? entity = null;
            entity = MatchExplicitMapping(
                entity,
                set3ByBibliographicCode,
                set2Record.BibliographicCode,
                "bibliographic");
            entity = MatchExplicitMapping(
                entity,
                set3ByTerminologyCode,
                terminologyCode,
                "terminology");
            entity = MatchExplicitMapping(entity, set3ByAlpha2Code, set2Record.Alpha2Code, "alpha-2");

            if (entity == null)
            {
                entity = new EntityBuilder("set2:" + terminologyCode);
                AddEntity(entity, entities, entityKeys);
            }
            else
            {
                EnsureSet2AndSet3MappingsAgree(set2Record, terminologyCode, entity.Set3Record);
            }

            if (entity.Set2Record != null)
            {
                throw new InvalidDataException(
                    $"ISO 639 Set 2 record '{set2Record.BibliographicCode}' maps to an entity that already has a Set 2 row.");
            }

            entity.Set2Record = set2Record;
            AddMapping(set2ByCode, set2Record.BibliographicCode, entity, "Set 2");
            if (terminologyCode != set2Record.BibliographicCode)
            {
                AddMapping(set2ByCode, terminologyCode, entity, "Set 2");
            }
        }

        foreach (Iso639Set5Record set5Record in set5Records)
        {
            EntityBuilder? entity = null;
            if (set2ByCode.TryGetValue(set5Record.Code, out EntityBuilder? sameCodeSet2Entity) &&
                HaveCommonEnglishName(sameCodeSet2Entity.Set2Record, set5Record))
            {
                entity = sameCodeSet2Entity;
            }

            if (entity == null)
            {
                entity = new EntityBuilder("set5:" + set5Record.Code);
                AddEntity(entity, entities, entityKeys);
            }

            if (entity.Set5Record != null)
            {
                throw new InvalidDataException(
                    $"ISO 639 Set 5 record '{set5Record.Code}' maps to an entity that already has a Set 5 row.");
            }

            entity.Set5Record = set5Record;
        }

        ReconciledIso639Entity[] results = new ReconciledIso639Entity[entities.Count];
        for (int index = 0; index < entities.Count; index++)
        {
            results[index] = entities[index].Build();
        }

        return new ReconciledIso639Data(results, set2Data.ReservedRanges);
    }

    private static void AddEntity(
        EntityBuilder entity,
        List<EntityBuilder> entities,
        HashSet<string> entityKeys)
    {
        if (!entityKeys.Add(entity.Key))
        {
            throw new InvalidDataException($"Reconciled entity key '{entity.Key}' is duplicated.");
        }

        entities.Add(entity);
    }

    private static void AddOptionalMapping(
        Dictionary<string, EntityBuilder> mappings,
        string? code,
        EntityBuilder entity,
        string mappingName)
    {
        if (code != null)
        {
            AddMapping(mappings, code, entity, mappingName);
        }
    }

    private static void AddMapping(
        Dictionary<string, EntityBuilder> mappings,
        string code,
        EntityBuilder entity,
        string mappingName)
    {
        if (!mappings.TryAdd(code, entity))
        {
            throw new InvalidDataException($"{mappingName} code '{code}' maps to more than one source row.");
        }
    }

    private static EntityBuilder? MatchExplicitMapping(
        EntityBuilder? current,
        Dictionary<string, EntityBuilder> mappings,
        string? code,
        string codeForm)
    {
        if (code == null || !mappings.TryGetValue(code, out EntityBuilder? candidate))
        {
            return current;
        }

        if (current != null && current != candidate)
        {
            throw new InvalidDataException(
                $"ISO 639 Set 2 {codeForm} code '{code}' conflicts with its other Set 3 mappings.");
        }

        return candidate;
    }

    private static void EnsureSet2AndSet3MappingsAgree(
        Iso639Set2Record set2Record,
        string terminologyCode,
        Iso639Set3Record? set3Record)
    {
        if (set3Record == null ||
            set3Record.Part2BibliographicCode != set2Record.BibliographicCode ||
            set3Record.Part2TerminologyCode != terminologyCode ||
            set3Record.Alpha2Code != set2Record.Alpha2Code)
        {
            throw new InvalidDataException(
                $"ISO 639 Set 2 record '{set2Record.BibliographicCode}' has an incomplete or conflicting Set 3 mapping.");
        }
    }

    private static bool HaveCommonEnglishName(
        Iso639Set2Record? set2Record,
        Iso639Set5Record set5Record)
    {
        if (set2Record == null)
        {
            return false;
        }

        HashSet<string> normalizedSet2Names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in set2Record.EnglishNames)
        {
            normalizedSet2Names.Add(NormalizeName(name));
        }

        foreach (string name in set5Record.EnglishNames)
        {
            if (normalizedSet2Names.Contains(NormalizeName(name)))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeName(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormKC);
        StringBuilder builder = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            UnicodeCategory category = char.GetUnicodeCategory(character);
            if (category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.DecimalDigitNumber)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private sealed class EntityBuilder
    {
        internal EntityBuilder(string key)
        {
            Key = key;
        }

        internal string Key { get; }

        internal Iso639Set2Record? Set2Record { get; set; }

        internal Iso639Set3Record? Set3Record { get; set; }

        internal Iso639Set5Record? Set5Record { get; set; }

        internal ReconciledIso639Entity Build()
        {
            string? alpha2Code = Set3Record?.Alpha2Code ?? Set2Record?.Alpha2Code;
            string? bibliographicCode = Set2Record?.BibliographicCode;
            string? terminologyCode = null;
            if (Set2Record != null)
            {
                terminologyCode = Set2Record.TerminologyCode ?? Set2Record.BibliographicCode;
            }

            return new ReconciledIso639Entity(
                Key,
                alpha2Code,
                bibliographicCode,
                terminologyCode,
                Set3Record?.Id,
                Set5Record?.Code,
                Set2Record,
                Set3Record,
                Set5Record);
        }
    }
}
