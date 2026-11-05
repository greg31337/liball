using System.IO.Compression;
using System.Text;
using uspto;

namespace uspto.Tests;

public class ZipExtractionOperationTests
{
    [Test]
    public void ExtractsOnlySelectedEntriesIntoCanonicalDestinations()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath,
                ("folder/", Array.Empty<byte>(), 0, CompressionLevel.Optimal),
                ("./folder/keep.txt", Encoding.ASCII.GetBytes("keep"), 0, CompressionLevel.Optimal),
                ("folder/skip.txt", Encoding.ASCII.GetBytes("skip"), 0, CompressionLevel.Optimal));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.ExtractAll = false;
            options.SelectedEntries = new[] { "folder\\keep.txt" };

            ExtractionResult result = ZipExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Zip));
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.FailureCode, Is.Null);
                Assert.That(result.BytesWritten, Is.EqualTo(4L));
                Assert.That(File.ReadAllText(Path.Combine(destination, "folder", "keep.txt")), Is.EqualTo("keep"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "skip.txt")), Is.False);
                Assert.That(result.Entries.Single(item => item.Entry.Name == "folder/keep.txt").IsExtracted, Is.True);
                Assert.That(result.Entries.Single(item => item.Entry.Name == "folder/skip.txt").IsSkipped, Is.True);
                Assert.That(result.SafetyLimits, Has.Length.EqualTo(4));
                Assert.That(result.SafetyLimits.All(item => item.IsAccepted), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TraversalEntryCannotEscapeDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");
        string escapedPath = Path.Combine(directory, "escaped.txt");

        try
        {
            CreateZip(archivePath, ("../escaped.txt", Encoding.ASCII.GetBytes("bad"), 0, CompressionLevel.Optimal));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_PATH_TRAVERSAL"));
                Assert.That(result.Entries.Single().FailureCode, Is.EqualTo("ARCHIVE_PATH_TRAVERSAL"));
                Assert.That(File.Exists(escapedPath), Is.False);
                Assert.That(Directory.GetFiles(destination, "*.partial", SearchOption.AllDirectories), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AbsoluteEntryCannotEscapeDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("/absolute.txt", Encoding.ASCII.GetBytes("bad"), 0, CompressionLevel.NoCompression));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_PATH_TRAVERSAL"));
                Assert.That(result.Entries.Single().FailureCode, Is.EqualTo("ARCHIVE_PATH_TRAVERSAL"));
                Assert.That(File.Exists(Path.Combine(destination, "absolute.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReservedDeviceNameIsRejectedPortably()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("folder/CON.txt", Encoding.ASCII.GetBytes("bad"), 0, CompressionLevel.Optimal));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_DEVICE_NAME_REJECTED"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "CON.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DuplicateCanonicalNamesAreRejectedByDefault()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath,
                ("folder/data.txt", Encoding.ASCII.GetBytes("first"), 0, CompressionLevel.Optimal),
                ("folder\\data.txt", Encoding.ASCII.GetBytes("second"), 0, CompressionLevel.Optimal));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_DUPLICATE_ENTRY"));
                Assert.That(result.Entries[0].IsExtracted, Is.True);
                Assert.That(result.Entries[1].FailureCode, Is.EqualTo("ARCHIVE_DUPLICATE_ENTRY"));
                Assert.That(File.ReadAllText(Path.Combine(destination, "folder", "data.txt")), Is.EqualTo("first"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AllowedDuplicateCanonicalNamesKeepFirstEntryAndSkipLaterEntries()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath,
                ("folder/data.txt", Encoding.ASCII.GetBytes("first"), 0, CompressionLevel.Optimal),
                ("folder\\data.txt", Encoding.ASCII.GetBytes("second"), 0, CompressionLevel.Optimal));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Limits.RejectDuplicateNames = false;

            ExtractionResult result = ZipExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries[0].IsExtracted, Is.True);
                Assert.That(result.Entries[1].IsSkipped, Is.True);
                Assert.That(File.ReadAllText(Path.Combine(destination, "folder", "data.txt")), Is.EqualTo("first"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SymbolicLinkEntryIsReportedAndNotMaterialized()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("folder/link", Encoding.ASCII.GetBytes("target.txt"), -1610612736, CompressionLevel.NoCompression));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_LINK_REJECTED"));
                Assert.That(result.Entries.Single().Entry.IsLink, Is.True);
                Assert.That(File.Exists(Path.Combine(destination, "folder", "link")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SpecialUnixEntryIsReportedAndNotMaterialized()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("folder/device", Array.Empty<byte>(), 536870912, CompressionLevel.NoCompression));

            ExtractionResult result = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_SPECIAL_ENTRY_REJECTED"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "device")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void EntryCountLimitStopsBeforeLaterFilesAreCreated()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath,
                ("one.txt", Encoding.ASCII.GetBytes("one"), 0, CompressionLevel.NoCompression),
                ("two.txt", Encoding.ASCII.GetBytes("two"), 0, CompressionLevel.NoCompression));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Limits.MaximumEntries = 1;

            ExtractionResult result = ZipExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_LIMIT_EXCEEDED"));
                Assert.That(result.SafetyLimits.Single(item => item.LimitName == "MaximumEntries").IsAccepted, Is.False);
                Assert.That(File.Exists(Path.Combine(destination, "one.txt")), Is.True);
                Assert.That(File.Exists(Path.Combine(destination, "two.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void EntrySizeAndTotalSizeLimitsUseDeclaredUncompressedSizes()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "entry-limit");

        try
        {
            CreateZip(archivePath,
                ("one.txt", Encoding.ASCII.GetBytes("12345"), 0, CompressionLevel.NoCompression),
                ("two.txt", Encoding.ASCII.GetBytes("67890"), 0, CompressionLevel.NoCompression));
            ExtractionOptions entryOptions = CreateOptions(archivePath, destination);
            entryOptions.Limits.MaximumEntryBytes = 4L;

            ExtractionResult entryResult = ZipExtractionOperation.Extract(entryOptions);

            ExtractionOptions totalOptions = CreateOptions(archivePath, Path.Combine(directory, "total-limit"));
            totalOptions.Limits.MaximumTotalBytes = 9L;
            ExtractionResult totalResult = ZipExtractionOperation.Extract(totalOptions);

            Assert.Multiple(() =>
            {
                Assert.That(entryResult.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED"));
                Assert.That(entryResult.SafetyLimits.Single(item => item.LimitName == "MaximumEntryBytes").IsAccepted, Is.False);
                Assert.That(File.Exists(Path.Combine(destination, "one.txt")), Is.False);
                Assert.That(totalResult.FailureCode, Is.EqualTo("ARCHIVE_TOTAL_SIZE_LIMIT_EXCEEDED"));
                Assert.That(totalResult.SafetyLimits.Single(item => item.LimitName == "MaximumTotalBytes").IsAccepted, Is.False);
                Assert.That(File.Exists(Path.Combine(directory, "total-limit", "one.txt")), Is.True);
                Assert.That(File.Exists(Path.Combine(directory, "total-limit", "two.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CompressionRatioLimitRejectsHighlyCompressedEntry()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("zeros.bin", new byte[32768], 0, CompressionLevel.SmallestSize));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Limits.MaximumCompressionRatio = 2;

            ExtractionResult result = ZipExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_COMPRESSION_RATIO_EXCEEDED"));
                Assert.That(result.SafetyLimits.Single(item => item.LimitName == "MaximumCompressionRatio").IsAccepted, Is.False);
                Assert.That(File.Exists(Path.Combine(destination, "zeros.bin")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExistingFileIsPreservedUnlessOverwriteIsEnabled()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");
        string destinationFile = Path.Combine(destination, "data.txt");

        try
        {
            Directory.CreateDirectory(destination);
            File.WriteAllText(destinationFile, "old");
            CreateZip(archivePath, ("data.txt", Encoding.ASCII.GetBytes("new"), 0, CompressionLevel.NoCompression));

            ExtractionResult preserved = ZipExtractionOperation.Extract(CreateOptions(archivePath, destination));
            string preservedText = File.ReadAllText(destinationFile);
            ExtractionOptions overwriteOptions = CreateOptions(archivePath, destination);
            overwriteOptions.OverwriteExisting = true;
            ExtractionResult overwritten = ZipExtractionOperation.Extract(overwriteOptions);

            Assert.Multiple(() =>
            {
                Assert.That(preserved.FailureCode, Is.EqualTo("ARCHIVE_DESTINATION_EXISTS"));
                Assert.That(preservedText, Is.EqualTo("old"));
                Assert.That(overwritten.IsComplete, Is.True);
                Assert.That(File.ReadAllText(destinationFile), Is.EqualTo("new"));
                Assert.That(Directory.GetFiles(destination, "*.partial", SearchOption.AllDirectories), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledExtractionReturnsWithoutCreatingDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("data.txt", Encoding.ASCII.GetBytes("data"), 0, CompressionLevel.NoCompression));
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Context.CancellationToken = cancellation.Token;

            ExtractionResult result = ZipExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsCancelled, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
                Assert.That(Directory.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationDuringExtractionStopsBeforeAllEntriesAndRemovesPartialFiles()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            (string Name, byte[] Content, int ExternalAttributes, CompressionLevel Compression)[] entries =
                new (string Name, byte[] Content, int ExternalAttributes, CompressionLevel Compression)[1000];
            for (int index = 0; index < entries.Length; index++)
            {
                entries[index] = ("entry-" + index.ToString("D4") + ".bin", new byte[1024], 0, CompressionLevel.NoCompression);
            }

            CreateZip(archivePath, entries);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Context.CancellationToken = cancellation.Token;
            Task<ExtractionResult> extraction = Task.Run(() => ZipExtractionOperation.Extract(options));

            Assert.That(SpinWait.SpinUntil(() => Directory.Exists(destination), 5000), Is.True);
            Assert.That(extraction.IsCompleted, Is.False);
            cancellation.Cancel();
            ExtractionResult result = extraction.GetAwaiter().GetResult();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsCancelled, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
                Assert.That(result.Entries.Length, Is.LessThan(entries.Length));
                Assert.That(Directory.GetFiles(destination, "*.partial", SearchOption.AllDirectories), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void UncommittedAtomicFileIsRemovedOnDispose()
    {
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "data.txt");
        string temporary;

        try
        {
            using (AtomicArchiveExtractionFile atomic = new AtomicArchiveExtractionFile(destination))
            {
                temporary = atomic.TemporaryPath;
                atomic.Output!.WriteByte(1);
                Assert.That(File.Exists(temporary), Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(File.Exists(temporary), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TruncatedZipDoesNotCommitOrLeavePartialFiles()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            CreateZip(archivePath, ("data.txt", Encoding.ASCII.GetBytes("content"), 0, CompressionLevel.NoCompression));
            byte[] archive = File.ReadAllBytes(archivePath);
            File.WriteAllBytes(archivePath, archive.Take(archive.Length - 8).ToArray());

            Assert.Throws<InvalidDataException>(() => ZipExtractionOperation.Extract(CreateOptions(archivePath, destination)));

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(Path.Combine(destination, "data.txt")), Is.False);
                Assert.That(Directory.Exists(destination) ? Directory.GetFiles(destination, "*.partial", SearchOption.AllDirectories) : Array.Empty<string>(), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static ExtractionOptions CreateOptions(string archivePath, string destination)
    {
        return new ExtractionOptions
        {
            ArchivePath = archivePath,
            DestinationDirectory = destination,
            Context = new RequestContext()
        };
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-zip-extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CreateZip(string archivePath, params (string Name, byte[] Content, int ExternalAttributes, CompressionLevel Compression)[] entries)
    {
        using FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, false);
        foreach ((string name, byte[] content, int externalAttributes, CompressionLevel compression) in entries)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, compression);
            entry.ExternalAttributes = externalAttributes;
            if (content.Length != 0)
            {
                using Stream destination = entry.Open();
                destination.Write(content);
            }
        }
    }
}
