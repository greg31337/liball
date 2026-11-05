using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TarExtractionOperationTests
{
    [Test]
    public void TarExtractsOnlySelectedRegularEntryIntoCanonicalDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath,
                ("folder/", TarEntryType.Directory, Array.Empty<byte>(), null),
                ("folder/skip.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("skip"), null),
                ("./folder/keep.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("keep"), null));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.ExtractAll = false;
            options.SelectedEntries = new[] { "folder\\keep.txt" };

            ExtractionResult result = TarExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Tar));
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.FailureCode, Is.Null);
                Assert.That(result.BytesWritten, Is.EqualTo(4L));
                Assert.That(File.ReadAllText(Path.Combine(destination, "folder", "keep.txt")), Is.EqualTo("keep"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "skip.txt")), Is.False);
                Assert.That(result.Entries.Single(item => item.Entry.Name == "folder/skip.txt").IsSkipped, Is.True);
                Assert.That(result.Entries.Single(item => item.Entry.Name == "folder/keep.txt").IsExtracted, Is.True);
                Assert.That(result.SafetyLimits.All(item => item.IsAccepted), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TarGzipExtractsDirectoryAndRegularFiles()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar.gz");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            byte[] tar = CreateTarBytes(
                ("records/", TarEntryType.Directory, Array.Empty<byte>(), null),
                ("records/data.xml", TarEntryType.RegularFile, Encoding.UTF8.GetBytes("<data />"), null));
            WriteGzip(archivePath, tar);

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.TarGzip));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries[0].Entry.IsDirectory, Is.True);
                Assert.That(result.Entries.All(item => item.IsExtracted), Is.True);
                Assert.That(File.ReadAllText(Path.Combine(destination, "records", "data.xml")), Is.EqualTo("<data />"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TraversalEntryCannotEscapeTarDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");
        string escapedPath = Path.Combine(directory, "escaped.txt");

        try
        {
            WriteTar(archivePath, ("../escaped.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("bad"), null));

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

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
    public void ReservedDeviceNameIsRejectedInTarPath()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath, ("folder/CON.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("bad"), null));

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_DEVICE_NAME_REJECTED"));
                Assert.That(result.Entries.Single().FailureCode, Is.EqualTo("ARCHIVE_DEVICE_NAME_REJECTED"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "CON.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TruncatedTarDoesNotCommitOrLeavePartialFiles()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath, ("data.bin", TarEntryType.RegularFile, new byte[200000], null));
            byte[] archive = File.ReadAllBytes(archivePath);
            File.WriteAllBytes(archivePath, archive.Take(1024).ToArray());

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_LENGTH_MISMATCH"));
                Assert.That(result.Entries.Single().FailureCode, Is.EqualTo("ARCHIVE_ENTRY_LENGTH_MISMATCH"));
                Assert.That(File.Exists(Path.Combine(destination, "data.bin")), Is.False);
                Assert.That(Directory.Exists(destination) ? Directory.GetFiles(destination, "*.partial", SearchOption.AllDirectories) : Array.Empty<string>(), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DuplicateCanonicalTarNamesFollowConfiguredPolicy()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string rejectedDestination = Path.Combine(directory, "rejected");
        string allowedDestination = Path.Combine(directory, "allowed");

        try
        {
            WriteTar(archivePath,
                ("folder/data.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("first"), null),
                ("folder\\data.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("second"), null));

            ExtractionResult rejected = TarExtractionOperation.Extract(CreateOptions(archivePath, rejectedDestination));
            ExtractionOptions allowedOptions = CreateOptions(archivePath, allowedDestination);
            allowedOptions.Limits.RejectDuplicateNames = false;
            ExtractionResult allowed = TarExtractionOperation.Extract(allowedOptions);

            Assert.Multiple(() =>
            {
                Assert.That(rejected.FailureCode, Is.EqualTo("ARCHIVE_DUPLICATE_ENTRY"));
                Assert.That(rejected.Entries[1].FailureCode, Is.EqualTo("ARCHIVE_DUPLICATE_ENTRY"));
                Assert.That(allowed.IsComplete, Is.True);
                Assert.That(allowed.Entries[1].IsSkipped, Is.True);
                Assert.That(File.ReadAllText(Path.Combine(allowedDestination, "folder", "data.txt")), Is.EqualTo("first"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(TarEntryType.SymbolicLink)]
    [TestCase(TarEntryType.HardLink)]
    public void TarLinksAreReportedAndNotMaterialized(TarEntryType entryType)
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath, ("folder/link", entryType, Array.Empty<byte>(), "target.txt"));

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

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
    public void TarFifoEntryIsRejectedAsDeviceContent()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath, ("folder/pipe", TarEntryType.Fifo, Array.Empty<byte>(), null));

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_DEVICE_ENTRY_REJECTED"));
                Assert.That(File.Exists(Path.Combine(destination, "folder", "pipe")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TarEntryCountLimitStopsBeforeLaterFileIsCreated()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath,
                ("one.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("one"), null),
                ("two.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("two"), null));
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Limits.MaximumEntries = 1;

            ExtractionResult result = TarExtractionOperation.Extract(options);

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
    public void TarEntryAndTotalSizeLimitsPreventOversizedWrites()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string entryDestination = Path.Combine(directory, "entry-limit");
        string totalDestination = Path.Combine(directory, "total-limit");

        try
        {
            WriteTar(archivePath,
                ("one.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("12345"), null),
                ("two.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("67890"), null));
            ExtractionOptions entryOptions = CreateOptions(archivePath, entryDestination);
            entryOptions.Limits.MaximumEntryBytes = 4L;
            ExtractionResult entryResult = TarExtractionOperation.Extract(entryOptions);
            ExtractionOptions totalOptions = CreateOptions(archivePath, totalDestination);
            totalOptions.Limits.MaximumTotalBytes = 9L;
            ExtractionResult totalResult = TarExtractionOperation.Extract(totalOptions);

            Assert.Multiple(() =>
            {
                Assert.That(entryResult.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED"));
                Assert.That(File.Exists(Path.Combine(entryDestination, "one.txt")), Is.False);
                Assert.That(totalResult.FailureCode, Is.EqualTo("ARCHIVE_TOTAL_SIZE_LIMIT_EXCEEDED"));
                Assert.That(File.Exists(Path.Combine(totalDestination, "one.txt")), Is.True);
                Assert.That(File.Exists(Path.Combine(totalDestination, "two.txt")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExistingTarDestinationIsPreservedUnlessOverwriteIsEnabled()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");
        string destinationFile = Path.Combine(destination, "data.txt");

        try
        {
            Directory.CreateDirectory(destination);
            File.WriteAllText(destinationFile, "old");
            WriteTar(archivePath, ("data.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("new"), null));

            ExtractionResult preserved = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));
            string preservedText = File.ReadAllText(destinationFile);
            ExtractionOptions overwriteOptions = CreateOptions(archivePath, destination);
            overwriteOptions.OverwriteExisting = true;
            ExtractionResult overwritten = TarExtractionOperation.Extract(overwriteOptions);

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
    public void PreCancelledTarExtractionDoesNotCreateDestination()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            WriteTar(archivePath, ("data.txt", TarEntryType.RegularFile, Encoding.ASCII.GetBytes("data"), null));
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            ExtractionOptions options = CreateOptions(archivePath, destination);
            options.Context.CancellationToken = cancellation.Token;

            ExtractionResult result = TarExtractionOperation.Extract(options);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsCancelled, Is.True);
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
    public void NonTarInputReturnsUnsupportedFormatResult()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");
        string destination = Path.Combine(directory, "extracted");

        try
        {
            using (FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, false))
            {
                archive.CreateEntry("data.txt");
            }

            ExtractionResult result = TarExtractionOperation.Extract(CreateOptions(archivePath, destination));

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Zip));
                Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_ARCHIVE_FORMAT"));
                Assert.That(Directory.Exists(destination), Is.False);
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
        string directory = Path.Combine(Path.GetTempPath(), "uspto-tar-extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteTar(string archivePath, params (string Name, TarEntryType Type, byte[] Content, string? LinkName)[] entries)
    {
        File.WriteAllBytes(archivePath, CreateTarBytes(entries));
    }

    private static byte[] CreateTarBytes(params (string Name, TarEntryType Type, byte[] Content, string? LinkName)[] entries)
    {
        using MemoryStream output = new MemoryStream();
        using (TarWriter writer = new TarWriter(output, true))
        {
            foreach ((string name, TarEntryType type, byte[] content, string? linkName) in entries)
            {
                UstarTarEntry entry = new UstarTarEntry(type, name);
                if (type == TarEntryType.RegularFile)
                {
                    entry.DataStream = new MemoryStream(content, false);
                }

                if (type == TarEntryType.SymbolicLink || type == TarEntryType.HardLink)
                {
                    entry.LinkName = linkName ?? "target";
                }

                writer.WriteEntry(entry);
            }
        }

        return output.ToArray();
    }

    private static void WriteGzip(string archivePath, byte[] content)
    {
        using FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using GZipStream gzip = new GZipStream(output, CompressionLevel.SmallestSize, false);
        gzip.Write(content);
    }
}
