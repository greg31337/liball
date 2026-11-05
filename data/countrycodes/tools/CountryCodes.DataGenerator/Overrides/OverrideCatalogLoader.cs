using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CountryCodes.DataGenerator.Reconciliation;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Overrides;

internal static class OverrideCatalogLoader
{
    private const string ExpiryRule = "remove-or-rereview-when-source-revision-changes";

    internal static OverrideCatalog Load(string path, SourceManifestInfo manifest)
    {
        List<string> issues = new List<string>();
        JsonDocument document;
        try
        {
            using FileStream stream = File.OpenRead(path);
            document = JsonDocument.Parse(stream);
        }
        catch (Exception exception) when (exception is IOException || exception is JsonException)
        {
            throw new OverrideException(new[] { $"Could not read overrides: {exception.Message}" });
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new OverrideException(new[] { "Override document root must be an object." });
            }

            RequireOnlyProperties(
                root,
                new[] { "schemaVersion", "policyVersion", "overrides" },
                "root",
                issues);
            int schemaVersion = ReadInteger(root, "schemaVersion", "root", issues);
            int policyVersion = ReadInteger(root, "policyVersion", "root", issues);
            if (schemaVersion != 1)
            {
                issues.Add($"Unsupported override schemaVersion '{schemaVersion}'.");
            }

            if (policyVersion != 1)
            {
                issues.Add($"Unsupported override policyVersion '{policyVersion}'.");
            }

            List<OverrideEntry> entries = new List<OverrideEntry>();
            if (!root.TryGetProperty("overrides", out JsonElement array) ||
                array.ValueKind != JsonValueKind.Array)
            {
                issues.Add("root.overrides must be an array.");
            }
            else
            {
                int index = 0;
                foreach (JsonElement element in array.EnumerateArray())
                {
                    OverrideEntry? entry = ReadEntry(element, index, manifest, issues);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }

                    index += 1;
                }
            }

            ValidateUniqueIds(entries, issues);
            if (issues.Count != 0)
            {
                throw new OverrideException(issues);
            }

