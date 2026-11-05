using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class SemanticValueChange
{
    internal SemanticValueChange(string key, string before, string after)
    {
        Key = key;
        Before = before;
        After = after;
    }

    internal string Key { get; }

    internal string Before { get; }

    internal string After { get; }
}

internal sealed class SemanticDifference
{
    internal SemanticDifference(
        SortedDictionary<string, string> added,
        SortedDictionary<string, string> removed,
        List<SemanticValueChange> changed)
    {
        Added = added;
        Removed = removed;
        Changed = changed;
    }

    internal SortedDictionary<string, string> Added { get; }

    internal SortedDictionary<string, string> Removed { get; }

    internal List<SemanticValueChange> Changed { get; }

    internal int Count => Added.Count + Removed.Count + Changed.Count;
}

internal sealed class SemanticDiffReport
{
    internal SemanticDiffReport(
        SemanticDifference codes,
        SemanticDifference retirements,
        SemanticDifference names,
        SemanticDifference mappings,
        SemanticDifference setMemberships,
        SemanticDifference hierarchy,
        SemanticDifference aliases,
        SemanticDifference preferredValues)
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

    internal SemanticDifference Codes { get; }

    internal SemanticDifference Retirements { get; }

    internal SemanticDifference Names { get; }

    internal SemanticDifference Mappings { get; }

    internal SemanticDifference SetMemberships { get; }

    internal SemanticDifference Hierarchy { get; }

    internal SemanticDifference Aliases { get; }

    internal SemanticDifference PreferredValues { get; }

    internal int Count => Codes.Count + Retirements.Count + Names.Count + Mappings.Count +
        SetMemberships.Count + Hierarchy.Count + Aliases.Count + PreferredValues.Count;
}

internal static class SemanticDiffCommand
{
    internal static SemanticDiffReport Execute(string manifestPath, string stagingPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingPath);

        string manifestFullPath = Path.GetFullPath(manifestPath);
        DataManifest pinnedManifest = DataManifestReader.Read(manifestFullPath);
        string stagingFullPath = Path.GetFullPath(stagingPath);
        ValidateCompletedStage(stagingFullPath, pinnedManifest);
        DataManifest stagedManifest = DataManifestReader.Read(
            Path.Combine(stagingFullPath, "manifest.json"));
        ValidateManifestIdentity(pinnedManifest, stagedManifest);

