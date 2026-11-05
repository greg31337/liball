using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkFileDownloadOperationTests
{
    [Test]
    public void KnownLengthDownloadStreamsBytesReportsProgressAndLeavesCallerStreamOpen()
    {
        byte[] body = Encoding.ASCII.GetBytes("bulk-content");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(body, "application/x-custom-bulk", "response.bin"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 4);
        RecordingDownloadProgress progress = new RecordingDownloadProgress();
        using MemoryStream output = new MemoryStream();
        DownloadRequest request = new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/signed-download"),
            CatalogFileName = "catalog-data.tar.gz",
            Output = output,
            Progress = progress
        };

        DownloadResult result = operation.Download(request);
        output.WriteByte(1);

        Assert.Multiple(() =>
        {
            Assert.That(output.ToArray().Take(body.Length), Is.EqualTo(body));
            Assert.That(output.Length, Is.EqualTo(body.Length + 1));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
            Assert.That(result.Progress.TotalBytes, Is.EqualTo(body.Length));
            Assert.That(result.Progress.HasTotalBytes, Is.True);
            Assert.That(result.Content.FileName, Is.EqualTo("catalog-data.tar.gz"));
            Assert.That(result.Content.MediaType, Is.EqualTo("application/x-custom-bulk"));
            Assert.That(progress.Reports.First().State, Is.EqualTo(DownloadState.Downloading));
            Assert.That(progress.Reports.First().BytesTransferred, Is.Zero);
            Assert.That(progress.Reports.Last().State, Is.EqualTo(DownloadState.Completed));
            Assert.That(progress.Reports.Select(item => item.BytesTransferred), Is.Ordered);
        });
    }

    [Test]
    public void UnknownLengthDownloadReportsAnUnknownTotal()
    {
        byte[] body = Encoding.ASCII.GetBytes("unknown-length");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => StreamingResponse(new CountingReadStream(body, 3), 0L, false, "unknown.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);
        RecordingDownloadProgress progress = new RecordingDownloadProgress();
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            Output = output,
            Progress = progress
        });

        Assert.Multiple(() =>
        {
            Assert.That(output.ToArray(), Is.EqualTo(body));
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.HasContentLength, Is.False);
            Assert.That(result.Progress.HasTotalBytes, Is.False);
            Assert.That(progress.Reports.All(item => !item.HasTotalBytes), Is.True);
            Assert.That(result.Content.FileName, Is.EqualTo("unknown.zip"));
        });
    }

    [Test]
    public void ExpectedSizeSuppliesProgressTotalWhenResponseLengthIsUnknown()
    {
        byte[] body = Encoding.ASCII.GetBytes("expected");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => StreamingResponse(new MemoryStream(body), 0L, false, "expected.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);
        RecordingDownloadProgress progress = new RecordingDownloadProgress();
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            Output = output,
            ExpectedSize = body.Length,
            HasExpectedSize = true,
            Progress = progress
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Progress.TotalBytes, Is.EqualTo(body.Length));
            Assert.That(result.Progress.HasTotalBytes, Is.True);
            Assert.That(progress.Reports.All(item => item.HasTotalBytes), Is.True);
        });
    }

    [Test]
    public void ResponseMetadataSuppliesFilenameLengthFormatAndValidators()
    {
        byte[] body = Encoding.ASCII.GetBytes("metadata");
        DateTimeOffset modified = new DateTimeOffset(2026, 9, 7, 12, 30, 0, TimeSpan.Zero);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) =>
        {
            HttpResponseMessage response = BinaryResponse(body, UsptoMediaTypes.Zip, "server-data.zip");
            response.Headers.ETag = new EntityTagHeaderValue("\"metadata-v1\"");
            response.Headers.AcceptRanges.Add("bytes");
            response.Content.Headers.LastModified = modified;
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            Output = output
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Content.FileName, Is.EqualTo("server-data.zip"));
            Assert.That(result.Content.MediaType, Is.EqualTo(UsptoMediaTypes.Zip));
            Assert.That(result.Content.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(result.Content.ContentLength, Is.EqualTo(body.Length));
            Assert.That(result.Content.HasContentLength, Is.True);
            Assert.That(result.Content.ETag, Is.EqualTo("\"metadata-v1\""));
            Assert.That(result.Content.LastModified, Is.EqualTo(modified));
            Assert.That(result.Content.HasLastModified, Is.True);
            Assert.That(result.Content.AcceptsByteRanges, Is.True);
            Assert.That(result.Progress.FileName, Is.EqualTo("server-data.zip"));
        });
    }

    [Test]
    public void StreamDownloadReturnsMatchingNormalizedChecksum()
    {
        byte[] body = Encoding.ASCII.GetBytes("checksummed stream");
        string expected = Convert.ToBase64String(SHA256.HashData(body));
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(body, UsptoMediaTypes.Binary, "catalog.bin"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            Output = output,
            ExpectedChecksums = new ChecksumExpectation[]
            {
                new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = expected }
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
            Assert.That(result.Checksums, Has.Length.EqualTo(1));
            Assert.That(result.Checksums[0].IsAvailable, Is.True);
            Assert.That(result.Checksums[0].IsComplete, Is.True);
            Assert.That(result.Checksums[0].IsMatch, Is.True);
            Assert.That(result.Checksums[0].ActualValue, Is.EqualTo(Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant()));
        });
    }

    [Test]
    public void ChecksumMismatchPreventsAtomicDiskCommit()
    {
        byte[] body = Encoding.ASCII.GetBytes("actual content");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "catalog.bin");
        string different = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("different")));
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(body, UsptoMediaTypes.Binary, "catalog.bin"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);

        try
        {
            DownloadResult result = operation.Download(new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/download"),
                DestinationPath = destination,
                ExpectedChecksums = new ChecksumExpectation[]
                {
                    new ChecksumExpectation { Algorithm = ChecksumAlgorithm.Sha256, Value = different }
                }
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("CHECKSUM_MISMATCH"));
                Assert.That(result.Checksums.Single().IsMatch, Is.False);
                Assert.That(result.Checksums.Single().IsComplete, Is.True);
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SuccessfulDiskDownloadCommitsAtomically()
    {
        byte[] body = Encoding.ASCII.GetBytes("replacement");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "catalog.zip");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(body, UsptoMediaTypes.Zip, null));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);

        try
        {
            DownloadResult result = operation.Download(new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/download"),
                CatalogFileName = "catalog.zip",
                DestinationPath = destination
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(result.FinalPath, Is.EqualTo(Path.GetFullPath(destination)));
                Assert.That(File.ReadAllBytes(destination), Is.EqualTo(body));
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExistingDestinationIsPreservedUnlessOverwriteIsEnabled()
    {
        byte[] body = Encoding.ASCII.GetBytes("replacement");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "catalog.zip");
        File.WriteAllText(destination, "original");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(body, UsptoMediaTypes.Zip, "catalog.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);

        try
        {
            DownloadRequest request = new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/download"),
                CatalogFileName = "catalog.zip",
                DestinationPath = destination
            };
            DownloadResult rejected = operation.Download(request);
            request.OverwriteExisting = true;
            DownloadResult replaced = operation.Download(request);

            Assert.Multiple(() =>
            {
                Assert.That(rejected.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(rejected.FailureCode, Is.EqualTo("DESTINATION_EXISTS"));
                Assert.That(handler.RequestUris, Has.Count.EqualTo(1));
                Assert.That(replaced.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllBytes(destination), Is.EqualTo(body));
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DeclaredLengthMismatchFailsWithoutCommittingDiskTarget()
    {
        byte[] body = Encoding.ASCII.GetBytes("short");
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "catalog.zip");
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => StreamingResponse(new MemoryStream(body), body.Length + 1L, true, "catalog.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 2);

        try
        {
            DownloadResult result = operation.Download(new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/download"),
                DestinationPath = destination,
                ExpectedChecksums = new ChecksumExpectation[]
                {
                    new ChecksumExpectation
                    {
                        Algorithm = ChecksumAlgorithm.Sha256,
                        Value = Convert.ToHexString(SHA256.HashData(body))
                    }
                }
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
                Assert.That(result.Progress.BytesTransferred, Is.EqualTo(body.Length));
                Assert.That(result.Checksums.Single().IsAvailable, Is.True);
                Assert.That(result.Checksums.Single().IsComplete, Is.False);
                Assert.That(result.Checksums.Single().IsMatch, Is.False);
                Assert.That(result.Checksums.Single().ActualValue, Is.Null);
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExpectedLengthMismatchIsRejectedBeforeReadingTheBody()
    {
        CountingReadStream source = new CountingReadStream(Encoding.ASCII.GetBytes("12345"), 2);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => StreamingResponse(source, 5L, true, "catalog.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 2);
        using MemoryStream output = new MemoryStream();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            Output = output,
            ExpectedSize = 6L,
            HasExpectedSize = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
            Assert.That(result.FailureCode, Is.EqualTo("RESPONSE_LENGTH_MISMATCH"));
            Assert.That(source.ReadCount, Is.Zero);
            Assert.That(output.Length, Is.Zero);
        });
    }

    [Test]
    public void CancellationDuringDiskTransferDoesNotCommitOrLeaveAPartialFile()
    {
        string directory = CreateTemporaryDirectory();
        string destination = Path.Combine(directory, "catalog.zip");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelAfterReadStream source = new CancelAfterReadStream(Encoding.ASCII.GetBytes("cancelled-content"), cancellation);
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => StreamingResponse(source, 0L, false, "catalog.zip"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);

        try
        {
            Assert.Throws<OperationCanceledException>(() => operation.Download(new DownloadRequest
            {
                SourceUri = new Uri("https://api.uspto.gov/download"),
                DestinationPath = destination,
                Context = new RequestContext { CancellationToken = cancellation.Token }
            }));

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(Directory.GetFiles(directory, "*.partial"), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledDownloadReturnsCancelledWithoutNetworkAccess()
    {
        RecordingPatentDocumentDownloadHandler handler = new RecordingPatentDocumentDownloadHandler((call, request) => BinaryResponse(new byte[] { 1 }, UsptoMediaTypes.Binary, "catalog.bin"));
        using HttpClient client = new HttpClient(handler);
        BulkFileDownloadOperation operation = CreateOperation(client, 3);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using MemoryStream output = new MemoryStream();
        cancellation.Cancel();

        DownloadResult result = operation.Download(new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/download"),
            CatalogFileName = "catalog.bin",
            Output = output,
            Context = new RequestContext { CancellationToken = cancellation.Token }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(DownloadState.Cancelled));
            Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
            Assert.That(handler.RequestUris, Is.Empty);
        });
    }

    private static BulkFileDownloadOperation CreateOperation(HttpClient client, int bufferSize)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            HttpClient = client,
            OdpApiKey = "bulk-key",
            ConnectTimeoutMilliseconds = 5000,
            HeaderTimeoutMilliseconds = 5000,
            BodyTimeoutMilliseconds = 5000,
            Download = new DownloadPolicy
            {
                TimeoutMilliseconds = 5000,
                BufferSize = bufferSize,
                RedirectHopCount = 3
            }
        };
        DownloadHostPolicy hostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        return new BulkFileDownloadOperation(client, options, hostPolicy);
    }

    private static HttpResponseMessage BinaryResponse(byte[] body, string mediaType, string? fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        if (fileName != null)
        {
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        }

        return response;
    }

    private static HttpResponseMessage StreamingResponse(Stream source, long contentLength, bool hasContentLength, string fileName)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StreamingTestContent(source, UsptoMediaTypes.Binary);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        if (hasContentLength)
        {
            response.Content.Headers.ContentLength = contentLength;
        }

        return response;
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-bulk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

internal class CancelAfterReadStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;
    internal bool HasCancelled = false;

    internal CancelAfterReadStream(byte[] body, CancellationTokenSource cancellation)
        : base(body, false)
    {
        Cancellation = cancellation;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int read = base.Read(buffer, offset, count);
        if (!HasCancelled)
        {
            HasCancelled = true;
            Cancellation.Cancel();
        }

        return Task.FromResult(read);
    }
}
