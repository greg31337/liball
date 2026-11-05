using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class EmbeddedPackageNode
{
    internal string Path = "";
    internal string QualifiedPath = "";
    internal string MediaType = "";
    internal int Depth;
    internal bool IsDirectoryPackage;
    internal bool IsArchivePackage;
    internal bool IsValid;
    internal bool TraversalStopped;
    internal OdfPackageEntry[] Entries = [];
    internal EmbeddedPackageNode[] Children = [];
    internal OdfDiagnostic[] Diagnostics = [];
    internal string DetectedMediaType = "";
    internal OdfManifest? Manifest;
    internal ZipEntryIndex? EntryIndex;
}

internal class EmbeddedPackageTraversal
{
    internal EmbeddedPackageNode[] Packages = [];
    internal OdfDiagnostic[] Diagnostics = [];
    internal bool IsComplete = true;
}

internal static class EmbeddedPackageWalker
{
    internal static EmbeddedPackageTraversal WalkIndex(
        ZipEntryIndex index,
        OdfSecurityLimits limits)
    {
        EmbeddedPackageTraversal result = new EmbeddedPackageTraversal();
        List<OdfDiagnostic> diagnostics = new List<OdfDiagnostic>();
        if (limits.MaximumEmbeddedPackageDepth < 0)
        {
            result.IsComplete = false;
            diagnostics.Add(CreateTraversalDiagnostic(
                "The maximum embedded-package depth cannot be negative.",
                ""));
            result.Diagnostics = diagnostics.ToArray();
            return result;
        }
        result.Packages = WalkContext(
            index,
            "",
            "",
            0,
            limits,
            result,
            diagnostics);
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    static EmbeddedPackageNode[] WalkContext(
        ZipEntryIndex index,
        string currentPrefix,
        string qualifiedBase,
        int currentDepth,
        OdfSecurityLimits limits,
        EmbeddedPackageTraversal traversal,
        List<OdfDiagnostic> diagnostics)
    {
        List<EmbeddedPackageNode> nodes = new List<EmbeddedPackageNode>();
        EditablePackageEntry[] physicalEntries = index.GetEditableEntries();
        string[] directoryPaths = FindDirectDirectoryPackages(
            index.Manifest,
            physicalEntries,
            currentPrefix);

        foreach (string directoryPath in directoryPaths)
        {
            int depth = currentDepth + 1;
            string qualifiedPath = qualifiedBase + directoryPath;
            EmbeddedPackageNode node = new EmbeddedPackageNode();
            node.Path = directoryPath;
            node.QualifiedPath = qualifiedPath;
            node.MediaType = FindDirectoryMediaType(
                index.Manifest,
                physicalEntries,
                directoryPath);
            node.Depth = depth;
            node.IsDirectoryPackage = true;
            node.Entries = SelectDirectoryEntries(physicalEntries, directoryPath);
            if (depth > limits.MaximumEmbeddedPackageDepth)
            {
                StopAtDepthLimit(node, limits, traversal, diagnostics);
            }
            else
            {
                node.IsValid = node.MediaType.Length > 0;
                node.Children = WalkContext(
                    index,
                    directoryPath,
                    qualifiedBase,
                    depth,
                    limits,
                    traversal,
                    diagnostics);
            }
            nodes.Add(node);
        }

        foreach (EditablePackageEntry item in physicalEntries)
        {
            string path = item.Entry.Path;
            if (item.Entry.IsDirectory
                || !path.StartsWith(currentPrefix, StringComparison.Ordinal)
                || IsPackageInfrastructure(path, currentPrefix)
                || IsInsideDirectoryPackage(path, directoryPaths))
            {
                continue;
            }
            string mediaType = FindManifestMediaType(index.Manifest, path);
            bool declared = IsOdfMediaType(mediaType) || HasOdfExtension(path);
            bool zipData = item.IsContentAvailable && HasZipSignature(item.Data);
            if (!declared && !zipData)
            {
                continue;
            }
            int depth = currentDepth + 1;
            string qualifiedPath = qualifiedBase + path;
            EmbeddedPackageNode node = new EmbeddedPackageNode();
            node.Path = path;
            node.QualifiedPath = qualifiedPath;
            node.MediaType = mediaType;
            node.Depth = depth;
            node.IsArchivePackage = true;
            if (depth > limits.MaximumEmbeddedPackageDepth)
            {
                if (declared)
                {
                    StopAtDepthLimit(node, limits, traversal, diagnostics);
                    nodes.Add(node);
                }
                continue;
            }
            if (!item.IsContentAvailable || !zipData)
            {
                OdfDiagnostic diagnostic = CreateTraversalDiagnostic(
                    $"Embedded package '{qualifiedPath}' is unavailable or is not a ZIP package.",
                    qualifiedPath);
                node.Diagnostics = [diagnostic];
                node.IsValid = false;
                diagnostics.Add(diagnostic);
                nodes.Add(node);
                continue;
            }

            ZipPackageInspection inspection = ZipPackageReader.InspectBytesWithLimits(
                item.Data,
                limits,
                depth);
            node.DetectedMediaType = inspection.MediaType;
            node.Manifest = inspection.Manifest;
            bool odfPackage = IsOdfMediaType(inspection.MediaType)
                || IsOdfMediaType(inspection.ManifestMediaType);
            if (!declared && !odfPackage)
            {
                continue;
            }
            if (node.MediaType.Length == 0)
            {
                node.MediaType = inspection.MediaType;
            }
            QualifyInspectionDiagnostics(inspection.Diagnostics, qualifiedPath);
            node.Entries = inspection.Entries;
            node.Diagnostics = inspection.Diagnostics;
            node.IsValid = inspection.IsValid && odfPackage;
            AppendDiagnostics(inspection.Diagnostics, diagnostics);
            if (HasFatalDiagnostic(inspection.Diagnostics))
            {
                traversal.IsComplete = false;
                node.TraversalStopped = true;
                nodes.Add(node);
                continue;
            }
            ZipEntryIndex nestedIndex = new ZipEntryIndex(item.Data, inspection);
            node.EntryIndex = nestedIndex;
            node.Children = WalkContext(
                nestedIndex,
                "",
                qualifiedPath + "!/",
                depth,
                limits,
                traversal,
                diagnostics);
            nodes.Add(node);
        }
        return nodes.ToArray();
    }

    static string[] FindDirectDirectoryPackages(
        OdfManifest? manifest,
        EditablePackageEntry[] physicalEntries,
        string currentPrefix)
    {
        List<string> candidates = new List<string>();
        if (manifest != null)
        {
            foreach (OdfManifestEntry entry in manifest.Entries)
            {
                if (entry.FullPath != currentPrefix
                    && entry.FullPath != OdfPartNames.Root
                    && entry.FullPath.StartsWith(currentPrefix, StringComparison.Ordinal)
                    && entry.FullPath.EndsWith("/", StringComparison.Ordinal)
                    && IsOdfMediaType(entry.MediaType)
                    && !candidates.Contains(entry.FullPath))
                {
                    candidates.Add(entry.FullPath);
                }
            }
        }
        foreach (EditablePackageEntry item in physicalEntries)
        {
            string directoryPath = GetMimetypeDirectory(item.Entry.Path);
            if (directoryPath.Length > 0
                && directoryPath != currentPrefix
                && directoryPath.StartsWith(currentPrefix, StringComparison.Ordinal)
                && item.IsContentAvailable
                && IsOdfMediaType(Encoding.UTF8.GetString(item.Data))
                && !candidates.Contains(directoryPath))
            {
                candidates.Add(directoryPath);
            }
        }

        List<string> direct = new List<string>();
        foreach (string candidate in candidates)
        {
            bool hasCloserParent = false;
            foreach (string possibleParent in candidates)
            {
                if (candidate != possibleParent
                    && possibleParent.StartsWith(currentPrefix, StringComparison.Ordinal)
                    && candidate.StartsWith(possibleParent, StringComparison.Ordinal))
                {
                    hasCloserParent = true;
                    break;
                }
            }
            if (!hasCloserParent)
            {
                direct.Add(candidate);
            }
        }
        return direct.ToArray();
    }

    static string FindDirectoryMediaType(
        OdfManifest? manifest,
        EditablePackageEntry[] physicalEntries,
        string directoryPath)
    {
        string manifestMediaType = FindManifestMediaType(manifest, directoryPath);
        if (IsOdfMediaType(manifestMediaType))
        {
            return manifestMediaType;
        }
        EditablePackageEntry? mimetype = FindEditableEntry(
            physicalEntries,
            directoryPath + OdfPartNames.Mimetype);
        if (mimetype != null && mimetype.IsContentAvailable)
        {
            string mediaType = Encoding.UTF8.GetString(mimetype.Data);
            if (IsOdfMediaType(mediaType))
            {
                return mediaType;
            }
        }
        return "";
    }

    static string FindManifestMediaType(OdfManifest? manifest, string path)
    {
        if (manifest == null)
        {
            return "";
        }
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == path)
            {
                return entry.MediaType;
            }
        }
        return "";
    }

    static EditablePackageEntry? FindEditableEntry(
        EditablePackageEntry[] entries,
        string path)
    {
        foreach (EditablePackageEntry entry in entries)
        {
            if (entry.Entry.Path == path)
            {
                return entry;
            }
        }
        return null;
    }

    static OdfPackageEntry[] SelectDirectoryEntries(
        EditablePackageEntry[] entries,
        string directoryPath)
    {
        List<OdfPackageEntry> result = new List<OdfPackageEntry>();
        foreach (EditablePackageEntry entry in entries)
        {
            if (entry.Entry.Path.StartsWith(directoryPath, StringComparison.Ordinal))
            {
                result.Add(entry.Entry);
            }
        }
        return result.ToArray();
    }

    static bool IsInsideDirectoryPackage(string path, string[] directoryPaths)
    {
        foreach (string directoryPath in directoryPaths)
        {
            if (path.StartsWith(directoryPath, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    static bool IsPackageInfrastructure(string path, string currentPrefix)
    {
        return path == currentPrefix + OdfPartNames.Mimetype
            || path == currentPrefix + OdfPartNames.Manifest;
    }

    static string GetMimetypeDirectory(string path)
    {
        if (!path.EndsWith("/" + OdfPartNames.Mimetype, StringComparison.Ordinal))
        {
            return "";
        }
        return path.Substring(0, path.Length - OdfPartNames.Mimetype.Length);
    }

    static bool IsOdfMediaType(string mediaType)
    {
        if (!mediaType.StartsWith(
            "application/vnd.oasis.opendocument.",
            StringComparison.Ordinal))
        {
            return false;
        }
        return OdfMediaTypeRules.ParseMediaType(mediaType).IsValid;
    }

    static bool HasOdfExtension(string path)
    {
        string lower = path.ToLowerInvariant();
        return lower.EndsWith(".odt", StringComparison.Ordinal)
            || lower.EndsWith(".ott", StringComparison.Ordinal)
            || lower.EndsWith(".odm", StringComparison.Ordinal)
            || lower.EndsWith(".ods", StringComparison.Ordinal)
            || lower.EndsWith(".ots", StringComparison.Ordinal)
            || lower.EndsWith(".odp", StringComparison.Ordinal)
            || lower.EndsWith(".otp", StringComparison.Ordinal)
            || lower.EndsWith(".odg", StringComparison.Ordinal)
            || lower.EndsWith(".otg", StringComparison.Ordinal)
            || lower.EndsWith(".odc", StringComparison.Ordinal)
            || lower.EndsWith(".odf", StringComparison.Ordinal)
            || lower.EndsWith(".odi", StringComparison.Ordinal)
            || lower.EndsWith(".odb", StringComparison.Ordinal);
    }

    static bool HasZipSignature(byte[] data)
    {
        return data.Length >= 4
            && data[0] == 80
            && data[1] == 75
            && (data[2] == 3 && data[3] == 4
                || data[2] == 5 && data[3] == 6
                || data[2] == 7 && data[3] == 8);
    }

    static void StopAtDepthLimit(
        EmbeddedPackageNode node,
        OdfSecurityLimits limits,
        EmbeddedPackageTraversal traversal,
        List<OdfDiagnostic> diagnostics)
    {
        OdfDiagnostic diagnostic = CreateTraversalDiagnostic(
            $"Embedded package depth {node.Depth} exceeds the configured limit {limits.MaximumEmbeddedPackageDepth}.",
            node.QualifiedPath);
        node.TraversalStopped = true;
        node.IsValid = false;
        node.Diagnostics = [diagnostic];
        traversal.IsComplete = false;
        diagnostics.Add(diagnostic);
    }

    static OdfDiagnostic CreateTraversalDiagnostic(string message, string path)
    {
        OdfLocation location = new OdfLocation();
        location.PartPath = path;
        return OdfDiagnosticFactory.Create(
            OdfDiagnosticCodes.InvalidPackage,
            message,
            OdfValidationSeverity.Fatal,
            location,
            null,
            OdfVersion.Unknown);
    }

    static void AppendDiagnostics(
        OdfDiagnostic[] source,
        List<OdfDiagnostic> destination)
    {
        foreach (OdfDiagnostic diagnostic in source)
        {
            destination.Add(diagnostic);
        }
    }

    static bool HasFatalDiagnostic(OdfDiagnostic[] diagnostics)
    {
        foreach (OdfDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Severity == OdfValidationSeverity.Fatal)
            {
                return true;
            }
        }
        return false;
    }

    static void QualifyInspectionDiagnostics(
        OdfDiagnostic[] diagnostics,
        string qualifiedPath)
    {
        foreach (OdfDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Location.PartPath.Length == 0)
            {
                diagnostic.Location.PartPath = qualifiedPath;
            }
            else
            {
                diagnostic.Location.PartPath = qualifiedPath
                    + "!/"
                    + diagnostic.Location.PartPath;
            }
            if (diagnostic.HasRelatedLocation)
            {
                if (diagnostic.RelatedLocation.PartPath.Length == 0)
                {
                    diagnostic.RelatedLocation.PartPath = qualifiedPath;
                }
                else
                {
                    diagnostic.RelatedLocation.PartPath = qualifiedPath
                        + "!/"
                        + diagnostic.RelatedLocation.PartPath;
                }
            }
        }
    }
}