            return new OverrideCatalog(policyVersion, entries);
        }
    }

    private static OverrideEntry? ReadEntry(
        JsonElement element,
        int index,
        SourceManifestInfo manifest,
        List<string> issues)
    {
        string context = $"overrides[{index}]";
        if (element.ValueKind != JsonValueKind.Object)
        {
            issues.Add($"{context} must be an object.");
            return null;
        }

        RequireOnlyProperties(
            element,
            new[]
            {
                "id", "kind", "targetKind", "targetKey", "field", "values",
                "provenance", "reason", "reviewer", "effectiveDate", "expiryCondition"
            },
            context,
            issues);
        string id = ReadString(element, "id", context, issues);
        string kind = ReadString(element, "kind", context, issues);
        string targetKind = ReadString(element, "targetKind", context, issues);
        string targetKey = ReadString(element, "targetKey", context, issues);
        string field = ReadString(element, "field", context, issues);
        IReadOnlyList<string> values = ReadValues(element, context, issues);
        OverrideProvenance provenance = ReadProvenance(element, context, issues);
        string reason = ReadString(element, "reason", context, issues);
        string reviewer = ReadString(element, "reviewer", context, issues);
        string effectiveDate = ReadString(element, "effectiveDate", context, issues);
        OverrideExpiryCondition expiry = ReadExpiry(element, context, issues);

        if (!string.Equals(kind, "source-delta", StringComparison.Ordinal) &&
            !string.Equals(kind, "alias", StringComparison.Ordinal) &&
            !string.Equals(kind, "supplement", StringComparison.Ordinal))
        {
            issues.Add($"{context}.kind '{kind}' is not supported.");
        }

        if (!ReconciliationSyntax.IsCanonicalDate(effectiveDate) || effectiveDate.Length != 10)
        {
            issues.Add($"{context}.effectiveDate must be a canonical YYYY-MM-DD date.");
        }
        else if (string.CompareOrdinal(effectiveDate, manifest.EffectiveDate) > 0)
        {
            issues.Add($"{context}.effectiveDate cannot be later than the dataset effective date.");
        }

        ValidateSource(provenance.SourceId, context + ".provenance.sourceId", manifest, issues);
        SourceSnapshotInfo? expirySource = FindSource(
            expiry.SourceId,
            context + ".expiryCondition.sourceId",
            manifest,
            issues);
        if (expirySource != null &&
            !string.Equals(expirySource.Revision, expiry.Revision, StringComparison.Ordinal))
        {
            issues.Add(
                $"{context} expired because source '{expiry.SourceId}' revision changed from " +
                $"'{expiry.Revision}' to '{expirySource.Revision}'.");
        }

        if (!string.Equals(expiry.Condition, ExpiryRule, StringComparison.Ordinal))
        {
            issues.Add($"{context}.expiryCondition.condition must be '{ExpiryRule}'.");
        }

        return new OverrideEntry(
            id,
            kind,
            targetKind,
            targetKey,
            field,
            values,
            provenance,
            reason,
            reviewer,
            effectiveDate,
            expiry);
    }

    private static OverrideProvenance ReadProvenance(
        JsonElement entry,
        string context,
        List<string> issues)
    {
        string propertyContext = context + ".provenance";
        if (!entry.TryGetProperty("provenance", out JsonElement element) ||
            element.ValueKind != JsonValueKind.Object)
        {
            issues.Add($"{propertyContext} must be an object.");
            return new OverrideProvenance(string.Empty, string.Empty, string.Empty);
        }

        RequireOnlyProperties(
            element,
            new[] { "sourceId", "upstreamKey", "evidence" },
            propertyContext,
            issues);
        return new OverrideProvenance(
            ReadString(element, "sourceId", propertyContext, issues),
            ReadString(element, "upstreamKey", propertyContext, issues),
            ReadString(element, "evidence", propertyContext, issues));
    }

    private static OverrideExpiryCondition ReadExpiry(
        JsonElement entry,
        string context,
        List<string> issues)
    {
        string propertyContext = context + ".expiryCondition";
        if (!entry.TryGetProperty("expiryCondition", out JsonElement element) ||
            element.ValueKind != JsonValueKind.Object)
        {
            issues.Add($"{propertyContext} must be an object.");
            return new OverrideExpiryCondition(string.Empty, string.Empty, string.Empty);
        }

        RequireOnlyProperties(
            element,
            new[] { "sourceId", "revision", "condition" },
            propertyContext,
            issues);
        return new OverrideExpiryCondition(
            ReadString(element, "sourceId", propertyContext, issues),
            ReadString(element, "revision", propertyContext, issues),
            ReadString(element, "condition", propertyContext, issues));
    }

    private static IReadOnlyList<string> ReadValues(
        JsonElement entry,
        string context,
        List<string> issues)
    {
        if (!entry.TryGetProperty("values", out JsonElement values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            issues.Add($"{context}.values must be a non-empty string array.");
            return Array.Empty<string>();
        }

        List<string> result = new List<string>();
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()))
            {
                issues.Add($"{context}.values must contain only non-empty strings.");
                continue;
            }

            string text = value.GetString() ?? string.Empty;
            if (!unique.Add(text))
            {
                issues.Add($"{context}.values contains duplicate '{text}'.");
            }

            result.Add(text);
        }

        if (result.Count == 0)
        {
            issues.Add($"{context}.values must not be empty.");
        }

        return result;
    }

    private static int ReadInteger(
        JsonElement parent,
        string name,
        string context,
        List<string> issues)
    {
        if (!parent.TryGetProperty(name, out JsonElement element) ||
            element.ValueKind != JsonValueKind.Number ||
            !element.TryGetInt32(out int value))
        {
            issues.Add($"{context}.{name} must be an integer.");
            return 0;
        }

        return value;
    }

    private static string ReadString(
        JsonElement parent,
        string name,
        string context,
        List<string> issues)
    {
        if (!parent.TryGetProperty(name, out JsonElement element) ||
            element.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(element.GetString()))
        {
            issues.Add($"{context}.{name} must be a non-empty string.");
            return string.Empty;
        }

        return element.GetString() ?? string.Empty;
    }

    private static void RequireOnlyProperties(
        JsonElement element,
        IReadOnlyList<string> allowed,
        string context,
        List<string> issues)
    {
        HashSet<string> names = new HashSet<string>(allowed, StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!names.Contains(property.Name))
            {
                issues.Add($"{context} contains unknown property '{property.Name}'.");
            }
        }
    }

    private static void ValidateUniqueIds(
        IReadOnlyList<OverrideEntry> entries,
        List<string> issues)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (OverrideEntry entry in entries)
        {
            if (!ids.Add(entry.Id))
            {
                issues.Add($"Duplicate override id '{entry.Id}'.");
            }
        }
    }

    private static void ValidateSource(
        string sourceId,
        string context,
        SourceManifestInfo manifest,
        List<string> issues)
    {
        FindSource(sourceId, context, manifest, issues);
    }

    private static SourceSnapshotInfo? FindSource(
        string sourceId,
        string context,
        SourceManifestInfo manifest,
        List<string> issues)
    {
        if (sourceId.Length == 0)
        {
            return null;
        }

        try
        {
            return manifest.GetSource(sourceId);
        }
        catch (StagingException)
        {
            issues.Add($"{context} references undeclared source '{sourceId}'.");
            return null;
        }
    }
}
