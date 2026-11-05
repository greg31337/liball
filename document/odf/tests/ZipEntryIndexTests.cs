using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class ZipEntryIndexTests
{
    [Test]
    public void EnumeratesInSourceOrderAndOpensIndependentReadOnlyStreams()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateArchive());

        OdfPackageEntry[] entries = index.EnumerateEntries();
        entries[0].Path = "changed-by-caller";
        using Stream first = index.OpenEntry("folder/original.txt");
        using Stream second = index.OpenEntry("folder/original.txt");
        string firstText = ReadText(first);
        string secondText = ReadText(second);

        Assert.Multiple(() =>
        {
            Assert.That(index.EnumerateEntries(), Has.Length.EqualTo(4));
            Assert.That(index.EnumerateEntries()[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(index.EnumerateEntries()[1].Path, Is.EqualTo("folder/"));
            Assert.That(index.EnumerateEntries()[2].Path, Is.EqualTo("folder/original.txt"));
            Assert.That(index.EnumerateEntries()[3].Path, Is.EqualTo(OdfPartNames.Content));
            Assert.That(firstText, Is.EqualTo("original"));
            Assert.That(secondText, Is.EqualTo("original"));
            Assert.That(first.CanWrite, Is.False);
            Assert.That(second.CanWrite, Is.False);
        });
        Assert.Throws<OdfPackageException>(() => index.OpenEntry("missing.bin"));
    }

    [Test]
    public void ExtractsFilesAndDirectoryEntriesToExplicitDestinations()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateArchive());
        string root = Path.Combine(
            Path.GetTempPath(),
            "odf-entry-index-" + Guid.NewGuid().ToString("N"));
        string filePath = Path.Combine(root, "original.txt");
        string directoryPath = Path.Combine(root, "empty-directory");
        Directory.CreateDirectory(root);
        try
        {
            OdfEditResult fileResult = index.ExtractEntry(
                "folder/original.txt",
                filePath);
            OdfEditResult directoryResult = index.ExtractEntry(
                "folder/",
                directoryPath);

            Assert.Multiple(() =>
            {
                Assert.That(fileResult.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(fileResult.Changed, Is.False);
                Assert.That(fileResult.AffectedCount, Is.EqualTo(1));
                Assert.That(File.ReadAllText(filePath), Is.EqualTo("original"));
                Assert.That(directoryResult.Status, Is.EqualTo(OdfEditStatus.Success));
                Assert.That(Directory.Exists(directoryPath), Is.True);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void AddsAndReplacesEntriesWithDefensiveContentCopies()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateArchive());
        byte[] addedData = Encoding.UTF8.GetBytes("one");

        OdfEditResult added = index.AddEntry(
            "Pictures/new.bin",
            addedData,
            OdfCompressionMethod.Deflated);
        addedData[0] = (byte)'X';
        OdfEditResult duplicate = index.AddEntry(
            "Pictures/new.bin",
            [],
            OdfCompressionMethod.Stored);
        OdfEditResult replaced = index.ReplaceEntry(
            "Pictures/new.bin",
            Encoding.UTF8.GetBytes("two"));
        OdfEditResult unchanged = index.ReplaceEntry(
            "Pictures/new.bin",
            Encoding.UTF8.GetBytes("two"));
        OdfEditResult directory = index.AddEntry(
            "Pictures/empty/",
            [],
            OdfCompressionMethod.Stored);
        OdfEditResult invalidDirectory = index.ReplaceEntry(
            "Pictures/empty/",
            [1]);
        OdfPackageEntry addedEntry = FindEntry(index, "Pictures/new.bin");

        Assert.Multiple(() =>
        {
            Assert.That(added.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(added.Changed, Is.True);
            Assert.That(duplicate.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(replaced.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(ReadEntryText(index, "Pictures/new.bin"), Is.EqualTo("two"));
            Assert.That(unchanged.Status, Is.EqualTo(OdfEditStatus.NoChange));
            Assert.That(directory.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(FindEntry(index, "Pictures/empty/").IsDirectory, Is.True);
            Assert.That(invalidDirectory.Status, Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(addedEntry.ExpandedSize, Is.EqualTo(3));
            Assert.That(addedEntry.HasValidCrc, Is.True);
            Assert.That(addedEntry.CompressionMethod, Is.EqualTo(OdfCompressionMethod.Deflated));
        });
    }

    [Test]
    public void RenameMoveCopyAndRemoveRetainContentAndStableOrdering()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateArchive());

        OdfEditResult renamed = index.RenameEntry(
            "folder/original.txt",
            "renamed.txt");
        OdfEditResult moved = index.MoveEntry(
            "folder/renamed.txt",
            "Archive/renamed.txt");
        OdfEditResult copied = index.CopyEntry(
            "Archive/renamed.txt",
            "Archive/copy.txt");
        OdfEditResult removed = index.RemoveEntry("Archive/renamed.txt");
        OdfPackageEntry[] entries = index.EnumerateEntries();

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(moved.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(copied.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(removed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(entries, Has.Length.EqualTo(4));
            Assert.That(entries[0].Path, Is.EqualTo(OdfPartNames.Mimetype));
            Assert.That(entries[1].Path, Is.EqualTo("folder/"));
            Assert.That(entries[2].Path, Is.EqualTo(OdfPartNames.Content));
            Assert.That(entries[3].Path, Is.EqualTo("Archive/copy.txt"));
            Assert.That(ReadEntryText(index, "Archive/copy.txt"), Is.EqualTo("original"));
        });
        Assert.Throws<OdfPackageException>(() => index.OpenEntry("Archive/renamed.txt"));
    }

    [Test]
    public void InvalidPathsCollisionsAndMissingSourcesDoNotMutateTheIndex()
    {
        ZipEntryIndex index = ZipEntryIndex.CreateBytes(CreateArchive());
        string[] originalPaths = EntryPaths(index);

        OdfEditResult traversal = index.AddEntry(
            "../outside.bin",
            [],
            OdfCompressionMethod.Stored);
        OdfEditResult collision = index.MoveEntry(
            OdfPartNames.Content,
            "folder/original.txt");
        OdfEditResult invalidRename = index.RenameEntry(
            "folder/original.txt",
            "nested/name.txt");
        OdfEditResult missingCopy = index.CopyEntry(
            "missing.bin",
            "copy.bin");
        OdfEditResult missingRemove = index.RemoveEntry("missing.bin");

        Assert.Multiple(() =>
        {
            Assert.That(traversal.Status, Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(collision.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(invalidRename.Status, Is.EqualTo(OdfEditStatus.ValidationFailed));
            Assert.That(missingCopy.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(missingRemove.Status, Is.EqualTo(OdfEditStatus.Conflict));
            Assert.That(EntryPaths(index), Is.EqualTo(originalPaths));
        });
    }

    [Test]
    public void StreamLoadingHonorsInputOwnershipAndOpenedEntriesAreSnapshots()
    {
        byte[] package = CreateArchive();
        TrackingInputStream keptInput = new TrackingInputStream(package);
        OdfLoadOptions keptOptions = new OdfLoadOptions();
        keptOptions.LeaveInputOpen = true;
        ZipEntryIndex keptIndex = ZipEntryIndex.CreateStream(keptInput, keptOptions);
        Stream openedEntry = keptIndex.OpenEntry("folder/original.txt");

        keptIndex.ReplaceEntry(
            "folder/original.txt",
            Encoding.UTF8.GetBytes("replacement"));
        string openedText = ReadText(openedEntry);
        openedEntry.Dispose();
        string currentText = ReadEntryText(keptIndex, "folder/original.txt");

        TrackingInputStream closedInput = new TrackingInputStream(package);
        OdfLoadOptions closedOptions = new OdfLoadOptions();
        closedOptions.LeaveInputOpen = false;
        ZipEntryIndex.CreateStream(closedInput, closedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(keptInput.WasDisposed, Is.False);
            Assert.That(openedText, Is.EqualTo("original"));
            Assert.That(currentText, Is.EqualTo("replacement"));
            Assert.That(closedInput.WasDisposed, Is.True);
        });
        keptInput.Dispose();
    }

    static byte[] CreateArchive()
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                OdfMediaTypes.Text,
                CompressionLevel.NoCompression);
            archive.CreateEntry("folder/", CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                "folder/original.txt",
                "original",
                CompressionLevel.SmallestSize);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                "<office:document-content/>",
                CompressionLevel.SmallestSize);
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

    static string ReadText(Stream input)
    {
        using StreamReader reader = new StreamReader(input, Encoding.UTF8, false, 1024, true);
        return reader.ReadToEnd();
    }

    static string ReadEntryText(ZipEntryIndex index, string path)
    {
        using Stream input = index.OpenEntry(path);
        return ReadText(input);
    }

    static OdfPackageEntry FindEntry(ZipEntryIndex index, string path)
    {
        foreach (OdfPackageEntry entry in index.EnumerateEntries())
        {
            if (entry.Path == path)
            {
                return entry;
            }
        }
        Assert.Fail($"Expected package entry '{path}'.");
        return new OdfPackageEntry();
    }

    static string[] EntryPaths(ZipEntryIndex index)
    {
        OdfPackageEntry[] entries = index.EnumerateEntries();
        string[] result = new string[entries.Length];
        int itemIndex = 0;
        while (itemIndex < entries.Length)
        {
            result[itemIndex] = entries[itemIndex].Path;
            itemIndex++;
        }
        return result;
    }
}

public class TrackingInputStream : MemoryStream
{
    public bool WasDisposed;

    public TrackingInputStream(byte[] data)
        : base(data, false)
    {
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}
