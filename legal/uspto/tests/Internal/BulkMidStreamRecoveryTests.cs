using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkMidStreamRecoveryTests
{
    [Test]
    public void InterruptedBodyReopensAtNewVerifiedBoundaryWithoutDuplicateBytes()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abcd");
        RecordingBulkRecoveryHandler handler = new RecordingBulkRecoveryHandler((call, request) => call == 0
            ? InterruptedResponse(Encoding.ASCII.GetBytes("efghij"), 2, 4L, 9L, 10L)
            : PartialResponse(Encoding.ASCII.GetBytes("ghij"), 6L, 9L, 10L));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client, 3);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        operation.RetryEngine = new RetryEngine(CreateRetryPolicy(3, 7), delay, new FixedRetryRandom(0.5));
        RecordingDownloadProgress progress = new RecordingDownloadProgress();

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, progress), CreateState(partialPath, 4L));

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("abcdefghij"));
                Assert.That(new FileInfo(destination).Length, Is.EqualTo(10L));
                Assert.That(handler.Ranges, Is.EqualTo(new string[] { "bytes=4-", "bytes=6-" }));
                Assert.That(handler.IfRanges, Is.EqualTo(new string[] { "\"stable\"", "\"stable\"" }));
                Assert.That(delay.Waits, Is.EqualTo(new long[] { 7L }));
                Assert.That(operation.RateLimiter.GetObservation(ApiService.Odp).Used, Is.EqualTo(2));
                Assert.That(progress.Reports.Select(item => item.BytesTransferred), Is.Ordered);
                Assert.That(progress.Reports.Last().BytesTransferred, Is.EqualTo(10L));
                Assert.That(progress.Reports.Last().ResumedBytes, Is.EqualTo(4L));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MultipleInterruptionsAdvanceEachRangeToTheExactWrittenBoundary()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "ab");
        RecordingBulkRecoveryHandler handler = new RecordingBulkRecoveryHandler((call, request) =>
        {
            if (call == 0)
            {
                return InterruptedResponse(Encoding.ASCII.GetBytes("cdefghij"), 2, 2L, 9L, 10L);
            }

            if (call == 1)
            {
                return InterruptedResponse(Encoding.ASCII.GetBytes("efghij"), 3, 4L, 9L, 10L);
            }

            return PartialResponse(Encoding.ASCII.GetBytes("hij"), 7L, 9L, 10L);
        });
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client, 4);

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, null), CreateState(partialPath, 2L));

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(File.ReadAllText(destination), Is.EqualTo("abcdefghij"));
                Assert.That(handler.Ranges, Is.EqualTo(new string[] { "bytes=2-", "bytes=4-", "bytes=7-" }));
                Assert.That(operation.RateLimiter.GetObservation(ApiService.Odp).Used, Is.EqualTo(3));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExhaustedRecoveryPersistsTheLastVerifiedBoundary()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        RecordingBulkRecoveryHandler handler = new RecordingBulkRecoveryHandler((call, request) => call == 0
            ? InterruptedResponse(Encoding.ASCII.GetBytes("defghij"), 1, 3L, 9L, 10L)
            : InterruptedResponse(Encoding.ASCII.GetBytes("efghij"), 1, 4L, 9L, 10L));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client, 3);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        operation.RetryEngine = new RetryEngine(CreateRetryPolicy(2, 5), delay, new FixedRetryRandom(0.5));

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, null), CreateState(partialPath, 3L));
            DownloadResumeState? persisted = PartialDownloadMetadataStore.Load(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(result.State, Is.EqualTo(DownloadState.Failed));
                Assert.That(result.FailureCode, Is.EqualTo("MID_STREAM_RECOVERY_EXHAUSTED"));
                Assert.That(result.HasResumeState, Is.True);
                Assert.That(result.ResumeState.CompletedBytes, Is.EqualTo(5L));
                Assert.That(persisted?.CompletedBytes, Is.EqualTo(5L));
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abcde"));
                Assert.That(File.Exists(destination), Is.False);
                Assert.That(handler.Ranges, Is.EqualTo(new string[] { "bytes=3-", "bytes=4-" }));
                Assert.That(delay.Waits, Is.EqualTo(new long[] { 5L }));
                Assert.That(operation.RateLimiter.GetObservation(ApiService.Odp).Used, Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void NonTransientBodyFailureDoesNotConsumeARetryAttempt()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        string destination = Path.Combine(directory, "dataset.zip");
        File.WriteAllText(partialPath, "abc");
        RecordingBulkRecoveryHandler handler = new RecordingBulkRecoveryHandler((call, request) => FaultedResponse(Encoding.ASCII.GetBytes("defghij"), new InvalidOperationException("permanent"), 3L, 9L, 10L));
        using HttpClient client = new HttpClient(handler);
        BulkFileResumeOperation operation = CreateOperation(client, 3);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        operation.RetryEngine = new RetryEngine(CreateRetryPolicy(3, 5), delay, new FixedRetryRandom(0.5));

        try
        {
            DownloadResult result = operation.Resume(CreateRequest(destination, null), CreateState(partialPath, 3L));

            Assert.Multiple(() =>
            {
                Assert.That(result.FailureCode, Is.EqualTo("MID_STREAM_RECOVERY_EXHAUSTED"));
                Assert.That(handler.Ranges, Has.Count.EqualTo(1));
                Assert.That(delay.Waits, Is.Empty);
                Assert.That(File.ReadAllText(partialPath), Is.EqualTo("abc"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static BulkFileResumeOperation CreateOperation(HttpClient client, int attemptCount)
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
                BufferSize = 4,
                RedirectHopCount = 3,
                AllowResume = true
            },
            Retry = CreateRetryPolicy(attemptCount, 0),
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

    private static RetryPolicy CreateRetryPolicy(int attemptCount, int delayMilliseconds)
    {
        return new RetryPolicy
        {
            AttemptCount = attemptCount,
            InitialDelayMilliseconds = delayMilliseconds,
            MaximumDelayMilliseconds = delayMilliseconds,
            BudgetMilliseconds = delayMilliseconds * attemptCount,
            UseJitter = false
        };
    }

    private static DownloadRequest CreateRequest(string destination, IProgress<DownloadProgress>? progress)
    {
        return new DownloadRequest
        {
            SourceUri = new Uri("https://api.uspto.gov/files/dataset.zip"),
            CatalogFileName = "dataset.zip",
            DestinationPath = destination,
            ExpectedSize = 10L,
            HasExpectedSize = true,
            Progress = progress
        };
    }

    private static DownloadResumeState CreateState(string partialPath, long completedBytes)
    {
        return new DownloadResumeState
        {
            SourceUri = new Uri("https://api.uspto.gov/files/dataset.zip"),
            PartialPath = partialPath,
            CompletedBytes = completedBytes,
            ExpectedTotalBytes = 10L,
            HasExpectedTotalBytes = true,
            ETag = "\"stable\""
        };
    }

    private static HttpResponseMessage PartialResponse(byte[] body, long from, long to, long total)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.PartialContent);
        response.Content = new ByteArrayContent(body);
        ApplyResponseHeaders(response, from, to, total);
        return response;
    }

    private static HttpResponseMessage InterruptedResponse(byte[] body, int bytesBeforeFailure, long from, long to, long total)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.PartialContent);
        response.Content = new StreamingTestContent(new FaultAfterBytesStream(body, bytesBeforeFailure, new IOException("connection interrupted")), UsptoMediaTypes.Binary);
        response.Content.Headers.ContentLength = body.Length;
        ApplyResponseHeaders(response, from, to, total);
        return response;
    }

    private static HttpResponseMessage FaultedResponse(byte[] body, Exception failure, long from, long to, long total)
    {
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.PartialContent);
        response.Content = new StreamingTestContent(new FaultAfterBytesStream(body, 0, failure), UsptoMediaTypes.Binary);
        response.Content.Headers.ContentLength = body.Length;
        ApplyResponseHeaders(response, from, to, total);
        return response;
    }

    private static void ApplyResponseHeaders(HttpResponseMessage response, long from, long to, long total)
    {
        response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, total);
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "dataset.zip" };
        response.Headers.ETag = new EntityTagHeaderValue("\"stable\"");
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-bulk-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

internal class FaultAfterBytesStream : MemoryStream
{
    internal int BytesBeforeFailure;
    internal Exception Failure;

    internal FaultAfterBytesStream(byte[] body, int bytesBeforeFailure, Exception failure)
        : base(body, false)
    {
        BytesBeforeFailure = bytesBeforeFailure;
        Failure = failure;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        long remainingBeforeFailure = BytesBeforeFailure - Position;
        if (remainingBeforeFailure <= 0L)
        {
            return Task.FromException<int>(Failure);
        }

        if (count > remainingBeforeFailure)
        {
            count = (int)remainingBeforeFailure;
        }

        return base.ReadAsync(buffer, offset, count, cancellationToken);
    }
}

internal class RecordingBulkRecoveryHandler : HttpMessageHandler
{
    internal Func<int, HttpRequestMessage, HttpResponseMessage> ResponseFactory;
    internal List<string> Ranges = new List<string>();
    internal List<string> IfRanges = new List<string>();

    internal RecordingBulkRecoveryHandler(Func<int, HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        ResponseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Ranges.Add(request.Headers.Range?.ToString() ?? string.Empty);
        string ifRange = string.Empty;
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
