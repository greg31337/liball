using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ManifestSynchronizerTests
{
    [Test]
    public void ReconcilesDirectoriesResourcesAndStandardPartsAfterAdds()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + FileEntry(OdfPartNames.Content, OdfMediaTypes.Xml)
            + FileEntry("stale.bin", OdfMediaTypes.OctetStream)
            + "</m:manifest>";
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            [OdfPartNames.Content, OdfPartNames.Styles],
            ["content", "styles"]));

        index.AddEntry("Pictures/", [], OdfCompressionMethod.Stored);
        index.AddEntry("Pictures/photo.png", [1, 2, 3], OdfCompressionMethod.Deflated);
        index.AddEntry(OdfPartNames.Metadata, Encoding.UTF8.GetBytes("meta"), OdfCompressionMethod.Deflated);
        index.AddEntry(OdfPartNames.Settings, Encoding.UTF8.GetBytes("settings"), OdfCompressionMethod.Deflated);
        index.AddEntry(OdfPartNames.DocumentSignatures, Encoding.UTF8.GetBytes("signatures"), OdfCompressionMethod.Deflated);
        index.AddEntry(OdfPartNames.MetadataManifest, Encoding.UTF8.GetBytes("rdf"), OdfCompressionMethod.Deflated);
        index.AddEntry(OdfPartNames.Thumbnail, [4, 5, 6], OdfCompressionMethod.Deflated);

        OdfManifest manifest = index.Manifest!;
        OdfManifest stored = ReadStoredManifest(index);

        Assert.Multiple(() =>
        {
            Assert.That(FindManifestEntry(manifest, OdfPartNames.Root).MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.Styles).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(FindManifestEntry(manifest, "Pictures/").MediaType, Is.Empty);
            Assert.That(FindManifestEntry(manifest, "Pictures/photo.png").MediaType, Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.Metadata).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.Settings).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.DocumentSignatures).MediaType, Is.EqualTo(OdfMediaTypes.Xml));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.MetadataManifest).MediaType, Is.EqualTo(OdfMediaTypes.RdfXml));
            Assert.That(FindManifestEntry(manifest, OdfPartNames.Thumbnail).MediaType, Is.EqualTo(OdfMediaTypes.Png));
            Assert.That(HasManifestEntry(manifest, "stale.bin"), Is.False);
            Assert.That(HasManifestEntry(manifest, OdfPartNames.Mimetype), Is.False);
            Assert.That(HasManifestEntry(manifest, OdfPartNames.Manifest), Is.False);
            Assert.That(ManifestPaths(stored), Is.EqualTo(ManifestPaths(manifest)));
            Assert.That(FindManifestEntry(stored, "Pictures/photo.png").MediaType, Is.EqualTo(OdfMediaTypes.Png));
        });
    }

    [Test]
    public void ReplaceRenameMoveCopyAndRemoveKeepManifestMetadataSynchronized()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + "<m:file-entry m:full-path='secret.bin' m:media-type='application/octet-stream'"
            + " m:size='3' ext:tag='keep'><m:encryption-data m:checksum-type='SHA256'"
            + " m:checksum='Y2hlY2s='><m:algorithm m:algorithm-name='urn:test'"
            + " m:initialisation-vector='aXY='/></m:encryption-data></m:file-entry>"
            + "</m:manifest>";
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            ["secret.bin"],
            ["one"]));

        OdfEditResult replaced = index.ReplaceEntry(
            "secret.bin",
            Encoding.UTF8.GetBytes("three"));
        OdfEditResult renamed = index.RenameEntry("secret.bin", "renamed.bin");
        OdfEditResult copied = index.CopyEntry("renamed.bin", "copied.bin");
        OdfEditResult moved = index.MoveEntry("copied.bin", "Archive/moved.bin");
        OdfManifestEntry renamedEntry = FindManifestEntry(index.Manifest!, "renamed.bin");
        OdfManifestEntry movedEntry = FindManifestEntry(index.Manifest!, "Archive/moved.bin");
        OdfEditResult removed = index.RemoveEntry("renamed.bin");
        OdfManifest stored = ReadStoredManifest(index);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(renamedEntry.Size, Is.EqualTo(5));
            Assert.That(renamedEntry.SizeLexical, Is.EqualTo("5"));
            Assert.That(renamedEntry.EncryptionData, Is.Not.Null);
            Assert.That(renamedEntry.ForeignAttributes[0].Value, Is.EqualTo("keep"));
            Assert.That(movedEntry.Size, Is.EqualTo(5));
            Assert.That(movedEntry.EncryptionData, Is.Not.Null);
            Assert.That(movedEntry.EncryptionData, Is.Not.SameAs(renamedEntry.EncryptionData));
            Assert.That(movedEntry.ForeignAttributes[0], Is.Not.SameAs(renamedEntry.ForeignAttributes[0]));
            Assert.That(HasManifestEntry(index.Manifest!, "renamed.bin"), Is.False);
            Assert.That(HasManifestEntry(index.Manifest!, "Archive/moved.bin"), Is.True);
            Assert.That(HasManifestEntry(stored, "renamed.bin"), Is.False);
            Assert.That(FindManifestEntry(stored, "Archive/moved.bin").Size, Is.EqualTo(5));
        });
    }

    [Test]
    public void NestedMimetypeEntriesClassifyImplicitEmbeddedSubdocuments()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + "</m:manifest>";
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            [],
            []));

        OdfEditResult added = index.AddEntry(
            "Object1/mimetype",
            Encoding.UTF8.GetBytes(OdfMediaTypes.Spreadsheet),
            OdfCompressionMethod.Stored);
        OdfEditResult replaced = index.ReplaceEntry(
            "Object1/mimetype",
            Encoding.UTF8.GetBytes(OdfMediaTypes.Presentation));
        OdfEditResult copied = index.CopyEntry(
            "Object1/mimetype",
            "Object2/mimetype");
        OdfEditResult removed = index.RemoveEntry("Object1/mimetype");
        OdfManifest manifest = index.Manifest!;
        OdfManifest stored = ReadStoredManifest(index);

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasManifestEntry(manifest, "Object1/"), Is.False);
            Assert.That(FindManifestEntry(manifest, "Object2/").MediaType, Is.EqualTo(OdfMediaTypes.Presentation));
            Assert.That(FindManifestEntry(manifest, "Object2/mimetype").MediaType, Is.EqualTo("text/plain"));
            Assert.That(FindManifestEntry(stored, "Object2/").MediaType, Is.EqualTo(OdfMediaTypes.Presentation));
        });
    }

    [Test]
    public void ReplacingPackageMimetypeUpdatesTheRootManifestEntry()
    {
        string manifestXml = ManifestStart()
            + FileEntry(OdfPartNames.Root, OdfMediaTypes.Text)
            + "</m:manifest>";
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreatePackage(
            manifestXml,
            [],
            []));

        OdfEditResult result = index.ReplaceEntry(
            OdfPartNames.Mimetype,
            Encoding.UTF8.GetBytes(OdfMediaTypes.Spreadsheet));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(FindManifestEntry(index.Manifest!, OdfPartNames.Root).MediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
            Assert.That(FindManifestEntry(ReadStoredManifest(index), OdfPartNames.Root).MediaType, Is.EqualTo(OdfMediaTypes.Spreadsheet));
        });
    }

    static byte[] CreatePackage(
        string manifestXml,
        string[] paths,
        string[] contents)
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
            int index = 0;
            while (index < paths.Length)
            {
                WriteEntry(
                    archive,
                    paths[index],
                    contents[index],
                    CompressionLevel.SmallestSize);
                index++;
            }
        }
        return output.ToArray();
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

    static OdfManifest ReadStoredManifest(ZipEntryIndex index)
    {
        using Stream input = index.OpenEntry(OdfPartNames.Manifest);
        using StreamReader reader = new StreamReader(input, Encoding.UTF8);
        return ManifestParser.ParseText(reader.ReadToEnd());
    }

    static OdfManifestEntry FindManifestEntry(OdfManifest manifest, string path)
    {
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected manifest entry '{path}'.");
        return new OdfManifestEntry();
    }

    static bool HasManifestEntry(OdfManifest manifest, string path)
    {
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == path)
            {
                return true;
            }
        }
        return false;
    }

    static string[] ManifestPaths(OdfManifest manifest)
    {
        string[] result = new string[manifest.Entries.Length];
        int index = 0;
        while (index < manifest.Entries.Length)
        {
            result[index] = manifest.Entries[index].FullPath;
            index++;
        }
        return result;
    }

    static string ManifestStart()
    {
        return "<m:manifest xmlns:m='"
            + OdfNamespaces.Manifest
            + "' xmlns:ext='urn:extension' m:version='1.4'>";
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
