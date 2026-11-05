using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace odf;

internal static class ManifestSynchronizer
{
    internal static void SynchronizePackageState(
        OdfManifest manifest,
        EditablePackageEntry[] packageEntries)
    {
        List<OdfManifestEntry> synchronized = new List<OdfManifestEntry>();
        OdfManifestEntry? root = FindManifestEntry(manifest.Entries, OdfPartNames.Root);
        if (root == null)
        {
            root = CreateManifestEntry(OdfPartNames.Root, "");
            root.Version = manifest.Version;
        }
        root.MediaType = FindPackageMediaType(packageEntries, OdfPartNames.Mimetype);
        synchronized.Add(root);

        foreach (OdfManifestEntry existing in manifest.Entries)
        {
            if (existing.FullPath == OdfPartNames.Root
                || !ShouldTrackPath(existing.FullPath)
                || ContainsManifestPath(synchronized, existing.FullPath))
            {
                continue;
            }
            EditablePackageEntry? packageEntry = FindPackageEntry(
                packageEntries,
                existing.FullPath);
            bool embeddedDirectory = IsEmbeddedDirectory(
                existing.FullPath,
                packageEntries);
            if (packageEntry != null || embeddedDirectory)
            {
                RefreshManifestEntry(existing, packageEntry, packageEntries);
                synchronized.Add(existing);
            }
        }

        foreach (EditablePackageEntry packageEntry in packageEntries)
        {
            string path = packageEntry.Entry.Path;
            if (!ShouldTrackPath(path) || ContainsManifestPath(synchronized, path))
            {
                continue;
            }
            OdfManifestEntry entry = CreateManifestEntry(
                path,
                InferMediaType(path, packageEntry.Data, packageEntries));
            RefreshManifestEntry(entry, packageEntry, packageEntries);
            synchronized.Add(entry);
        }

        foreach (EditablePackageEntry packageEntry in packageEntries)
        {
            string embeddedDirectory = GetEmbeddedDirectoryPath(packageEntry.Entry.Path);
            if (embeddedDirectory.Length == 0
                || ContainsManifestPath(synchronized, embeddedDirectory))
            {
                continue;
            }
            OdfManifestEntry entry = CreateManifestEntry(
                embeddedDirectory,
                FindPackageMediaType(packageEntries, embeddedDirectory + OdfPartNames.Mimetype));
            synchronized.Add(entry);
        }
        manifest.Entries = synchronized.ToArray();
    }

    internal static void SynchronizeChangedPath(
        OdfManifest manifest,
        string sourcePath,
        string destinationPath,
        EditablePackageEntry[] packageEntries)
    {
        OdfManifestEntry? entry = FindManifestEntry(manifest.Entries, sourcePath);
        if (entry != null)
        {
            entry.FullPath = destinationPath;
        }
        SynchronizePackageState(manifest, packageEntries);
    }

    internal static void SynchronizeCopiedPath(
        OdfManifest manifest,
        string sourcePath,
        string destinationPath,
        EditablePackageEntry[] packageEntries)
    {
        OdfManifestEntry? source = FindManifestEntry(manifest.Entries, sourcePath);
        if (source != null && FindManifestEntry(manifest.Entries, destinationPath) == null)
        {
            OdfManifestEntry[] expanded = new OdfManifestEntry[manifest.Entries.Length + 1];
            Array.Copy(manifest.Entries, expanded, manifest.Entries.Length);
            OdfManifestEntry copy = CloneManifestEntry(source);
            copy.FullPath = destinationPath;
            expanded[expanded.Length - 1] = copy;
            manifest.Entries = expanded;
        }
        SynchronizePackageState(manifest, packageEntries);
    }

    static void RefreshManifestEntry(
        OdfManifestEntry entry,
        EditablePackageEntry? packageEntry,
        EditablePackageEntry[] packageEntries)
    {
        if (entry.FullPath.EndsWith("/", StringComparison.Ordinal))
        {
            string packageMediaType = FindPackageMediaType(
                packageEntries,
                entry.FullPath + OdfPartNames.Mimetype);
            if (packageMediaType.Length > 0)
            {
                entry.MediaType = packageMediaType;
            }
            entry.HasSize = false;
            entry.Size = 0;
            entry.SizeLexical = "";
            entry.SizeIsValid = false;
            return;
        }
        if (packageEntry == null)
        {
            return;
        }
        string knownMediaType = InferKnownMediaType(entry.FullPath);
        if (knownMediaType.Length > 0)
        {
            entry.MediaType = knownMediaType;
        }
        else if (entry.MediaType.Length == 0)
        {
            entry.MediaType = InferMediaType(
                entry.FullPath,
                packageEntry.Data,
                packageEntries);
        }
        if (entry.HasSize || entry.EncryptionData != null)
        {
            entry.Size = packageEntry.Entry.ExpandedSize;
            entry.SizeLexical = entry.Size.ToString(CultureInfo.InvariantCulture);
            entry.HasSize = true;
            entry.SizeIsValid = true;
        }
    }

