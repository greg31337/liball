using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkFileResumeOperationTests
{
    [Test]
    public void StrongEtagResumeAppendsVerifiedRangeAndCommitsDestination()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        byte[] prefix = Encoding.ASCII.GetBytes("hello");
        byte[] suffix = Encoding.ASCII.GetBytes(" world");
        File.WriteAllBytes(partialPath, prefix);
        DownloadResumeState state = CreateState(partialPath, prefix.Length, prefix.Length + suffix.Length, "\"v1\"");
        PartialDownloadMetadataStore.Save(state, DateTimeOffset.UtcNow, null);
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(suffix, prefix.Length, prefix.Length + suffix.Length - 1L, prefix.Length + suffix.Length, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);
        RecordingDownloadProgress progress = new RecordingDownloadProgress();

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, prefix.Length + suffix.Length, progress), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(result.FinalPath, Is.EqualTo(Path.GetFullPath(destination)));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("hello world"));
                Assert.That(File.Exists(partialPath), Is.False);
                Assert.That(File.Exists(PartialDownloadMetadataStore.GetMetadataPath(partialPath)), Is.False);
                Assert.That(handler.Ranges, Is.EqualTo(new string?[] { "bytes=5-" }));
                Assert.That(handler.IfRanges, Is.EqualTo(new string?[] { "\"v1\"" }));
                Assert.That(progress.Reports.First().ResumedBytes, Is.EqualTo(5L));
                Assert.That(progress.Reports.Last().BytesTransferred, Is.EqualTo(11L));
                Assert.That(progress.Reports.Last().ResumedBytes, Is.EqualTo(5L));
                Assert.That(progress.Reports.Last().TotalBytes, Is.EqualTo(11L));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void LastModifiedResumeUsesHttpDateIfRange()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DateTimeOffset modified = new DateTimeOffset(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, null);
        state.LastModified = modified;
        state.HasLastModified = true;
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) =>
        {
            HttpResponseMessage response = PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, null);
            response.Content.Headers.LastModified = modified;
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(handler.IfRanges.Single(), Is.EqualTo("Tue, 01 Sep 2026 12:30:00 GMT"));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("abcdef"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MissingResumeValidatorRestartsWithoutRangeHeaders()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "old");
        DownloadResumeState state = CreateState(partialPath, 3L, 8L, null);
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => FullResponse(Encoding.ASCII.GetBytes("new-data"), "\"new\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 8L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(result.Progress.ResumedBytes, Is.Zero);
                Assert.That(File.ReadAllText(destination), Is.EqualTo("new-data"));
                Assert.That(handler.Ranges, Is.EqualTo(new string?[] { null }));
                Assert.That(handler.IfRanges, Is.EqualTo(new string?[] { null }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(false, 3L, "INVALID_CONTENT_RANGE")]
    [TestCase(true, 2L, "RANGE_LENGTH_MISMATCH")]
    public void InvalidPartialResponseMetadataIsRejectedBeforeAppend(bool includeContentRange, long contentLength, string expectedCode)
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) =>
        {
            HttpResponseMessage response = PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, "\"v1\"");
            if (!includeContentRange)
            {
                response.Content.Headers.ContentRange = null;
            }

            response.Content.Headers.ContentLength = contentLength;
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo(expectedCode));
                Assert.That(result.ResumeState.CompletedBytes, Is.EqualTo(3L));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ChangedLastModifiedValidatorIsRejectedBeforeAppend()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DateTimeOffset original = new DateTimeOffset(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, null);
        state.LastModified = original;
        state.HasLastModified = true;
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) =>
        {
            HttpResponseMessage response = PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, null);
            response.Content.Headers.LastModified = original.AddMinutes(1);
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("RESUME_VALIDATOR_CHANGED"));
                Assert.That(handler.IfRanges.Single(), Is.EqualTo("Tue, 01 Sep 2026 12:30:00 GMT"));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PreCancelledResumePreservesPartialWithoutNetworkAccess()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        DownloadRequest request = CreateRequest(destination, 6L, null);
        request.Context.CancellationToken = cancellation.Token;

        try
        {
            DownloadResult result = operation.Resume(request, state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Cancelled));
                Assert.That(result.FailureCode, Is.EqualTo("CANCELLED"));
                Assert.That(result.HasResumeState, Is.True);
                Assert.That(result.ResumeState, Is.SameAs(state));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(handler.Ranges, Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase(2L, 5L, 6L)]
    [TestCase(3L, 4L, 6L)]
    [TestCase(3L, 5L, 7L)]
    public void InvalidPartialRangesFailWithoutChangingVerifiedBytes(long from, long to, long total)
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        int bodyLength = (int)(to - from + 1L);
        byte[] body = Enumerable.Repeat((byte)'x', bodyLength).ToArray();
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(body, from, to, total, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.AnyOf("INVALID_CONTENT_RANGE", "RANGE_TOTAL_MISMATCH"));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ChangedResponseValidatorIsRejectedBeforeAppend()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, "\"v2\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.FailureCode, Is.EqualTo("RESUME_VALIDATOR_CHANGED"));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FullResponseTruncatesPartialAndRestartsFromZero()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "old-trailing-content");
        DownloadResumeState state = CreateState(partialPath, 3L, 8L, "\"old\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => FullResponse(Encoding.ASCII.GetBytes("new-data"), "\"new\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 8L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("new-data"));
                Assert.That(new FileInfo(destination).Length, Is.EqualTo(8L));
                Assert.That(handler.Ranges.Single(), Is.EqualTo("bytes=3-"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CompleteRangeNotSatisfiableResponseCommitsVerifiedPartial()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "completeJUNK");
        DownloadResumeState state = CreateState(partialPath, 8L, 8L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => RangeNotSatisfiableResponse(8L, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 8L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("complete"));
                Assert.That(handler.Ranges, Has.Count.EqualTo(1));
                Assert.That(handler.Ranges[0], Is.EqualTo("bytes=8-"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void UnsatisfiedIncompleteRangeRetriesAsFullDownload()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "old");
        DownloadResumeState state = CreateState(partialPath, 3L, 8L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => call == 0 ? RangeNotSatisfiableResponse(9L, "\"v1\"") : FullResponse(Encoding.ASCII.GetBytes("new-data"), "\"v2\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 8L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("new-data"));
                Assert.That(handler.Ranges, Is.EqualTo(new string?[] { "bytes=3-", null }));
                Assert.That(handler.IfRanges, Is.EqualTo(new string?[] { "\"v1\"", null }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DisabledResumeStartsAFullRequestWithoutRangeHeaders()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "old");
        DownloadResumeState state = CreateState(partialPath, 3L, 8L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => FullResponse(Encoding.ASCII.GetBytes("new-data"), "\"v2\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);
        DownloadRequest request = CreateRequest(destination, 8L, null);
        request.ResumePartial = false;

        try
        {
            DownloadResult result = operation.Resume(request, state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("new-data"));
                Assert.That(handler.Ranges, Is.EqualTo(new string?[] { null }));
                Assert.That(handler.IfRanges, Is.EqualTo(new string?[] { null }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SuccessfulRangeTruncatesBytesPastVerifiedBoundaryBeforeAppend()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abcJUNK");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("abcdef"));
                Assert.That(new FileInfo(destination).Length, Is.EqualTo(6L));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ResumedChecksumMismatchKeepsCompletedPartialUncommitted()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) => PartialResponse(Encoding.ASCII.GetBytes("def"), 3L, 5L, 6L, "\"v1\""));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);
        DownloadRequest request = CreateRequest(destination, 6L, null);
        request.ExpectedChecksums = new ChecksumExpectation[]
        {
            new ChecksumExpectation
            {
                Algorithm = ChecksumAlgorithm.Sha256,
                Value = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("different")))
            }
        };

        try
        {
            DownloadResult result = operation.Resume(request, state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("CHECKSUM_MISMATCH"));
                Assert.That(result.Checksums.Single().IsComplete, Is.True);
                Assert.That(result.Checksums.Single().IsMatch, Is.False);
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abcdef"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ShortPartialBodyRetainsNewVerifiedBytesWhenRecoveryResponseIsInvalid()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        DownloadResumeState state = CreateState(partialPath, 3L, 6L, "\"v1\"");
        RecordingBulkResumeHandler handler = new RecordingBulkResumeHandler((call, request) =>
        {
            HttpResponseMessage response = PartialResponse(Encoding.ASCII.GetBytes("de"), 3L, 5L, 6L, "\"v1\"");
            response.Content.Headers.ContentLength = 3L;
            return response;
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, 6L, null), state);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("INVALID_CONTENT_RANGE"));
                Assert.That(result.ResumeState.CompletedBytes, Is.EqualTo(5L));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abcde"));
                Assert.That(File.Exists(destination), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static BulkFileResumeOperation CreateOperation(HttpClient client)
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
                BufferSize = 2,
                RedirectHopCount = 3,
                AllowResume = true
            },
            Retry = new RetryPolicy
            {
                AttemptCount = 3,
                InitialDelayMilliseconds = 0,
                MaximumDelayMilliseconds = 0,
                BudgetMilliseconds = 0,
                UseJitter = false
            },
            RateLimits = new RateLimitPolicy[]
            {
                new RateLimitPolicy
                {
                    Service = ApiService.Odp,
                    RequestsPerWindow = 100,
                    WindowMilliseconds = 60000
                }
            }
        };
        DownloadHostPolicy hostPolicy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        return new BulkFileResumeOperation(client, options, hostPolicy);
    }

    private static DownloadRequest CreateRequest(string destination, long expectedSize, IProgress<DownloadProgress>? progress)
    {
        return new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/files/dataset.zip?X-Amz-Signature=fresh"),
            CatalogFileName = "dataset.zip",
            DestinationPath = destination,
            ExpectedSize = expectedSize,
            HasExpectedSize = true,
            Progress = progress
        };
    }

    private static DownloadResumeState CreateState(string partialPath, long completedBytes, long expectedSize, string? entityTag)
    {
        return new DownloadResumeState
        {
            SourceUri = new Uri("https://api.uspto.gov/files/dataset.zip?X-Amz-Signature=%5BREDACTED%5D"),
            PartialPath = partialPath,
            CompletedBytes = completedBytes,
            ExpectedTotalBytes = expectedSize,
            HasExpectedTotalBytes = true,
            ETag = entityTag
        };
    }

    private static HttpResponseMessage PartialResponse(byte[] body, long from, long to, long total, string? entityTag)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.PartialContent);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Binary);
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, total);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "dataset.zip" };
        if (entityTag != null)
        {
            response.Headers.ETag = new EntityTagHeaderValue(entityTag);
        }

        return response;
    }

    private static HttpResponseMessage FullResponse(byte[] body, string entityTag)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new ByteArrayContent(body);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Binary);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "dataset.zip" };
        response.Headers.ETag = new EntityTagHeaderValue(entityTag);
        return response;
    }

    private static HttpResponseMessage RangeNotSatisfiableResponse(long total, string entityTag)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content = new ByteArrayContent(Array.Empty<byte>());
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(total);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "dataset.zip" };
        response.Headers.ETag = new EntityTagHeaderValue(entityTag);
        return response;
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-bulk-resume-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

internal class RecordingBulkResumeHandler : HttpMessageHandler
{
    internal Func<int, HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<string?> Ranges = new List<string?>();
    internal List<string?> IfRanges = new List<string?>();

    internal RecordingBulkResumeHandler(Func<int, HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ResponseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Ranges.Add(request.Headers.Range?.ToString());
        string? ifRange = null;
        if (request.Headers.TryGetValues("If-Range", out IEnumerable<string>? values))
        {
            ifRange = values.Single();
        }

        IfRanges.Add(ifRange);
        HttpResponseMessage response = ResponseFactory(Ranges.Count - 1, request);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}
