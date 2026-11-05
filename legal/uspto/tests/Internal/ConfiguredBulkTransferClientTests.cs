using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class ConfiguredBulkTransferClientTests
{
    [Test]
    public void LocalVerificationChecksSizeAndChecksumWithoutNetworkAccess()
    {
        byte[] body = Encoding.ASCII.GetBytes("locally verified bulk content");
        string digest = Convert.ToBase64String(SHA256.HashData(body));
        string different = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("different")));
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "dataset.bin");
        File.WriteAllBytes(path, body);
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(new UsptoClientOptions());

        try
        {
            DownloadResult matching = clients.BulkTransferClient!.VerifyBulkFile(new DownloadRequest
            {
                DestinationPath = path,
                ExpectedSize = body.Length,
                HasExpectedSize = true,
                ExpectedChecksums = new[]
                {
                    new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = digest }
                }
            });
            DownloadResult wrongSizeAndChecksum = clients.BulkTransferClient.VerifyBulkFile(new DownloadRequest
            {
                DestinationPath = path,
                ExpectedSize = body.Length + 1L,
                HasExpectedSize = true,
                ExpectedChecksums = new[]
                {
                    new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = different }
                }
            });

            Assert.Multiple(() =>
            {
                Assert.That(matching.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(matching.FinalPath, Is.EqualTo(Path.GetFullPath(path)));
                Assert.That(matching.Progress.BytesTransferred, Is.EqualTo(body.Length));
                Assert.That(matching.Progress.TotalBytes, Is.EqualTo(body.Length));
                Assert.That(matching.Checksums.Single().IsMatch, Is.True);
                Assert.That(wrongSizeAndChecksum.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(wrongSizeAndChecksum.FailureCode, Is.EqualTo("DOWNLOAD_SIZE_MISMATCH"));
                Assert.That(wrongSizeAndChecksum.Checksums.Single().IsComplete, Is.True);
                Assert.That(wrongSizeAndChecksum.Checksums.Single().IsMatch, Is.False);
            });
        }
        finally
        {
            clients.Lifetime!.Dispose();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PlainGzipExtractionReturnsExplicitUnsupportedResult()
    {
        string directory = CreateTemporaryDirectory();
        string archivePath = Path.Combine(directory, "records.json.gz");
        string destination = Path.Combine(directory, "extracted");
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(new UsptoClientOptions());

        try
        {
            using (FileStream output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (GZipStream gzip = new GZipStream(output, CompressionLevel.SmallestSize, false))
            {
                gzip.Write(Encoding.UTF8.GetBytes("{\"id\":1}"));
            }

            ExtractionResult result = clients.BulkTransferClient!.ExtractBulkArchive(new ExtractionOptions
            {
                ArchivePath = archivePath,
                DestinationDirectory = destination
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Detection.Format, Is.EqualTo(CompressionFormat.Gzip));
                Assert.That(result.Detection.IsSupported, Is.True);
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailureCode, Is.EqualTo("UNSUPPORTED_ARCHIVE_FORMAT"));
                Assert.That(Directory.Exists(destination), Is.False);
            });
        }
        finally
        {
            clients.Lifetime!.Dispose();
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-bulk-verification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
