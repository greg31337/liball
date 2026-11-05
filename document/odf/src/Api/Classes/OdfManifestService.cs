using System;

namespace odf;

/// <summary>
/// Provides parsing, serialization, querying, editing, and synchronization for package manifests.
/// </summary>
public class OdfManifestService : IOdfManifestService
{
    /// <summary>
    /// Initializes a manifest service.
    /// </summary>
    public OdfManifestService()
    {
    }

    /// <summary>
    /// Parses manifest XML bytes.
    /// </summary>
    /// <param name="data">The manifest XML bytes.</param>
    /// <returns>The parsed manifest.</returns>
    public OdfManifest ReadManifestBytes(byte[] data)
    {
        return ManifestParser.ParseBytes(data);
    }

    /// <summary>
    /// Serializes a manifest to XML bytes.
    /// </summary>
    /// <param name="manifest">The manifest to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The serialized manifest XML.</returns>
    public byte[] WriteManifestBytes(
        OdfManifest manifest,
        OdfSaveOptions options)
    {
        return ManifestWriter.WriteBytes(manifest, options);
    }

    /// <summary>
    /// Finds a manifest entry by exact full path.
    /// </summary>
    /// <param name="manifest">The manifest to search.</param>
    /// <param name="path">The exact manifest full path.</param>
    /// <returns>The matching entry, or <c>null</c>.</returns>
    public OdfManifestEntry? FindManifestEntry(
        OdfManifest manifest,
        string path)
    {
        int index = FindManifestEntryIndex(manifest, path);
        return index < 0 ? null : manifest.Entries[index];
    }

    /// <summary>
    /// Adds a manifest entry when its full path is not present.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="entry">The entry to append.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult AddManifestEntry(
        OdfManifest manifest,
        OdfManifestEntry entry)
    {
        OdfEditResult validation = ValidateManifestEntry(entry);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }
        if (FindManifestEntryIndex(manifest, entry.FullPath) >= 0)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Manifest entry '{entry.FullPath}' already exists.",
                entry.FullPath);
        }
        OdfManifestEntry[] expanded = new OdfManifestEntry[
            manifest.Entries.Length + 1];
        Array.Copy(manifest.Entries, expanded, manifest.Entries.Length);
        expanded[expanded.Length - 1] = entry;
        manifest.Entries = expanded;
        RecalculateManifestValidity(manifest);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Added manifest entry '{entry.FullPath}'.",
            entry.FullPath);
    }

    /// <summary>
    /// Replaces a manifest entry identified by its current full path.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="path">The current full path.</param>
    /// <param name="entry">The replacement entry.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult UpdateManifestEntry(
        OdfManifest manifest,
        string path,
        OdfManifestEntry entry)
    {
        int sourceIndex = FindManifestEntryIndex(manifest, path);
        if (sourceIndex < 0)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Manifest entry '{path}' does not exist.",
                path);
        }
        OdfEditResult validation = ValidateManifestEntry(entry);
        if (validation.Status != OdfEditStatus.Success)
        {
            return validation;
        }
        int destinationIndex = FindManifestEntryIndex(
            manifest,
            entry.FullPath);
        if (destinationIndex >= 0 && destinationIndex != sourceIndex)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Manifest entry '{entry.FullPath}' already exists.",
                entry.FullPath);
        }
        if (ManifestEntriesMatch(manifest.Entries[sourceIndex], entry))
        {
            return CreateEditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                $"Manifest entry '{path}' already has the requested values.",
                path);
        }
        manifest.Entries[sourceIndex] = entry;
        RecalculateManifestValidity(manifest);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Updated manifest entry '{path}'.",
            entry.FullPath);
    }

    /// <summary>
    /// Removes a manifest entry by exact full path.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="path">The full path to remove.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemoveManifestEntry(
        OdfManifest manifest,
        string path)
    {
        int sourceIndex = FindManifestEntryIndex(manifest, path);
        if (sourceIndex < 0)
        {
            return CreateEditResult(
                OdfEditStatus.Conflict,
                false,
                0,
                $"Manifest entry '{path}' does not exist.",
                path);
        }
        OdfManifestEntry[] reduced = new OdfManifestEntry[
            manifest.Entries.Length - 1];
        int source = 0;
        int destination = 0;
        while (source < manifest.Entries.Length)
        {
            if (source != sourceIndex)
            {
                reduced[destination] = manifest.Entries[source];
                destination = destination + 1;
            }
            source = source + 1;
        }
        manifest.Entries = reduced;
        RecalculateManifestValidity(manifest);
        return CreateEditResult(
            OdfEditStatus.Success,
            true,
            1,
            $"Removed manifest entry '{path}'.",
            path);
    }

    /// <summary>
    /// Reconciles a loaded package manifest with its current ZIP entries.
    /// </summary>
    /// <param name="package">The package whose manifest is synchronized.</param>
    /// <returns>The synchronization result.</returns>
    public OdfEditResult SynchronizePackageManifest(OdfPackage package)
    {
        ZipEntryIndex index = PackageApiModel.RequireEntryIndex(package);
        OdfEditResult result = index.SynchronizeManifest();
        PackageApiModel.RefreshPackage(package);
        return result;
    }

    internal int FindManifestEntryIndex(OdfManifest manifest, string path)
    {
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            if (manifest.Entries[index].FullPath == path)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal OdfEditResult ValidateManifestEntry(OdfManifestEntry entry)
    {
        bool validPath = entry.FullPath == OdfPartNames.Root;
        if (!validPath)
        {
            PackagePathValue path = PackagePathRules.CanonicalizeEntryPath(
                entry.FullPath);
            validPath = path.IsValid && path.IsCanonical;
        }
        if (!validPath)
        {
            return CreateEditResult(
                OdfEditStatus.ValidationFailed,
                false,
                0,
                $"Manifest full path '{entry.FullPath}' is not canonical and safe.",
                entry.FullPath);
        }
        return CreateEditResult(
            OdfEditStatus.Success,
            false,
            0,
            "",
            entry.FullPath);
    }

    internal void RecalculateManifestValidity(OdfManifest manifest)
    {
        bool valid = manifest.Version == "1.2"
            || manifest.Version == "1.3"
            || manifest.Version == "1.4";
        valid = valid && manifest.Entries.Length > 0;
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            OdfEditResult validation = ValidateManifestEntry(entry);
            if (validation.Status != OdfEditStatus.Success)
            {
                valid = false;
            }
        }
        manifest.IsValid = valid;
    }

    internal bool ManifestEntriesMatch(
        OdfManifestEntry first,
        OdfManifestEntry second)
    {
        return first.FullPath == second.FullPath
            && first.MediaType == second.MediaType
            && first.Version == second.Version
            && first.PreferredViewMode == second.PreferredViewMode
            && first.Size == second.Size
            && first.SizeLexical == second.SizeLexical
            && first.HasSize == second.HasSize
            && first.SizeIsValid == second.SizeIsValid
            && first.EncryptionData == second.EncryptionData
            && first.ForeignAttributes == second.ForeignAttributes
            && first.ForeignElements == second.ForeignElements
            && first.Node == second.Node;
    }

    internal OdfEditResult CreateEditResult(
        OdfEditStatus status,
        bool changed,
        int affectedCount,
        string message,
        string path)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affectedCount;
        result.Message = message;
        result.Location.PartPath = path;
        return result;
    }
}
