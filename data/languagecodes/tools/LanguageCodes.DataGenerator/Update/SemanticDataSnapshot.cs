using System;
using System.Collections.Generic;
using System.IO;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.Reconciliation;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class SemanticDataSnapshot
{
    internal SemanticDataSnapshot(
        Dictionary<string, string> codes,
        Dictionary<string, string> retirements,
        Dictionary<string, string> names,
        Dictionary<string, string> mappings,
        Dictionary<string, string> setMemberships,
        Dictionary<string, string> hierarchy,
        Dictionary<string, string> aliases,
        Dictionary<string, string> preferredValues)
    {
        Codes = codes;
        Retirements = retirements;
        Names = names;
        Mappings = mappings;
        SetMemberships = setMemberships;
        Hierarchy = hierarchy;
        Aliases = aliases;
        PreferredValues = preferredValues;
    }

    internal Dictionary<string, string> Codes { get; }

    internal Dictionary<string, string> Retirements { get; }

    internal Dictionary<string, string> Names { get; }

    internal Dictionary<string, string> Mappings { get; }

    internal Dictionary<string, string> SetMemberships { get; }

    internal Dictionary<string, string> Hierarchy { get; }

    internal Dictionary<string, string> Aliases { get; }

    internal Dictionary<string, string> PreferredValues { get; }

    internal static SemanticDataSnapshot Load(DataManifest manifest, string sourceRoot)
    {
        Iso639Set2Data set2 = Iso639Set2Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-2-code-table"));
        Iso639Set3Record[] set3 = Iso639Set3Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-code-table"));
        Iso639Set3NameRecord[] names = Iso639Set3NameIndexReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-name-index"));
        Iso639Set3RetirementRecord[] retirements = Iso639Set3RetirementReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-retirements"));
        Iso639Set3MacrolanguageMapping[] mappings = Iso639Set3MacrolanguageReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-3-macrolanguage-mappings"),
            set3,
            retirements);
        Iso639Set5Record[] set5 = Iso639Set5Reader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-5-code-table"));
        Iso639Set5HierarchyData hierarchy = Iso639Set5HierarchyReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iso-639-5-hierarchy"),
            set5);
        IanaLanguageSubtagRegistry iana = IanaLanguageSubtagRegistryReader.Read(
            ResolveSourcePath(manifest, sourceRoot, "iana-language-subtag-registry"));
        ReconciledIso639Data reconciled = Iso639Reconciler.Reconcile(set2, set3, set5);
        ReconciledCodeAliasData aliases = CodeAliasDeriver.Derive(
            reconciled,
            retirements,
            iana);

        return new SemanticDataSnapshot(
            BuildCodeIndex(reconciled.Entities),
            BuildRetirementIndex(retirements),
            BuildNameIndex(set2, set3, names, set5, iana),
            BuildMappingIndex(mappings),
            BuildMembershipIndex(reconciled.Entities),
            BuildHierarchyIndex(hierarchy),
            BuildAliasIndex(aliases.Aliases),
            BuildPreferredValueIndex(iana.Records));
    }

    private static string ResolveSourcePath(
        DataManifest manifest,
        string sourceRoot,
        string sourceId)
    {
        string path = Path.Combine(
            sourceRoot,
            manifest.GetSource(sourceId).Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Required semantic-diff source '{sourceId}' was not found.",
                path);
        }

        return path;
    }

    private static Dictionary<string, string> BuildCodeIndex(
        ReconciledIso639Entity[] entities)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (ReconciledIso639Entity entity in entities)
        {
            string details = JoinValues(
                entity.Alpha2Code,
                entity.Set2BibliographicCode,
                entity.Set2TerminologyCode,
                entity.Set3Code,
                entity.Set5Code,
                entity.Set3Record?.Scope.ToString(),
                entity.Set3Record?.LanguageType.ToString(),
                entity.Set3Record?.Comment);
            values.Add(entity.Key, details);
        }

        return values;
    }

    private static Dictionary<string, string> BuildRetirementIndex(
        Iso639Set3RetirementRecord[] records)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (Iso639Set3RetirementRecord record in records)
        {
            values.Add(
                record.Id,
                JoinValues(
                    record.ReferenceName,
                    record.Reason.ToString(),
                    record.ChangeToCode,
                    record.Remedy,
                    string.Join(",", record.ReplacementCodes),
                    record.EffectiveDate.ToString("yyyy-MM-dd")));
        }

        return values;
    }

    private static Dictionary<string, string> BuildNameIndex(
        Iso639Set2Data set2,
        Iso639Set3Record[] set3,
        Iso639Set3NameRecord[] set3Names,
        Iso639Set5Record[] set5,
        IanaLanguageSubtagRegistry iana)
    {
        Dictionary<string, List<string>> grouped =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Iso639Set2Record record in set2.Records)
        {
            AddNameGroup(grouped, "set2", record.BibliographicCode, "en", record.EnglishNames);
            AddNameGroup(grouped, "set2", record.BibliographicCode, "fr", record.FrenchNames);
        }

        foreach (Iso639Set3Record record in set3)
        {
            AddName(grouped, "set3-reference", record.Id, "", record.ReferenceName);
        }

        foreach (Iso639Set3NameRecord record in set3Names)
        {
            AddName(grouped, "set3-print", record.Id, "", record.PrintName);
            AddName(grouped, "set3-inverted", record.Id, "", record.InvertedName);
        }

        foreach (Iso639Set5Record record in set5)
        {
            AddNameGroup(grouped, "set5", record.Code, "en", record.EnglishNames);
            AddNameGroup(grouped, "set5", record.Code, "fr", record.FrenchNames);
        }

        foreach (IanaRegistryRecord record in iana.Records)
        {
            AddNameGroup(
                grouped,
                "iana",
                IanaIdentity(record),
                "description",
                record.Descriptions);
        }

        Dictionary<string, string> values = CreateIndex();
        foreach (KeyValuePair<string, List<string>> group in grouped)
        {
            group.Value.Sort(StringComparer.Ordinal);
            values.Add(group.Key, JoinValues(group.Value.ToArray()));
        }

        return values;
    }

    private static Dictionary<string, string> BuildMappingIndex(
        Iso639Set3MacrolanguageMapping[] mappings)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (Iso639Set3MacrolanguageMapping mapping in mappings)
        {
            values.Add(
                mapping.MacrolanguageId + ">" + mapping.IndividualLanguageId,
                mapping.Status.ToString());
        }

        return values;
    }

    private static Dictionary<string, string> BuildMembershipIndex(
        ReconciledIso639Entity[] entities)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (ReconciledIso639Entity entity in entities)
        {
            values.Add(
                entity.Key,
                JoinValues(
                    entity.InSet1 ? "Set1" : null,
                    entity.InSet2 ? "Set2" : null,
                    entity.InSet3 ? "Set3" : null,
                    entity.InSet5 ? "Set5" : null));
        }

        return values;
    }

    private static Dictionary<string, string> BuildHierarchyIndex(
        Iso639Set5HierarchyData hierarchy)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (Iso639Set5HierarchyEntry entry in hierarchy.Entries)
        {
            values.Add(
                "entry:" + entry.Code,
                JoinValues(
                    entry.GroupKind.ToString(),
                    string.Join(">", entry.HierarchyPath),
                    string.Join(",", entry.NoteIds)));
        }

        foreach (Iso639Set5HierarchyRelationship relationship in hierarchy.Relationships)
        {
            values.Add(
                "edge:" + relationship.ParentCode + ">" + relationship.ChildCode,
                string.Empty);
        }

        return values;
    }

    private static Dictionary<string, string> BuildAliasIndex(ReconciledCodeAlias[] aliases)
    {
        Dictionary<string, List<string>> grouped =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (ReconciledCodeAlias alias in aliases)
        {
            string[] provenance = new string[alias.Provenance.Length];
            for (int index = 0; index < alias.Provenance.Length; index++)
            {
                ReconciledCodeAliasProvenance item = alias.Provenance[index];
                provenance[index] = item.Source + ":" + item.RecordIdentifier;
            }

            Array.Sort(provenance, StringComparer.Ordinal);
            string key = JoinValues(alias.Kind.ToString(), alias.Value);
            if (!grouped.TryGetValue(key, out List<string>? semanticValues))
            {
                semanticValues = new List<string>();
                grouped.Add(key, semanticValues);
            }

            semanticValues.Add(
                JoinValues(
                    alias.EntityKey,
                    alias.PreferredValue,
                    alias.IsDeprecated ? "deprecated" : "active",
                    string.Join(",", provenance)));
        }

        Dictionary<string, string> values = CreateIndex();
        foreach (KeyValuePair<string, List<string>> group in grouped)
        {
            group.Value.Sort(StringComparer.Ordinal);
            values.Add(group.Key, string.Join(";", group.Value));
        }

        return values;
    }

    private static Dictionary<string, string> BuildPreferredValueIndex(
        IanaRegistryRecord[] records)
    {
        Dictionary<string, string> values = CreateIndex();
        foreach (IanaRegistryRecord record in records)
        {
            if (record.PreferredValue != null)
            {
                values.Add(IanaIdentity(record), record.PreferredValue);
            }
        }

        return values;
    }

    private static void AddNameGroup(
        Dictionary<string, List<string>> values,
        string source,
        string id,
        string kind,
        string[] names)
    {
        foreach (string name in names)
        {
            AddName(values, source, id, kind, name);
        }
    }

    private static void AddName(
        Dictionary<string, List<string>> values,
        string source,
        string id,
        string kind,
        string name)
    {
        string key = JoinValues(source, id, kind);
        if (!values.TryGetValue(key, out List<string>? names))
        {
            names = new List<string>();
            values.Add(key, names);
        }

        names.Add(name);
    }

    private static string IanaIdentity(IanaRegistryRecord record)
    {
        string value = record.Subtag ?? record.Tag ?? string.Empty;
        return record.RecordType + ":" + value;
    }

    private static Dictionary<string, string> CreateIndex()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static string JoinValues(params string?[] values)
    {
        string[] escaped = new string[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            string value = values[index] ?? string.Empty;
            escaped[index] = value.Replace("\\", "\\\\").Replace("|", "\\|");
        }

        return string.Join("|", escaped);
    }
}
