using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackagePartDiscoveryTests
{
    [Test]
    public void DiscoversEveryStandardAndResourceCategoryInPackageOrder()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + FileEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.Styles, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.Metadata, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.Settings, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.DocumentSignatures, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.MacroSignatures, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.PackageSignatures, OdfMediaTypes.Xml)
            + FileEntry(OdfPartNames.MetadataManifest, OdfMediaTypes.RdfXml)
            + FileEntry("Metadata/graph.data", OdfMediaTypes.RdfXml)
            + FileEntry(OdfPartNames.Thumbnail, OdfMediaTypes.Png)
            + FileEntry("Pictures/photo.jpg", OdfMediaTypes.Jpeg)
            + FileEntry("ObjectReplacements/preview.bin", OdfMediaTypes.OctetStream)
            + "<m:file-entry m:full-path='Media/image-data' m:media-type='image/png'>"
            + "<m:encryption-data/></m:file-entry>"
            + FileEntry("Scripts/python/main.py", "text/x-python")
            + FileEntry("Configurations2/accelerator/current.xml", OdfMediaTypes.Xml)
            + FileEntry("Fonts/font.ttf", OdfMediaTypes.OctetStream)
            + "</m:manifest>";
        string[] paths =
        [
            OdfPartNames.Content,
            OdfPartNames.Styles,
            OdfPartNames.Metadata,
            OdfPartNames.Settings,
            OdfPartNames.DocumentSignatures,
            OdfPartNames.MacroSignatures,
            OdfPartNames.PackageSignatures,
            OdfPartNames.MetadataManifest,
            "Metadata/graph.data",
            OdfPartNames.ThumbnailsDirectory,
            OdfPartNames.Thumbnail,
            OdfPartNames.PicturesDirectory,
            "Pictures/photo.jpg",
            "ObjectReplacements/preview.bin",
            "Media/image-data",
            OdfPartNames.ScriptsDirectory,
            "Scripts/python/main.py",
            OdfPartNames.ConfigurationsDirectory,
            "Configurations2/accelerator/current.xml",
            "Fonts/font.ttf",
            "META-INF/custom.xml"
        ];
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            paths));

        PackagePartCatalog catalog = index.DiscoverParts();
        DiscoveredPackagePart encryptedImage = FindPart(
            catalog.ImageParts,
            "Media/image-data");
        DiscoveredPackagePart unmanifestedResource = FindPart(
            catalog.ResourceParts,
            "META-INF/custom.xml");

        Assert.Multiple(() =>
        {
            Assert.That(catalog.AllParts, Has.Length.EqualTo(23));
            Assert.That(CategoryCount(catalog), Is.EqualTo(catalog.AllParts.Length));
            Assert.That(PartPaths(catalog.InfrastructureParts), Is.EqualTo(new[] { OdfPartNames.Mimetype, OdfPartNames.Manifest }));
            Assert.That(catalog.DirectoryParts, Has.Length.EqualTo(4));
            Assert.That(PartPaths(catalog.ContentParts), Is.EqualTo(new[] { OdfPartNames.Content }));
            Assert.That(PartPaths(catalog.StyleParts), Is.EqualTo(new[] { OdfPartNames.Styles }));
            Assert.That(PartPaths(catalog.MetadataParts), Is.EqualTo(new[] { OdfPartNames.Metadata }));
            Assert.That(PartPaths(catalog.SettingsParts), Is.EqualTo(new[] { OdfPartNames.Settings }));
            Assert.That(catalog.SignatureParts, Has.Length.EqualTo(3));
            Assert.That(PartPaths(catalog.RdfParts), Is.EqualTo(new[] { OdfPartNames.MetadataManifest, "Metadata/graph.data" }));
            Assert.That(PartPaths(catalog.ThumbnailParts), Is.EqualTo(new[] { OdfPartNames.Thumbnail }));
            Assert.That(PartPaths(catalog.ImageParts), Is.EqualTo(new[] { "Pictures/photo.jpg", "ObjectReplacements/preview.bin", "Media/image-data" }));
            Assert.That(PartPaths(catalog.ScriptParts), Is.EqualTo(new[] { "Scripts/python/main.py" }));
            Assert.That(PartPaths(catalog.ConfigurationParts), Is.EqualTo(new[] { "Configurations2/accelerator/current.xml" }));
            Assert.That(PartPaths(catalog.ResourceParts), Is.EqualTo(new[] { "Fonts/font.ttf", "META-INF/custom.xml" }));
            Assert.That(encryptedImage.MediaType, Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(encryptedImage.HasManifestEntry, Is.True);
            Assert.That(encryptedImage.IsEncrypted, Is.True);
            Assert.That(encryptedImage.ManifestEntry, Is.Not.Null);
            Assert.That(unmanifestedResource.HasManifestEntry, Is.False);
            Assert.That(unmanifestedResource.MediaType, Is.EqualTo(OdfMediaTypes.Xml));
        });
    }

    [Test]
    public void PathAndExtensionFallbacksWorkWithoutAManifest()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackageWithoutManifest(
            [
                OdfPartNames.Content,
                "Pictures/photo.PNG",
                "data/graph.RDF",
                "Thumbnails/alternate.gif",
                "Scripts/main.js",
                "Configurations/current.xml",
                "custom.dat"
            ]));

        PackagePartCatalog catalog = index.DiscoverParts();

        Assert.Multiple(() =>
        {
            Assert.That(index.Manifest, Is.Null);
            Assert.That(PartPaths(catalog.ContentParts), Is.EqualTo(new[] { OdfPartNames.Content }));
            Assert.That(FindPart(catalog.ContentParts, OdfPartNames.Content).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(FindPart(catalog.ImageParts, "Pictures/photo.PNG").MediaType, Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(FindPart(catalog.RdfParts, "data/graph.RDF").MediaType, Is.EqualTo(OdfMediaTypes.RdfXml));
            Assert.That(PartPaths(catalog.ThumbnailParts), Is.EqualTo(new[] { "Thumbnails/alternate.gif" }));
            Assert.That(PartPaths(catalog.ScriptParts), Is.EqualTo(new[] { "Scripts/main.js" }));
            Assert.That(PartPaths(catalog.ConfigurationParts), Is.EqualTo(new[] { "Configurations/current.xml" }));
            Assert.That(PartPaths(catalog.ResourceParts), Is.EqualTo(new[] { "custom.dat" }));
        });
    }

    [Test]
    public void DiscoveryReflectsCurrentPathsAfterPackageMutations()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + FileEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + "</m:manifest>";
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            [OdfPartNames.Content]));

        index.AddEntry("Pictures/picture.png", [1], OdfCompressionMethod.Deflated);
        index.MoveEntry("Pictures/picture.png", "Scripts/picture.png");
        index.AddEntry("custom.bin", [2], OdfCompressionMethod.Stored);
        index.RenameEntry("custom.bin", OdfPartNames.Metadata);
        index.AddEntry("other.dat", [3], OdfCompressionMethod.Stored);
        index.RemoveEntry(OdfPartNames.Content);
        PackagePartCatalog catalog = index.DiscoverParts();

        Assert.Multiple(() =>
        {
            Assert.That(catalog.ContentParts, Is.Empty);
            Assert.That(PartPaths(catalog.ScriptParts), Is.EqualTo(new[] { "Scripts/picture.png" }));
            Assert.That(FindPart(catalog.ScriptParts, "Scripts/picture.png").MediaType, Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(PartPaths(catalog.MetadataParts), Is.EqualTo(new[] { OdfPartNames.Metadata }));
            Assert.That(FindPart(catalog.MetadataParts, OdfPartNames.Metadata).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(PartPaths(catalog.ResourceParts), Is.EqualTo(new[] { "other.dat" }));
            Assert.That(CategoryCount(catalog), Is.EqualTo(catalog.AllParts.Length));
        });
    }

    static int CategoryCount(PackagePartCatalog catalog)
    {
        return catalog.InfrastructureParts.Length
            + catalog.DirectoryParts.Length
            + catalog.ContentParts.Length
            + catalog.StyleParts.Length
            + catalog.MetadataParts.Length
            + catalog.SettingsParts.Length
            + catalog.SignatureParts.Length
            + catalog.RdfParts.Length
            + catalog.ThumbnailParts.Length
            + catalog.ImageParts.Length
            + catalog.ScriptParts.Length
            + catalog.ConfigurationParts.Length
            + catalog.ResourceParts.Length;
    }

    static DiscoveredPackagePart FindPart(
        DiscoveredPackagePart[] parts,
        string path)
    {
        foreach (DiscoveredPackagePart part in parts)
        {
            if (part.Entry.Path == path)
            {
                return part;
            }
        }
        Assert.Fail($"Expected discovered part '{path}'.");
        return new DiscoveredPackagePart();
    }

    static string[] PartPaths(DiscoveredPackagePart[] parts)
    {
        string[] result = new string[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            result[index] = parts[index].Entry.Path;
            index++;
        }
        return result;
    }

    static byte[] CreatePackage(string manifestXml, string[] paths)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                OdfMediaTypes.Text,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Manifest,
                manifestXml,
                CompressionLevel.SmallestSize);
            WritePaths(archive, paths);
        }
        return output.ToArray();
    }

    static byte[] CreatePackageWithoutManifest(string[] paths)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                OdfMediaTypes.Text,
                CompressionLevel.NoCompression);
            WritePaths(archive, paths);
        }
        return output.ToArray();
    }

    static void WritePaths(ZipArchive archive, string[] paths)
    {
        foreach (string path in paths)
        {
            if (path.EndsWith("/", StringComparison.Ordinal))
            {
                archive.CreateEntry(path, CompressionLevel.NoCompression);
            }
            else
            {
                WriteEntry(archive, path, "data", CompressionLevel.SmallestSize);
            }
        }
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compressionLevel)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);
        using Stream output = entry.Open();
        using StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false));
        writer.Write(content);
    }

    static string ManifestStart()
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' m:version='1.4'>";
    }

    static string FileEntry(string path, string mediaType)
    {
        return "<m:file-entry m:full-path='"
            + path
            + "' m:media-type='"
            + mediaType
            + "'/>";
    }
}
