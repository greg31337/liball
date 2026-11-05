using System;
using System.Collections.Generic;

namespace gltf;

internal class TransformExtensionSnapshot
{
    internal List<string> Used = new List<string>();
    internal List<string> Required = new List<string>();
}

internal class TransformReportAggregator
{
    internal TransformExtensionSnapshot CaptureExtensionSnapshot(
        GltfDocument document)
    {
        TransformExtensionSnapshot result = new TransformExtensionSnapshot();
        CopyStrings(document.ExtensionsUsed, result.Used);
        CopyStrings(document.ExtensionsRequired, result.Required);
        return result;
    }

    internal void RecordExtensionChanges(
        TransformExtensionSnapshot before,
        GltfDocument after,
        GltfTransformStepResult result)
    {
        RecordDeclarationChanges(
            before.Used,
            after.ExtensionsUsed,
            false,
            result.ExtensionChanges);
        RecordDeclarationChanges(
            before.Required,
            after.ExtensionsRequired,
            true,
            result.ExtensionChanges);
        NormalizeExtensionChanges(result.ExtensionChanges);
    }

    internal void RecordDeclarationChanges(
        List<string> before,
        List<string> after,
        bool required,
        List<GltfTransformExtensionChange> target)
    {
        int index = 0;
        while (index < before.Count)
        {
            string name = before[index];
            if (!after.Contains(name))
            {
                AddExtensionChange(target, name, required, false);
            }

            index++;
        }

        index = 0;
        while (index < after.Count)
        {
            string name = after[index];
            if (!before.Contains(name))
            {
                AddExtensionChange(target, name, required, true);
            }

            index++;
        }
    }

    internal void AddStepResult(
        GltfTransformReport report,
        GltfTransformStepResult result,
        bool includeChanges)
    {
        NormalizeStrings(result.Codecs);
        NormalizeExtensionChanges(result.ExtensionChanges);
        report.Steps.Add(result);
        if (includeChanges)
        {
            AddStrings(report.ChangedObjects, result.ChangedObjects);
            AddStrings(report.ChangedResources, result.ChangedResources);
            AddExtensionChanges(report.ExtensionChanges, result.ExtensionChanges);
        }

        AddStrings(report.Codecs, result.Codecs);
        if (result.Status == GltfOperationStatus.Skipped
            && result.Reason != null
            && result.Reason.Length > 0)
        {
            AddString(report.SkippedReasons, result.Reason);
        }

        AddDiagnostics(report.Diagnostics, result.Diagnostics);
        NormalizeStrings(report.ChangedObjects);
        NormalizeStrings(report.ChangedResources);
        NormalizeStrings(report.Codecs);
        NormalizeStrings(report.SkippedReasons);
        NormalizeExtensionChanges(report.ExtensionChanges);
    }

    internal void AddStrings(List<string> target, List<string> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            AddString(target, source[index]);
            index++;
        }
    }

    internal void AddString(List<string> target, string value)
    {
        if (value != null && !target.Contains(value))
        {
            target.Add(value);
        }
    }

    internal void NormalizeStrings(List<string> values)
    {
        List<string> normalized = new List<string>();
        int index = 0;
        while (index < values.Count)
        {
            AddString(normalized, values[index]);
            index++;
        }

        normalized.Sort(StringComparer.Ordinal);
        values.Clear();
        CopyStrings(normalized, values);
    }

    internal void AddExtensionChanges(
        List<GltfTransformExtensionChange> target,
        List<GltfTransformExtensionChange> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            GltfTransformExtensionChange change = source[index];
            AddExtensionChange(
                target,
                change.Name,
                change.Required,
                change.Added);
            index++;
        }
    }

    internal void AddExtensionChange(
        List<GltfTransformExtensionChange> target,
        string name,
        bool required,
        bool added)
    {
        int index = 0;
        while (index < target.Count)
        {
            GltfTransformExtensionChange existing = target[index];
            if (existing.Name == name
                && existing.Required == required
                && existing.Added == added)
            {
                return;
            }

            index++;
        }

        GltfTransformExtensionChange change = new GltfTransformExtensionChange();
        change.Name = name;
        change.Required = required;
        change.Added = added;
        target.Add(change);
    }

    internal void NormalizeExtensionChanges(
        List<GltfTransformExtensionChange> changes)
    {
        List<GltfTransformExtensionChange> normalized =
            new List<GltfTransformExtensionChange>();
        AddExtensionChanges(normalized, changes);
        changes.Clear();
        int sourceIndex = 0;
        while (sourceIndex < normalized.Count)
        {
            InsertExtensionChange(changes, normalized[sourceIndex]);
            sourceIndex++;
        }
    }

    internal void InsertExtensionChange(
        List<GltfTransformExtensionChange> target,
        GltfTransformExtensionChange change)
    {
        int index = target.Count;
        target.Add(change);
        while (index > 0
            && CompareExtensionChanges(target[index - 1], change) > 0)
        {
            target[index] = target[index - 1];
            index--;
        }

        target[index] = change;
    }

    internal int CompareExtensionChanges(
        GltfTransformExtensionChange first,
        GltfTransformExtensionChange second)
    {
        int nameOrder = string.CompareOrdinal(first.Name, second.Name);
        if (nameOrder != 0)
        {
            return nameOrder;
        }

        if (first.Required != second.Required)
        {
            return first.Required ? 1 : -1;
        }

        if (first.Added != second.Added)
        {
            return first.Added ? 1 : -1;
        }

        return 0;
    }

    internal void AddDiagnostics(
        List<GltfValidationIssue> target,
        List<GltfValidationIssue> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal void CopyStrings(List<string> source, List<string> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
