using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackageApiTests
{
    [Test]
    public void PackageReaderSupportsBytesStreamsPathsEntryAccessAndMalformedInput()
    {
        byte[] source = CreatePackage();
        IOdfPackageReader reader = new OdfPackageReader();
        OdfLoadOptions options = new OdfLoadOptions();
        OdfPackage fromBytes = reader.ReadPackageBytes(source, options);
        OdfPackage malformed = reader.ReadPackageBytes([1, 2, 3], options);

        TrackingInputStream input = new TrackingInputStream(source);
        OdfLoadOptions streamOptions = new OdfLoadOptions();
        streamOptions.LeaveInputOpen = false;
        OdfPackage fromStream = reader.ReadPackageStream(input, streamOptions);

        string packagePath = Path.GetTempFileName();
        string extractedPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(packagePath, source);
            OdfPackage fromPath = reader.ReadPackagePath(packagePath, options);
            using Stream entry = reader.OpenPackageEntry(fromPath, "alpha.txt");
            OdfEditResult extraction = reader.ExtractPackageEntry(
                fromPath,
                "alpha.txt",
                extractedPath);

            Assert.Multiple(() =>
            {
                Assert.That(fromBytes.IsValid, Is.True);
                Assert.That(fromBytes.MediaType, Is.EqualTo(OdfMediaTypes.Text));
                Assert.That(fromBytes.DocumentKind, Is.EqualTo(OdfDocumentKind.Text));
                Assert.That(fromBytes.Entries, Has.Length.EqualTo(3));
                Assert.That(fromBytes.Manifest, Is.Not.Null);
                Assert.That(malformed.IsValid, Is.False);
                Assert.That(malformed.Diagnostics, Is.Not.Empty);
                Assert.That(input.WasDisposed, Is.True);
                Assert.That(fromStream.Entries, Has.Length.EqualTo(3));
                Assert.That(ReadText(entry), Is.EqualTo("alpha"));
                Assert.That(extraction.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(File.ReadAllText(extractedPath), Is.EqualTo("alpha"));
            });
        }
        finally
        {
            File.Delete(packagePath);
            File.Delete(extractedPath);
        }
    }

    [Test]
    public void PackageWriterExposesMutationsAndAllSaveTargets()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        IOdfPackageWriter writer = new OdfPackageWriter();
        OdfPackage package = reader.ReadPackageBytes(
            CreatePackage(),
            new OdfLoadOptions());

        OdfEditResult added = writer.AddPackageEntry(
            package,
            "new.txt",
            Encoding.UTF8.GetBytes("new"),
            OdfCompressionMethod.Deflated);
        OdfEditResult replaced = writer.ReplacePackageEntry(
            package,
            "alpha.txt",
            Encoding.UTF8.GetBytes("replacement"));
        OdfEditResult renamed = writer.RenamePackageEntry(
            package,
            "new.txt",
            "renamed.txt");
        OdfEditResult moved = writer.MovePackageEntry(
            package,
            "renamed.txt",
            "folder/renamed.txt");
        OdfEditResult copied = writer.CopyPackageEntry(
            package,
            "folder/renamed.txt",
            "folder/copy.txt");
        OdfEditResult removed = writer.RemovePackageEntry(
            package,
            "folder/copy.txt");

        OdfSaveOptions deterministic = new OdfSaveOptions();
        deterministic.Deterministic = true;
        byte[] first = writer.WritePackageBytes(package, deterministic);
        byte[] second = writer.WritePackageBytes(package, deterministic);
        OdfPackage written = reader.ReadPackageBytes(first, new OdfLoadOptions());
        using Stream replacedEntry = reader.OpenPackageEntry(written, "alpha.txt");
        using Stream movedEntry = reader.OpenPackageEntry(
            written,
            "folder/renamed.txt");

        TrackingOutputStream output = new TrackingOutputStream();
        OdfSaveOptions streamOptions = new OdfSaveOptions();
        streamOptions.LeaveOutputOpen = false;
        writer.WritePackageStream(package, output, streamOptions);

        string outputPath = Path.GetTempFileName();
        try
        {
            writer.WritePackagePath(package, outputPath, deterministic);
            OdfPackage pathPackage = reader.ReadPackagePath(
                outputPath,
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(first, Is.EqualTo(second));
                Assert.That(ReadText(replacedEntry), Is.EqualTo("replacement"));
                Assert.That(ReadText(movedEntry), Is.EqualTo("new"));
                Assert.That(HasPackageEntry(package, "folder/renamed.txt"), Is.True);
                Assert.That(HasPackageEntry(package, "folder/copy.txt"), Is.False);
                Assert.That(HasManifestEntry(package.Manifest, "folder/renamed.txt"), Is.True);
                Assert.That(output.WasDisposed, Is.True);
                Assert.That(pathPackage.IsValid, Is.True);
            });
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Test]
    public void ManifestServiceParsesWritesEditsAndSynchronizesPackages()
    {
        IOdfManifestService service = new OdfManifestService();
        OdfManifest manifest = service.ReadManifestBytes(
            Encoding.UTF8.GetBytes(CreateManifest()));
        OdfManifestEntry addedEntry = new OdfManifestEntry();
        addedEntry.FullPath = "Pictures/image.png";
        addedEntry.MediaType = OdfMediaTypes.Png;
        OdfEditResult added = service.AddManifestEntry(manifest, addedEntry);
        OdfEditResult duplicate = service.AddManifestEntry(manifest, addedEntry);
        OdfManifestEntry updatedEntry = new OdfManifestEntry();
        updatedEntry.FullPath = "Pictures/renamed.png";
        updatedEntry.MediaType = OdfMediaTypes.Png;
        OdfEditResult updated = service.UpdateManifestEntry(
            manifest,
            "Pictures/image.png",
            updatedEntry);
        OdfEditResult removed = service.RemoveManifestEntry(
            manifest,
            "Pictures/renamed.png");
        OdfManifestEntry invalidEntry = new OdfManifestEntry();
        invalidEntry.FullPath = "../escape.bin";
        OdfEditResult invalid = service.AddManifestEntry(manifest, invalidEntry);
        byte[] xml = service.WriteManifestBytes(manifest, new OdfSaveOptions());
        OdfManifest reparsed = service.ReadManifestBytes(xml);

        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage package = reader.ReadPackageBytes(
            CreatePackage(),
            new OdfLoadOptions());
        OdfManifest packageManifest = package.Manifest ?? new OdfManifest();
        service.RemoveManifestEntry(packageManifest, "alpha.txt");
        OdfEditResult synchronized = service.SynchronizePackageManifest(package);

        Assert.Multiple(() =>
        {
            Assert.That(service.FindManifestEntry(manifest, OdfPartNames.Root), Is.Not.Null);
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(updated.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(invalid.Status, Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(reparsed.IsValid, Is.True);
            Assert.That(service.FindManifestEntry(reparsed, "Pictures/renamed.png"), Is.Null);
            Assert.That(synchronized.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(HasManifestEntry(package.Manifest, "alpha.txt"), Is.True);
        });
    }

    static byte[] CreatePackage()
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
                CreateManifest(),
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                "alpha.txt",
                "alpha",
                CompressionLevel.SmallestSize);
        }
        return output.ToArray();
    }

    static string CreateManifest()
    {
        return "<manifest:manifest xmlns:manifest='"
            + OdfNamespaces.Manifest
            + "' manifest:version='1.4'>"
            + "<manifest:file-entry manifest:full-path='/' manifest:media-type='"
            + OdfMediaTypes.Text
            + "'/><manifest:file-entry manifest:full-path='alpha.txt' manifest:media-type='text/plain'/>"
            + "</manifest:manifest>";
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        string content,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream output = entry.Open();
        byte[] data = Encoding.UTF8.GetBytes(content);
        output.Write(data, 0, data.Length);
    }

    static string ReadText(Stream input)
    {
        using StreamReader reader = new StreamReader(
            input,
            Encoding.UTF8,
            false,
            1024,
            true);
        return reader.ReadToEnd();
    }

    static bool HasPackageEntry(OdfPackage package, string path)
    {
        foreach (OdfPackageEntry entry in package.Entries)
        {
            if (entry.Path == path)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasManifestEntry(OdfManifest? manifest, string path)
    {
        if (manifest == null)
        {
            return false;
        }
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == path)
            {
                return true;
            }
        }
        return false;
    }
}
