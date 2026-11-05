using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class PackageContractTests
{
    [Test]
    public void PackageReaderContractCoversEveryInputAndEntryOperation()
    {
        byte[] data = CreatePackage();
        IOdfPackageReader reader = new OdfPackageReader();
        OdfPackage fromBytes = reader.ReadPackageBytes(
            data,
            new OdfLoadOptions());

        TrackingInputStream keptInput = new TrackingInputStream(data);
        OdfLoadOptions keptOptions = new OdfLoadOptions();
        keptOptions.LeaveInputOpen = true;
        OdfPackage fromKeptStream = reader.ReadPackageStream(
            keptInput,
            keptOptions);

        TrackingInputStream closedInput = new TrackingInputStream(data);
        OdfLoadOptions closedOptions = new OdfLoadOptions();
        closedOptions.LeaveInputOpen = false;
        OdfPackage fromClosedStream = reader.ReadPackageStream(
            closedInput,
            closedOptions);

        string packagePath = Path.GetTempFileName();
        string malformedPath = Path.GetTempFileName();
        string extractedPath = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(packagePath, data);
            File.WriteAllBytes(malformedPath, [1, 2, 3, 4]);
            OdfPackage fromPath = reader.ReadPackagePath(
                packagePath,
                new OdfLoadOptions());
            OdfPackage malformedBytes = reader.ReadPackageBytes(
                [1, 2, 3, 4],
                new OdfLoadOptions());
            using MemoryStream malformedInput = new MemoryStream([1, 2, 3, 4]);
            OdfPackage malformedStream = reader.ReadPackageStream(
                malformedInput,
                new OdfLoadOptions());
            OdfPackage malformedFile = reader.ReadPackagePath(
                malformedPath,
                new OdfLoadOptions());

            using Stream firstSnapshot = reader.OpenPackageEntry(
                fromPath,
                "alpha.txt");
            using Stream secondSnapshot = reader.OpenPackageEntry(
                fromPath,
                "alpha.txt");
            int firstByte = firstSnapshot.ReadByte();
            string secondText = ReadText(secondSnapshot);
            OdfEditResult extracted = reader.ExtractPackageEntry(
                fromPath,
                "alpha.txt",
                extractedPath);
            OdfEditResult missingExtraction = reader.ExtractPackageEntry(
                fromPath,
                "missing.txt",
                extractedPath);

            Assert.Multiple(() =>
            {
                AssertValidPackage(fromBytes);
                AssertValidPackage(fromKeptStream);
                AssertValidPackage(fromClosedStream);
                AssertValidPackage(fromPath);
                Assert.That(keptInput.WasDisposed, Is.False);
                Assert.That(keptInput.CanRead, Is.True);
                Assert.That(closedInput.WasDisposed, Is.True);
                Assert.That(malformedBytes.IsValid, Is.False);
                Assert.That(malformedStream.IsValid, Is.False);
                Assert.That(malformedFile.IsValid, Is.False);
                Assert.That(malformedBytes.Diagnostics, Is.Not.Empty);
                Assert.That(malformedStream.Diagnostics, Is.Not.Empty);
                Assert.That(malformedFile.Diagnostics, Is.Not.Empty);
                Assert.That(firstByte, Is.EqualTo((int)'a'));
                Assert.That(secondText, Is.EqualTo("alpha"));
                Assert.That(extracted.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(extracted.Changed, Is.False);
                Assert.That(File.ReadAllText(extractedPath), Is.EqualTo("alpha"));
                Assert.That(missingExtraction.Status,
                    Is.EqualTo(OdfEditStatus.Conflict));
                Assert.That(
                    () => reader.OpenPackageEntry(fromPath, "missing.txt"),
                    Throws.TypeOf<OdfPackageException>());
            });
        }
        finally
        {
            keptInput.Dispose();
            File.Delete(packagePath);
            File.Delete(malformedPath);
            File.Delete(extractedPath);
        }
    }

    [Test]
    public void PackageWriterContractMutatesEntriesAndReportsRoutineFailures()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        IOdfPackageWriter writer = new OdfPackageWriter();
        OdfPackage package = reader.ReadPackageBytes(
            CreatePackage(),
            new OdfLoadOptions());
        byte[] addedData = Encoding.UTF8.GetBytes("added");

        OdfEditResult added = writer.AddPackageEntry(
            package,
            "Folder/new.txt",
            addedData,
            OdfCompressionMethod.Deflated);
        addedData[0] = (byte)'X';
        OdfEditResult duplicate = writer.AddPackageEntry(
            package,
            "Folder/new.txt",
            [],
            OdfCompressionMethod.Stored);
        OdfEditResult invalidCompression = writer.AddPackageEntry(
            package,
            "invalid.bin",
            [],
            OdfCompressionMethod.Unsupported);
        OdfEditResult replaced = writer.ReplacePackageEntry(
            package,
            "alpha.txt",
            Encoding.UTF8.GetBytes("replacement"));
        OdfEditResult missingReplacement = writer.ReplacePackageEntry(
            package,
            "missing.txt",
            []);
        OdfEditResult renamed = writer.RenamePackageEntry(
            package,
            "Folder/new.txt",
            "renamed.txt");
        OdfEditResult invalidRename = writer.RenamePackageEntry(
            package,
            "Folder/renamed.txt",
            "nested/name.txt");
        OdfEditResult moved = writer.MovePackageEntry(
            package,
            "Folder/renamed.txt",
            "Archive/moved.txt");
        OdfEditResult copied = writer.CopyPackageEntry(
            package,
            "Archive/moved.txt",
            "Archive/copy.txt");
        OdfEditResult copyCollision = writer.CopyPackageEntry(
            package,
            "Archive/moved.txt",
            "Archive/copy.txt");
        OdfEditResult removed = writer.RemovePackageEntry(
            package,
            "Archive/copy.txt");
        OdfEditResult missingRemoval = writer.RemovePackageEntry(
            package,
            "Archive/copy.txt");

        using Stream addedEntry = reader.OpenPackageEntry(
            package,
            "Archive/moved.txt");
        using Stream replacedEntry = reader.OpenPackageEntry(
            package,
            "alpha.txt");

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(invalidCompression.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(missingReplacement.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(invalidRename.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copyCollision.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(missingRemoval.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(ReadText(addedEntry), Is.EqualTo("added"));
            Assert.That(ReadText(replacedEntry), Is.EqualTo("replacement"));
            Assert.That(HasEntry(package, "Archive/moved.txt"), Is.True);
            Assert.That(HasEntry(package, "Folder/new.txt"), Is.False);
            Assert.That(HasEntry(package, "Archive/copy.txt"), Is.False);
            Assert.That(HasManifestEntry(
                package.Manifest,
                "Archive/moved.txt"), Is.True);
        });
    }

    [Test]
    public void DeterministicPackageSaveMatchesAcrossBytesStreamsAndPaths()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        IOdfPackageWriter writer = new OdfPackageWriter();
        OdfPackage package = reader.ReadPackageBytes(
            CreatePackage(),
            new OdfLoadOptions());
        writer.AddPackageEntry(
            package,
            "z.txt",
            Encoding.UTF8.GetBytes("z"),
            OdfCompressionMethod.Deflated);
        writer.AddPackageEntry(
            package,
            "a.txt",
            Encoding.UTF8.GetBytes("a"),
            OdfCompressionMethod.Deflated);
        OdfSaveOptions deterministic = new OdfSaveOptions();
        deterministic.Deterministic = true;

        byte[] first = writer.WritePackageBytes(package, deterministic);
        byte[] repeated = writer.WritePackageBytes(package, deterministic);
        using MemoryStream keptOutput = new MemoryStream();
        writer.WritePackageStream(package, keptOutput, deterministic);
        byte[] streamBytes = keptOutput.ToArray();

        TrackingOutputStream closedOutput = new TrackingOutputStream();
        OdfSaveOptions closedOptions = new OdfSaveOptions();
        closedOptions.Deterministic = true;
        closedOptions.LeaveOutputOpen = false;
        writer.WritePackageStream(package, closedOutput, closedOptions);

        string path = Path.GetTempFileName();
        try
        {
            writer.WritePackagePath(package, path, deterministic);
            byte[] pathBytes = File.ReadAllBytes(path);
            OdfPackage reread = reader.ReadPackageBytes(
                first,
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(repeated));
                Assert.That(streamBytes, Is.EqualTo(first));
                Assert.That(pathBytes, Is.EqualTo(first));
                Assert.That(keptOutput.CanWrite, Is.True);
                Assert.That(closedOutput.WasDisposed, Is.True);
                Assert.That(reread.IsValid, Is.True);
                Assert.That(EntryPaths(reread), Is.EqualTo(new[]
                {
                    OdfPartNames.Mimetype,
                    OdfPartNames.Manifest,
                    "a.txt",
                    "alpha.txt",
                    "z.txt"
                }));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ManifestContractParsesWritesFindsAndCompletesEditLifecycle()
    {
        IOdfManifestService service = new OdfManifestService();
        OdfManifest manifest = service.ReadManifestBytes(
            Encoding.UTF8.GetBytes(CreateManifest()));
        OdfManifestEntry addedEntry = Entry(
            "Pictures/image.png",
            OdfMediaTypes.Png);
        OdfEditResult added = service.AddManifestEntry(
            manifest,
            addedEntry);
        OdfEditResult duplicate = service.AddManifestEntry(
            manifest,
            Entry("Pictures/image.png", OdfMediaTypes.Png));
        OdfEditResult invalid = service.AddManifestEntry(
            manifest,
            Entry("../escape.bin", OdfMediaTypes.OctetStream));
        OdfEditResult noChange = service.UpdateManifestEntry(
            manifest,
            "Pictures/image.png",
            addedEntry);
        OdfEditResult collision = service.UpdateManifestEntry(
            manifest,
            "Pictures/image.png",
            Entry(OdfPartNames.Root, OdfMediaTypes.Text));
        OdfEditResult missingUpdate = service.UpdateManifestEntry(
            manifest,
            "missing.bin",
            Entry("updated.bin", OdfMediaTypes.OctetStream));
        OdfEditResult updated = service.UpdateManifestEntry(
            manifest,
            "Pictures/image.png",
            Entry("Pictures/renamed.png", OdfMediaTypes.Png));
        OdfEditResult removed = service.RemoveManifestEntry(
            manifest,
            "Pictures/renamed.png");
        OdfEditResult missingRemoval = service.RemoveManifestEntry(
            manifest,
            "Pictures/renamed.png");

        OdfSaveOptions deterministic = new OdfSaveOptions();
        deterministic.Deterministic = true;
        byte[] first = service.WriteManifestBytes(manifest, deterministic);
        byte[] second = service.WriteManifestBytes(manifest, deterministic);
        OdfManifest reread = service.ReadManifestBytes(first);

        Assert.Multiple(() =>
        {
            Assert.That(manifest.IsValid, Is.True);
            Assert.That(service.FindManifestEntry(
                manifest,
                OdfPartNames.Root), Is.Not.Null);
            Assert.That(service.FindManifestEntry(
                manifest,
                "ALPHA.TXT"), Is.Null);
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(duplicate.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(invalid.Status,
                Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(noChange.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(collision.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(missingUpdate.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(updated.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(missingRemoval.Status,
                Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(reread.IsValid, Is.True);
            Assert.That(service.FindManifestEntry(
                reread,
                "Pictures/renamed.png"), Is.Null);
            Assert.That(
                () => service.ReadManifestBytes(
                    Encoding.UTF8.GetBytes("<manifest:manifest")),
                Throws.Exception);
        });
    }

    [Test]
    public void ManifestSynchronizationReconcilesCurrentPackageEntries()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        IOdfManifestService service = new OdfManifestService();
        OdfPackage package = reader.ReadPackageBytes(
            CreatePackage(),
            new OdfLoadOptions());
        OdfManifest manifest = package.Manifest!;
        service.RemoveManifestEntry(manifest, "alpha.txt");

        OdfEditResult synchronized = service.SynchronizePackageManifest(
            package);
        OdfEditResult unchanged = service.SynchronizePackageManifest(package);
        OdfPackage malformed = reader.ReadPackageBytes(
            [1, 2, 3, 4],
            new OdfLoadOptions());
        OdfEditResult unavailable = service.SynchronizePackageManifest(
            malformed);

        Assert.Multiple(() =>
        {
            Assert.That(synchronized.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(synchronized.Changed, Is.True);
            Assert.That(HasManifestEntry(package.Manifest, "alpha.txt"),
                Is.True);
            Assert.That(unchanged.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(unchanged.Changed, Is.False);
            Assert.That(unavailable.Status, Is.EqualTo(OdfEditStatus.Failed));
        });
    }

    static byte[] CreatePackage()
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
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
            + "<manifest:file-entry manifest:full-path='/' "
            + "manifest:media-type='" + OdfMediaTypes.Text + "'/>"
            + "<manifest:file-entry manifest:full-path='alpha.txt' "
            + "manifest:media-type='text/plain'/>"
            + "</manifest:manifest>";
    }

    static OdfManifestEntry Entry(string path, string mediaType)
    {
        return new OdfManifestEntry
        {
            FullPath = path,
            MediaType = mediaType
        };
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

    static void AssertValidPackage(OdfPackage package)
    {
        Assert.That(package.IsValid, Is.True);
        Assert.That(package.MediaType, Is.EqualTo(OdfMediaTypes.Text));
        Assert.That(package.Entries, Has.Length.EqualTo(3));
        Assert.That(package.Manifest, Is.Not.Null);
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

    static bool HasEntry(OdfPackage package, string path)
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

    static string[] EntryPaths(OdfPackage package)
    {
        string[] result = new string[package.Entries.Length];
        int index = 0;
        while (index < package.Entries.Length)
        {
            result[index] = package.Entries[index].Path;
            index = index + 1;
        }
        return result;
    }
}