    static OdfManifestEntry CreateManifestEntry(string path, string mediaType)
    {
        OdfManifestEntry result = new OdfManifestEntry();
        result.FullPath = path;
        result.MediaType = mediaType;
        result.Node.Name.NamespaceName = OdfNamespaces.Manifest;
        result.Node.Name.LocalName = "file-entry";
        result.Node.Name.Prefix = "manifest";
        result.Node.IsManifestElement = true;
        return result;
    }

    static bool ShouldTrackPath(string path)
    {
        return path.Length > 0
            && path != OdfPartNames.Root
            && path != OdfPartNames.Mimetype
            && path != OdfPartNames.Manifest;
    }

    static bool ContainsManifestPath(
        List<OdfManifestEntry> entries,
        string path)
    {
        foreach (OdfManifestEntry entry in entries)
        {
            if (entry.FullPath == path)
            {
                return true;
            }
        }
        return false;
    }

    static OdfManifestEntry? FindManifestEntry(
        OdfManifestEntry[] entries,
        string path)
    {
        foreach (OdfManifestEntry entry in entries)
        {
            if (entry.FullPath == path)
            {
                return entry;
            }
        }
        return null;
    }

    static EditablePackageEntry? FindPackageEntry(
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

    static string FindPackageMediaType(
        EditablePackageEntry[] entries,
        string path)
    {
        EditablePackageEntry? entry = FindPackageEntry(entries, path);
        if (entry == null || !entry.IsContentAvailable)
        {
            return "";
        }
        string mediaType = Encoding.UTF8.GetString(entry.Data);
        OdfMediaTypeValue parsed = OdfMediaTypeRules.ParseMediaType(mediaType);
        if (!parsed.IsValid
            || !mediaType.StartsWith(
                "application/vnd.oasis.opendocument.",
                StringComparison.Ordinal))
        {
            return "";
        }
        return mediaType;
    }

    static bool IsEmbeddedDirectory(
        string path,
        EditablePackageEntry[] packageEntries)
    {
        return path.EndsWith("/", StringComparison.Ordinal)
            && FindPackageMediaType(
                packageEntries,
                path + OdfPartNames.Mimetype).Length > 0;
    }

    static string GetEmbeddedDirectoryPath(string path)
    {
        if (!path.EndsWith("/" + OdfPartNames.Mimetype, StringComparison.Ordinal))
        {
            return "";
        }
        return path.Substring(0, path.Length - OdfPartNames.Mimetype.Length);
    }

    static string InferMediaType(
        string path,
        byte[] data,
        EditablePackageEntry[] packageEntries)
    {
        if (path.EndsWith("/", StringComparison.Ordinal))
        {
            return FindPackageMediaType(
                packageEntries,
                path + OdfPartNames.Mimetype);
        }
        string known = InferKnownMediaType(path);
        if (known.Length > 0)
        {
            return known;
        }
        if (path.EndsWith("/" + OdfPartNames.Mimetype, StringComparison.Ordinal))
        {
            string mediaType = Encoding.UTF8.GetString(data);
            OdfMediaTypeValue parsed = OdfMediaTypeRules.ParseMediaType(mediaType);
            if (parsed.IsValid)
            {
                return "text/plain";
            }
        }
        return OdfMediaTypes.OctetStream;
    }

    static string InferKnownMediaType(string path)
    {
        if (path == OdfPartNames.Content
            || path == OdfPartNames.Styles
            || path == OdfPartNames.Metadata
            || path == OdfPartNames.Settings
            || path == OdfPartNames.DocumentSignatures
            || path == OdfPartNames.MacroSignatures
            || path == OdfPartNames.PackageSignatures)
        {
            return OdfMediaTypes.Xml;
        }
        if (path == OdfPartNames.MetadataManifest)
        {
            return OdfMediaTypes.RdfXml;
        }
        if (path == OdfPartNames.Thumbnail)
        {
            return OdfMediaTypes.Png;
        }
        string lower = path.ToLowerInvariant();
        if (lower.EndsWith(".xml", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Xml;
        }
        if (lower.EndsWith(".rdf", StringComparison.Ordinal))
        {
            return OdfMediaTypes.RdfXml;
        }
        if (lower.EndsWith(".png", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Png;
        }
        if (lower.EndsWith(".jpg", StringComparison.Ordinal)
            || lower.EndsWith(".jpeg", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Jpeg;
        }
        if (lower.EndsWith(".gif", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Gif;
        }
        if (lower.EndsWith(".svg", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Svg;
        }
        if (lower.EndsWith(".odt", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Text;
        }
        if (lower.EndsWith(".ott", StringComparison.Ordinal))
        {
            return OdfMediaTypes.TextTemplate;
        }
        if (lower.EndsWith(".odm", StringComparison.Ordinal))
        {
            return OdfMediaTypes.TextMaster;
        }
        if (lower.EndsWith(".ods", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Spreadsheet;
        }
        if (lower.EndsWith(".ots", StringComparison.Ordinal))
        {
            return OdfMediaTypes.SpreadsheetTemplate;
        }
        if (lower.EndsWith(".odp", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Presentation;
        }
        if (lower.EndsWith(".otp", StringComparison.Ordinal))
        {
            return OdfMediaTypes.PresentationTemplate;
        }
        if (lower.EndsWith(".odg", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Graphics;
        }
        if (lower.EndsWith(".otg", StringComparison.Ordinal))
        {
            return OdfMediaTypes.GraphicsTemplate;
        }
        if (lower.EndsWith(".odc", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Chart;
        }
        if (lower.EndsWith(".odf", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Formula;
        }
        if (lower.EndsWith(".odi", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Image;
        }
        if (lower.EndsWith(".odb", StringComparison.Ordinal))
        {
            return OdfMediaTypes.Base;
        }
        return "";
    }

    static OdfManifestEntry CloneManifestEntry(OdfManifestEntry source)
    {
        OdfManifestEntry result = new OdfManifestEntry();
        result.FullPath = source.FullPath;
        result.MediaType = source.MediaType;
        result.Version = source.Version;
        result.PreferredViewMode = source.PreferredViewMode;
        result.Size = source.Size;
        result.SizeLexical = source.SizeLexical;
        result.HasSize = source.HasSize;
        result.SizeIsValid = source.SizeIsValid;
        if (source.EncryptionData != null)
        {
            result.EncryptionData = CloneManifestNode(source.EncryptionData);
            if (source.Encryption != null)
            {
                OdfManifest parsingManifest = new OdfManifest();
                result.Encryption = ManifestEncryptionParser.ParseEncryptionData(
                    result.EncryptionData,
                    result,
                    parsingManifest);
            }
        }
        result.ForeignAttributes = CloneManifestAttributes(source.ForeignAttributes);
        result.ForeignElements = CloneManifestNodes(source.ForeignElements);
        result.Node = CloneManifestNode(source.Node);
        return result;
    }

    static OdfManifestNode CloneManifestNode(OdfManifestNode source)
    {
        OdfManifestNode result = new OdfManifestNode();
        result.Name.NamespaceName = source.Name.NamespaceName;
        result.Name.LocalName = source.Name.LocalName;
        result.Name.Prefix = source.Name.Prefix;
        result.NamespaceDeclarations = CloneNamespaceDeclarations(
            source.NamespaceDeclarations);
        result.Attributes = CloneManifestAttributes(source.Attributes);
        result.Children = CloneManifestNodes(source.Children);
        result.OrderedContentXml = CloneStrings(source.OrderedContentXml);
        result.OrderedContentElementIndexes = CloneIntegers(
            source.OrderedContentElementIndexes);
        result.TextContent = source.TextContent;
        result.OriginalXml = source.OriginalXml;
        result.IsManifestElement = source.IsManifestElement;
        return result;
    }

    static OdfManifestNode[] CloneManifestNodes(OdfManifestNode[] source)
    {
        OdfManifestNode[] result = new OdfManifestNode[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CloneManifestNode(source[index]);
            index = index + 1;
        }
        return result;
    }

    static OdfAttribute[] CloneManifestAttributes(OdfAttribute[] source)
    {
        OdfAttribute[] result = new OdfAttribute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfAttribute attribute = new OdfAttribute();
            attribute.Name.NamespaceName = source[index].Name.NamespaceName;
            attribute.Name.LocalName = source[index].Name.LocalName;
            attribute.Name.Prefix = source[index].Name.Prefix;
            attribute.Value = source[index].Value;
            result[index] = attribute;
            index = index + 1;
        }
        return result;
    }

    static OdfNamespaceDeclaration[] CloneNamespaceDeclarations(
        OdfNamespaceDeclaration[] source)
    {
        OdfNamespaceDeclaration[] result = new OdfNamespaceDeclaration[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            OdfNamespaceDeclaration declaration = new OdfNamespaceDeclaration();
            declaration.Prefix = source[index].Prefix;
            declaration.NamespaceName = source[index].NamespaceName;
            result[index] = declaration;
            index = index + 1;
        }
        return result;
    }

    static string[] CloneStrings(string[] source)
    {
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static int[] CloneIntegers(int[] source)
    {
        int[] result = new int[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }
}