        string pinnedRoot = FindPinnedSourceRoot(manifestFullPath, pinnedManifest);
        string stagedRoot = Path.Combine(stagingFullPath, "files");
        SemanticDataSnapshot before = SemanticDataSnapshot.Load(pinnedManifest, pinnedRoot);
        SemanticDataSnapshot after = SemanticDataSnapshot.Load(stagedManifest, stagedRoot);
        return new SemanticDiffReport(
            Compare(before.Codes, after.Codes),
            Compare(before.Retirements, after.Retirements),
            Compare(before.Names, after.Names),
            Compare(before.Mappings, after.Mappings),
            Compare(before.SetMemberships, after.SetMemberships),
            Compare(before.Hierarchy, after.Hierarchy),
            Compare(before.Aliases, after.Aliases),
            Compare(before.PreferredValues, after.PreferredValues));
    }

    internal static void Write(Utf8JsonWriter writer, SemanticDiffReport report)
    {
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteBoolean("hasChanges", report.Count > 0);
        writer.WriteNumber("changeCount", report.Count);
        writer.WriteStartObject("summary");
        WriteSummary(writer, "codes", report.Codes);
        WriteSummary(writer, "retirements", report.Retirements);
        WriteSummary(writer, "names", report.Names);
        WriteSummary(writer, "mappings", report.Mappings);
        WriteSummary(writer, "setMemberships", report.SetMemberships);
        WriteSummary(writer, "hierarchy", report.Hierarchy);
        WriteSummary(writer, "aliases", report.Aliases);
        WriteSummary(writer, "ianaPreferredValues", report.PreferredValues);
        writer.WriteEndObject();
        writer.WriteStartObject("changes");
        WriteDifference(writer, "codes", report.Codes);
        WriteDifference(writer, "retirements", report.Retirements);
        WriteDifference(writer, "names", report.Names);
        WriteDifference(writer, "mappings", report.Mappings);
        WriteDifference(writer, "setMemberships", report.SetMemberships);
        WriteDifference(writer, "hierarchy", report.Hierarchy);
        WriteDifference(writer, "aliases", report.Aliases);
        WriteDifference(writer, "ianaPreferredValues", report.PreferredValues);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
    }

    private static SemanticDifference Compare(
        Dictionary<string, string> before,
        Dictionary<string, string> after)
    {
        SortedDictionary<string, string> added =
            new SortedDictionary<string, string>(StringComparer.Ordinal);
        SortedDictionary<string, string> removed =
            new SortedDictionary<string, string>(StringComparer.Ordinal);
        List<SemanticValueChange> changed = new List<SemanticValueChange>();
        foreach (KeyValuePair<string, string> item in before)
        {
            if (!after.TryGetValue(item.Key, out string? afterValue))
            {
                removed.Add(item.Key, item.Value);
            }
            else if (item.Value != afterValue)
            {
                changed.Add(new SemanticValueChange(item.Key, item.Value, afterValue));
            }
        }

        foreach (KeyValuePair<string, string> item in after)
        {
            if (!before.ContainsKey(item.Key))
            {
                added.Add(item.Key, item.Value);
            }
        }

        changed.Sort(
            delegate(SemanticValueChange left, SemanticValueChange right)
            {
                return string.CompareOrdinal(left.Key, right.Key);
            });
        return new SemanticDifference(added, removed, changed);
    }

    private static void ValidateCompletedStage(string stagingPath, DataManifest manifest)
    {
        if (!Directory.Exists(stagingPath))
        {
            throw new DirectoryNotFoundException(
                $"The staging directory does not exist: {stagingPath}");
        }

        string metadataPath = Path.Combine(stagingPath, "fetch-metadata.json");
        string stagedManifestPath = Path.Combine(stagingPath, "manifest.json");
        string filesPath = Path.Combine(stagingPath, "files");
        if (!File.Exists(metadataPath) || !File.Exists(stagedManifestPath) ||
            !Directory.Exists(filesPath))
        {
            throw new InvalidDataException(
                "The staging directory is incomplete; manifest, metadata, and files are required.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(metadataPath));
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out JsonElement schemaVersion) ||
                schemaVersion.GetInt32() != 1 ||
                !root.TryGetProperty("sourceCount", out JsonElement sourceCount) ||
                sourceCount.GetInt32() != manifest.Sources.Length ||
                !root.TryGetProperty("sources", out JsonElement sources) ||
                sources.ValueKind != JsonValueKind.Array ||
                sources.GetArrayLength() != manifest.Sources.Length)
            {
                throw new InvalidDataException(
                    "The staging fetch metadata does not describe a complete manifest fetch.");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "The staging fetch metadata is not valid JSON.",
                exception);
        }
    }

    private static void ValidateManifestIdentity(DataManifest pinned, DataManifest staged)
    {
        if (pinned.ManifestVersion != staged.ManifestVersion ||
            pinned.GeneratorSchemaVersion != staged.GeneratorSchemaVersion ||
            pinned.Sources.Length != staged.Sources.Length)
        {
            throw new InvalidDataException(
                "The staged manifest snapshot does not match the pinned manifest identity.");
        }

        foreach (DataManifestSource source in pinned.Sources)
        {
            DataManifestSource stagedSource = staged.GetSource(source.Id);
            if (source.Url != stagedSource.Url || source.Path != stagedSource.Path)
            {
                throw new InvalidDataException(
                    $"The staged manifest source '{source.Id}' does not match the pinned manifest.");
            }
        }
    }

    private static string FindPinnedSourceRoot(string manifestPath, DataManifest manifest)
    {
        string currentDirectory = Path.GetFullPath(Environment.CurrentDirectory);
        if (ContainsAllSources(currentDirectory, manifest))
        {
            return currentDirectory;
        }

        string? candidate = Path.GetDirectoryName(manifestPath);
        while (candidate != null)
        {
            if (ContainsAllSources(candidate, manifest))
            {
                return candidate;
            }

            candidate = Path.GetDirectoryName(candidate);
        }

        throw new DirectoryNotFoundException(
            "Could not locate the pinned source root described by the manifest.");
    }

    private static bool ContainsAllSources(string root, DataManifest manifest)
    {
        foreach (DataManifestSource source in manifest.Sources)
        {
            string path = Path.Combine(
                root,
                source.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteSummary(
        Utf8JsonWriter writer,
        string name,
        SemanticDifference difference)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("added", difference.Added.Count);
        writer.WriteNumber("removed", difference.Removed.Count);
        writer.WriteNumber("changed", difference.Changed.Count);
        writer.WriteEndObject();
    }

    private static void WriteDifference(
        Utf8JsonWriter writer,
        string name,
        SemanticDifference difference)
    {
        writer.WriteStartObject(name);
        writer.WriteStartArray("added");
        foreach (KeyValuePair<string, string> item in difference.Added)
        {
            WriteValue(writer, item.Key, item.Value);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("removed");
        foreach (KeyValuePair<string, string> item in difference.Removed)
        {
            WriteValue(writer, item.Key, item.Value);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("changed");
        foreach (SemanticValueChange change in difference.Changed)
        {
            writer.WriteStartObject();
            writer.WriteString("key", change.Key);
            writer.WriteString("before", change.Before);
            writer.WriteString("after", change.After);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, string key, string value)
    {
        writer.WriteStartObject();
        writer.WriteString("key", key);
        writer.WriteString("value", value);
        writer.WriteEndObject();
    }
}
