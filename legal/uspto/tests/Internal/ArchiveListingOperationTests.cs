using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using uspto;

namespace uspto.Tests;

public class ArchiveListingOperationTests
{
    [Test]
    public void ZipSignatureListsNormalizedFilesDirectoriesAndLinks()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");

        try
        {
            CreateZip(archivePath, new string[] { "records/", "records\\data.xml", "records/link" }, new byte[][]
            {
                Array.Empty<byte>(),
                Encoding.UTF8.GetBytes("<record />"),
                Array.Empty<byte>()
            }, 2);

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Zip));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.Detection.IsSupported, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.FailureCode, Is.Null);
                Assert.That(result.Entries.Select(item => item.Name), Is.EqualTo(new[] { "records/", "records/data.xml", "records/link" }));
                Assert.That(result.Entries[0].IsDirectory, Is.True);
                Assert.That(result.Entries[1].HasCompressedSize, Is.True);
                Assert.That(result.Entries[1].UncompressedSize, Is.EqualTo(10L));
                Assert.That(result.Entries[1].HasLastModified, Is.True);
                Assert.That(result.Entries[2].IsLink, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SignatureTakesPrecedenceAndReportsExtensionDisagreement()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");

        try
        {
            CreateZip(archivePath, new string[] { "data.txt" }, new byte[][] { Encoding.ASCII.GetBytes("zip") }, -1);

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Zip));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.False);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries.Single().Name, Is.EqualTo("data.txt"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GzipListsItsSingleUncompressedPayload()
    {
        byte[] body = Encoding.UTF8.GetBytes("gzip payload");
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "records.json.gz");

        try
        {
            WriteGzip(archivePath, body);

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());
            ArchiveEntry entry = result.Entries.Single();

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Gzip));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(entry.Name, Is.EqualTo("records.json"));
                Assert.That(entry.HasCompressedSize, Is.True);
                Assert.That(entry.CompressedSize, Is.EqualTo(new FileInfo(archivePath).Length));
                Assert.That(entry.HasUncompressedSize, Is.True);
                Assert.That(entry.UncompressedSize, Is.EqualTo(body.LongLength));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TarListsRegularDirectoryAndSymbolicLinkEntries()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tar");

        try
        {
            File.WriteAllBytes(archivePath, CreateTarBytes());

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Tar));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries.Select(item => item.Name), Is.EqualTo(new[] { "folder/", "folder/data.txt", "folder/current" }));
                Assert.That(result.Entries[0].IsDirectory, Is.True);
                Assert.That(result.Entries[1].UncompressedSize, Is.EqualTo(8L));
                Assert.That(result.Entries[1].HasCompressedSize, Is.False);
                Assert.That(result.Entries[2].IsLink, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GzippedTarIsDetectedFromNestedSignatureAndListed()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.tgz");

        try
        {
            WriteGzip(archivePath, CreateTarBytes());

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.TarGzip));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Entries, Has.Length.EqualTo(3));
                Assert.That(result.Entries[1].Name, Is.EqualTo("folder/data.txt"));
                Assert.That(result.Entries[1].UncompressedSize, Is.EqualTo(8L));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RecognizedUnsupportedSignatureReturnsExplicitResult()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.bz2");

        try
        {
            File.WriteAllBytes(archivePath, new byte[] { 0x42, 0x5a, 0x68, 0x39, 0x00 });

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Unsupported));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.Detection.IsSupported, Is.False);
                Assert.That(result.Entries, Is.Empty);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_ARCHIVE_FORMAT"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("dataset.7z", "377ABCAF271C")]
    [TestCase("dataset.xz", "FD377A585A00")]
    [TestCase("dataset.rar", "526172211A0700")]
    [TestCase("dataset.zst", "28B52FFD")]
    public void EveryOtherRecognizedUnsupportedSignatureIsReported(string fileName, string signature)
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, fileName);

        try
        {
            File.WriteAllBytes(archivePath, Convert.FromHexString(signature));

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Unsupported));
                Assert.That(result.Detection.DetectedBySignature, Is.True);
                Assert.That(result.Detection.ExtensionMatches, Is.True);
                Assert.That(result.Detection.IsSupported, Is.False);
                Assert.That(result.Entries, Is.Empty);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_ARCHIVE_FORMAT"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void UnknownContentReturnsExplicitUnsupportedResult()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.bin");

        try
        {
            File.WriteAllText(archivePath, "not an archive");

            ArchiveListingResult result = ArchiveListingOperation.List(archivePath, new RequestContext());

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Unknown));
                Assert.That(result.Detection.DetectedBySignature, Is.False);
                Assert.That(result.Detection.IsSupported, Is.False);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_ARCHIVE_FORMAT"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExtensionCanIdentifyFormatWhenSignatureIsUnavailable()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "damaged.zip");

        try
        {
            File.WriteAllBytes(archivePath, new byte[] { 0x00, 0x01, 0x02 });

            CompressionDetectionResult result = ArchiveFormatDetector.Detect(archivePath);

            Assert.Multiple(() =>
            {
                Assert.That(result.Format, Is.EqualTo(CompressionFormat.Zip));
                Assert.That(result.DetectedBySignature, Is.False);
                Assert.That(result.ExtensionMatches, Is.False);
                Assert.That(result.IsSupported, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ListingStopsAtConfiguredEntryLimit()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");

        try
        {
            CreateZip(archivePath, new string[] { "one.txt", "two.txt" }, new byte[][]
            {
                Encoding.ASCII.GetBytes("one"),
                Encoding.ASCII.GetBytes("two")
            }, -1);
            ExtractionLimits limits = new ExtractionLimits();
            limits.MaximumEntries = 1;

            ArchiveListingResult result = ArchiveListingOperation.ListWithLimits(archivePath, new RequestContext(), limits);

            Assert.Multiple(() =>
            {
                Assert.That(result.Entries, Has.Length.EqualTo(1));
                Assert.That(result.Entries[0].Name, Is.EqualTo("one.txt"));
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_LIMIT_EXCEEDED"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ZipListingEnforcesEntrySizeAndCompressionRatioLimits()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");

        try
        {
            CreateZip(archivePath, new string[] { "zeros.bin" }, new byte[][] { new byte[32768] }, -1);
            ExtractionLimits sizeLimits = new ExtractionLimits();
            sizeLimits.MaximumEntryBytes = 32767L;
            ArchiveListingResult sizeResult = ArchiveListingOperation.ListWithLimits(archivePath, new RequestContext(), sizeLimits);
            ExtractionLimits ratioLimits = new ExtractionLimits();
            ratioLimits.MaximumCompressionRatio = 2;
            ArchiveListingResult ratioResult = ArchiveListingOperation.ListWithLimits(archivePath, new RequestContext(), ratioLimits);

            Assert.Multiple(() =>
            {
                Assert.That(sizeResult.Entries, Is.Empty);
                Assert.That(sizeResult.IsComplete, Is.False);
                Assert.That(sizeResult.FailureCode, Is.EqualTo("ARCHIVE_ENTRY_SIZE_LIMIT_EXCEEDED"));
                Assert.That(ratioResult.Entries, Is.Empty);
                Assert.That(ratioResult.IsComplete, Is.False);
                Assert.That(ratioResult.FailureCode, Is.EqualTo("ARCHIVE_COMPRESSION_RATIO_EXCEEDED"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledListingStopsBeforeArchiveInspection()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "dataset.zip");

        try
        {
            CreateZip(archivePath, new string[] { "data.txt" }, new byte[][] { Encoding.ASCII.GetBytes("data") }, -1);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => ArchiveListingOperation.List(archivePath, new RequestContext
            {
                CancellationToken = cancellation.Token
            }));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void CreateZip(string archivePath, string[] names, byte[][] contents, int linkIndex)
    {
        using FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, false);
        for (int index = 0; index < names.Length; index += 1)
        {
            ZipArchiveEntry entry = archive.CreateEntry(names[index]);
            if (index == linkIndex)
            {
                entry.ExternalAttributes = -1610612736;
            }

            if (contents[index].Length != 0)
            {
                using Stream destination = entry.Open();
                destination.Write(contents[index]);
            }
        }
    }

    private static byte[] CreateTarBytes()
    {
        using MemoryStream output = new MemoryStream();
        using (TarWriter writer = new TarWriter(output, true))
        {
            UstarTarEntry folder = new UstarTarEntry(TarEntryType.Directory, "./folder/");
            writer.WriteEntry(folder);

            UstarTarEntry file = new UstarTarEntry(TarEntryType.RegularFile, "./folder\\data.txt");
            file.DataStream = new MemoryStream(Encoding.ASCII.GetBytes("tar data"), false);
            writer.WriteEntry(file);

            UstarTarEntry link = new UstarTarEntry(TarEntryType.SymbolicLink, "./folder/current");
            link.LinkName = "data.txt";
            writer.WriteEntry(link);
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
