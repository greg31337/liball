using System;
using System.Collections.Generic;

namespace odf;

internal class DiscoveredPackagePart
{
    internal OdfPackageEntry Entry = new OdfPackageEntry();
    internal OdfManifestEntry? ManifestEntry;
    internal string MediaType = "";
    internal bool HasManifestEntry;
    internal bool IsEncrypted;
}

internal class PackagePartCatalog
{
    internal DiscoveredPackagePart[] AllParts = [];
    internal DiscoveredPackagePart[] InfrastructureParts = [];
    internal DiscoveredPackagePart[] DirectoryParts = [];
    internal DiscoveredPackagePart[] ContentParts = [];
    internal DiscoveredPackagePart[] StyleParts = [];
    internal DiscoveredPackagePart[] MetadataParts = [];
    internal DiscoveredPackagePart[] SettingsParts = [];
    internal DiscoveredPackagePart[] SignatureParts = [];
    internal DiscoveredPackagePart[] RdfParts = [];
    internal DiscoveredPackagePart[] ThumbnailParts = [];
    internal DiscoveredPackagePart[] ImageParts = [];
    internal DiscoveredPackagePart[] ScriptParts = [];
    internal DiscoveredPackagePart[] ConfigurationParts = [];
    internal DiscoveredPackagePart[] ResourceParts = [];
}

internal static class PackagePartDiscovery
{
    internal static PackagePartCatalog DiscoverParts(
        OdfPackageEntry[] packageEntries,
        OdfManifest? manifest)
    {
        PackagePartCatalog result = new PackagePartCatalog();
        List<DiscoveredPackagePart> all = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> infrastructure = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> directories = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> content = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> styles = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> metadata = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> settings = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> signatures = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> rdf = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> thumbnails = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> images = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> scripts = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> configurations = new List<DiscoveredPackagePart>();
        List<DiscoveredPackagePart> resources = new List<DiscoveredPackagePart>();

        foreach (OdfPackageEntry entry in packageEntries)
        {
            DiscoveredPackagePart part = CreatePart(entry, manifest);
            all.Add(part);
            string path = entry.Path;
            if (entry.IsDirectory)
            {
                directories.Add(part);
            }
            else if (path == OdfPartNames.Mimetype || path == OdfPartNames.Manifest)
            {
                infrastructure.Add(part);
            }
            else if (path == OdfPartNames.Content)
            {
                content.Add(part);
            }
            else if (path == OdfPartNames.Styles)
            {
                styles.Add(part);
            }
            else if (path == OdfPartNames.Metadata)
            {
                metadata.Add(part);
            }
            else if (path == OdfPartNames.Settings)
            {
                settings.Add(part);
            }
            else if (IsSignaturePath(path))
            {
                signatures.Add(part);
            }
            else if (IsRdfPart(path, part.MediaType))
            {
                rdf.Add(part);
            }
            else if (IsThumbnailPath(path))
            {
                thumbnails.Add(part);
            }
            else if (IsScriptPath(path))
            {
                scripts.Add(part);
            }
            else if (IsConfigurationPath(path))
            {
                configurations.Add(part);
            }
            else if (IsImagePart(path, part.MediaType))
            {
                images.Add(part);
            }
            else
            {
                resources.Add(part);
            }
        }

        result.AllParts = all.ToArray();
        result.InfrastructureParts = infrastructure.ToArray();
        result.DirectoryParts = directories.ToArray();
        result.ContentParts = content.ToArray();
        result.StyleParts = styles.ToArray();
        result.MetadataParts = metadata.ToArray();
        result.SettingsParts = settings.ToArray();
        result.SignatureParts = signatures.ToArray();
        result.RdfParts = rdf.ToArray();
        result.ThumbnailParts = thumbnails.ToArray();
        result.ImageParts = images.ToArray();
        result.ScriptParts = scripts.ToArray();
        result.ConfigurationParts = configurations.ToArray();
        result.ResourceParts = resources.ToArray();
        return result;
    }

    static DiscoveredPackagePart CreatePart(
        OdfPackageEntry entry,
        OdfManifest? manifest)
    {
        DiscoveredPackagePart result = new DiscoveredPackagePart();
        result.Entry = entry;
        OdfManifestEntry? manifestEntry = FindManifestPart(manifest, entry.Path);
        if (manifestEntry != null)
        {
            result.ManifestEntry = manifestEntry;
            result.MediaType = manifestEntry.MediaType;
            result.HasManifestEntry = true;
            result.IsEncrypted = manifestEntry.EncryptionData != null;
        }
        if (result.MediaType.Length == 0)
        {
            result.MediaType = InferPartMediaType(entry.Path);
        }
        result.IsEncrypted = result.IsEncrypted || entry.IsEncrypted;
        return result;
    }

    static OdfManifestEntry? FindManifestPart(
        OdfManifest? manifest,
        string path)
    {
        if (manifest == null)
        {
            return null;
        }
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == path)
            {
                return entry;
            }
        }
        return null;
    }

    static bool IsSignaturePath(string path)
    {
        return path == OdfPartNames.DocumentSignatures
            || path == OdfPartNames.MacroSignatures
            || path == OdfPartNames.PackageSignatures;
    }

    static bool IsRdfPart(string path, string mediaType)
    {
        return path == OdfPartNames.MetadataManifest
            || mediaType == OdfMediaTypes.RdfXml
            || path.EndsWith(".rdf", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsThumbnailPath(string path)
    {
        return path == OdfPartNames.Thumbnail
            || path.StartsWith(
                OdfPartNames.ThumbnailsDirectory,
                StringComparison.Ordinal);
    }

    static bool IsScriptPath(string path)
    {
        return path.StartsWith(OdfPartNames.ScriptsDirectory, StringComparison.Ordinal);
    }

    static bool IsConfigurationPath(string path)
    {
        return path.StartsWith(
            OdfPartNames.ConfigurationsDirectory,
            StringComparison.Ordinal)
            || path.StartsWith("Configurations/", StringComparison.Ordinal);
    }

    static bool IsImagePart(string path, string mediaType)
    {
        return path.StartsWith(OdfPartNames.PicturesDirectory, StringComparison.Ordinal)
            || path.StartsWith(
                OdfPartNames.ObjectReplacementsDirectory,
                StringComparison.Ordinal)
            || mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || HasImageExtension(path);
    }

    static bool HasImageExtension(string path)
    {
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tif", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    static string InferPartMediaType(string path)
    {
        if (path == OdfPartNames.Content
            || path == OdfPartNames.Styles
            || path == OdfPartNames.Metadata
            || path == OdfPartNames.Settings
            || IsSignaturePath(path)
            || path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Xml;
        }
        if (path == OdfPartNames.MetadataManifest
            || path.EndsWith(".rdf", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.RdfXml;
        }
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Png;
        }
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Jpeg;
        }
        if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Gif;
        }
        if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return OdfMediaTypes.Svg;
        }
        return "";
    }
}
