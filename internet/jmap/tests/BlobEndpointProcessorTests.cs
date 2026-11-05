using System.Text;

namespace jmap.Tests;

public class BlobEndpointProcessorTests
{
    [Test]
    public async Task UploadStreamsToBackendRefreshesDedupAndEvictsOldestForUploaderQuota()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        FakeBlobService blobs = new FakeBlobService();
        blobs.Stored = UploadResult("account_1", "current", "application/octet-stream", 30);
        blobs.Unreferenced.Add(Unreferenced("account_1", "current", 30, now.AddDays(-2)));
        blobs.Unreferenced.Add(Unreferenced("account_1", "other", 30, now.AddHours(-1)));
        JmapServerOptions options = Options(40, 50, 2);
        IJmapServer processor = Server(options, blobs, now, true);
        TrackingStream body = new TrackingStream(new byte[30]);
        JmapEndpointRequest request = UploadRequest(body, true, 30);

        JmapEndpointResponse response = await processor.ProcessUploadEndpointAsync(request, CancellationToken.None);
        JmapJsonReader reader = new JmapJsonReader(10000, 64);
        BlobUploadResponse decoded = new BlobPushJsonCodec(reader).DecodeBlobUploadResponse(
            reader.ReadUtf8Memory(response.BodyBytes!));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(201));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.Json));
            Assert.That(response.HasContentLength, Is.True);
            Assert.That(decoded.BlobId, Is.EqualTo("current"));
            Assert.That(blobs.LastUpload!.Content, Is.SameAs(body));
            Assert.That(blobs.LastUpload.ContentLength, Is.EqualTo(30));
            Assert.That(blobs.LastContext, Is.SameAs(request.Context));
            Assert.That(blobs.RefreshedBlobIds, Is.EqualTo(new[] { "current" }));
            Assert.That(blobs.LastRetainUntil, Is.EqualTo(now.AddSeconds(3600)));
            Assert.That(blobs.EvictedBlobIds, Is.EqualTo(new[] { "other" }));
            Assert.That(body.WasDisposed, Is.False);
            Assert.That(body.ReadCount, Is.Zero);
        }
    }

    [Test]
    public async Task KnownOversizeUploadIsRejectedBeforeSessionOrBlobBackend()
    {
        FakeBlobService blobs = new FakeBlobService();
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        JmapEndpointRequest request = UploadRequest(new MemoryStream(new byte[11]), true, 11);

        JmapEndpointResponse response = await processor.ProcessUploadEndpointAsync(request, CancellationToken.None);
        JmapProblem problem = new JmapJsonCodec().DecodeProblemBytes(response.BodyBytes!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(413));
            Assert.That(problem.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(problem.Limit, Is.EqualTo(JmapProperties.MaxSizeUpload));
            Assert.That(blobs.StoreCalls, Is.Zero);
        }
    }

    [Test]
    public async Task ChunkedOversizeUploadIsEvictedBeforeReturningLimitProblem()
    {
        FakeBlobService blobs = new FakeBlobService();
        blobs.Stored = UploadResult("account_1", "oversize", "application/octet-stream", 11);
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        JmapEndpointRequest request = UploadRequest(new MemoryStream(new byte[11]), false, 0);

        JmapEndpointResponse response = await processor.ProcessUploadEndpointAsync(request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(413));
            Assert.That(blobs.StoreCalls, Is.EqualTo(1));
            Assert.That(blobs.EvictedBlobIds, Is.EqualTo(new[] { "oversize" }));
            Assert.That(blobs.RefreshedBlobIds, Is.Empty);
        }
    }

    [Test]
    public async Task ConcurrentUploadLimitRejectsExcessAndReleasesPermitAfterCompletion()
    {
        FakeBlobService blobs = new FakeBlobService();
        TaskCompletionSource<BlobUploadResponse> pending = new TaskCompletionSource<BlobUploadResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        blobs.PendingStore = pending;
        blobs.Unreferenced.Add(Unreferenced(
            "account_1",
            "first",
            1,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero)));
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);

        Task<JmapEndpointResponse> first = processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1),
            CancellationToken.None);
        Assert.That(blobs.StoreEntered.Wait(5000), Is.True);
        JmapEndpointResponse excess = await processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 2 }), true, 1),
            CancellationToken.None);
        pending.SetResult(UploadResult("account_1", "first", "application/octet-stream", 1));
        JmapEndpointResponse completed = await first;
        blobs.PendingStore = null;
        blobs.Stored = UploadResult("account_1", "first", "application/octet-stream", 1);
        JmapEndpointResponse after = await processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 3 }), true, 1),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(excess.StatusCode, Is.EqualTo(429));
            Assert.That(completed.StatusCode, Is.EqualTo(201));
            Assert.That(after.StatusCode, Is.EqualTo(201));
            Assert.That(blobs.StoreCalls, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task FailedUploadReturnsSanitizedFailureAndReleasesItsConcurrentPermit()
    {
        FakeBlobService blobs = new FakeBlobService();
        blobs.StoreFailure = new InvalidOperationException("store failed");
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);

        JmapEndpointResponse failed = await processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1),
            CancellationToken.None);

        blobs.StoreFailure = null;
        blobs.Stored = UploadResult("account_1", "after_failure", "application/octet-stream", 1);
        JmapEndpointResponse after = await processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 2 }), true, 1),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failed.StatusCode, Is.EqualTo(500));
            Assert.That(failed.ContentType, Is.EqualTo(JmapMediaTypes.ProblemJson));
            Assert.That(Encoding.UTF8.GetString(failed.BodyBytes!), Does.Not.Contain("store failed"));
            Assert.That(after.StatusCode, Is.EqualTo(201));
            Assert.That(blobs.StoreCalls, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task DownloadReturnsBackendStreamWithSafeDispositionAndImmutablePrivateCaching()
    {
        FakeBlobService blobs = new FakeBlobService();
        TrackingStream content = new TrackingStream(Encoding.UTF8.GetBytes("blob"));
        blobs.Download = new JmapDownloadContent();
        blobs.Download.Content = content;
        blobs.Download.MediaType = "application/pdf";
        blobs.Download.FileName = "résumé report.pdf";
        blobs.Download.Size = 4;
        blobs.Download.HasSize = true;
        blobs.Download.CacheLifetimeSeconds = 7200;
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        JmapEndpointRequest request = DownloadRequest("application/pdf", "résumé report.pdf");

        JmapEndpointResponse response = await processor.ProcessDownloadEndpointAsync(request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(response.BodyStream, Is.SameAs(content));
            Assert.That(response.BodyBytes, Is.Null);
            Assert.That(response.ContentLength, Is.EqualTo(4));
            Assert.That(response.HasContentLength, Is.True);
            Assert.That(response.Headers["Cache-Control"], Is.EqualTo("private, immutable, max-age=7200"));
            Assert.That(response.Headers["Content-Disposition"], Does.StartWith("attachment"));
            Assert.That(response.Headers["Content-Disposition"], Does.Contain("filename="));
            Assert.That(response.Headers["Content-Disposition"], Does.Contain("filename*="));
            Assert.That(content.ReadCount, Is.Zero);
            Assert.That(content.WasDisposed, Is.False);
            Assert.That(blobs.LastDownload!.BlobId, Is.EqualTo("blob_1"));
            Assert.That(blobs.LastContext, Is.SameAs(request.Context));
        }
    }

    [Test]
    public async Task InaccessibleUnreferencedBlobUsesIndistinguishableNotFoundResponse()
    {
        FakeBlobService blobs = new FakeBlobService();
        blobs.Download = null;
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        JmapEndpointRequest request = DownloadRequest("application/octet-stream", "private.bin");
        request.Context.UserId = "not-the-uploader";

        JmapEndpointResponse response = await processor.ProcessDownloadEndpointAsync(request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(404));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.ProblemJson));
            Assert.That(response.BodyStream, Is.Null);
            Assert.That(blobs.OpenCalls, Is.EqualTo(1));
            Assert.That(blobs.LastContext!.UserId, Is.EqualTo("not-the-uploader"));
        }
    }

    [Test]
    public async Task AccountValidationPreventsBackendAccessForUnknownAccount()
    {
        FakeBlobService blobs = new FakeBlobService();
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            false);

        JmapEndpointResponse upload = await processor.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1),
            CancellationToken.None);
        JmapEndpointResponse download = await processor.ProcessDownloadEndpointAsync(
            DownloadRequest("application/octet-stream", "blob.bin"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.StatusCode, Is.EqualTo(404));
            Assert.That(download.StatusCode, Is.EqualTo(404));
            Assert.That(blobs.StoreCalls, Is.Zero);
            Assert.That(blobs.OpenCalls, Is.Zero);
        }
    }

    [Test]
    public async Task InvalidBackendDownloadMetadataIsSanitizedAndDisposesStream()
    {
        FakeBlobService blobs = new FakeBlobService();
        TrackingStream content = new TrackingStream(new byte[] { 1 });
        blobs.Download = new JmapDownloadContent();
        blobs.Download.Content = content;
        blobs.Download.MediaType = "text/plain";
        blobs.Download.FileName = "../unsafe.txt";
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);

        JmapEndpointResponse response = await processor.ProcessDownloadEndpointAsync(
            DownloadRequest("text/plain", "safe.txt"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(500));
            Assert.That(response.ContentType, Is.EqualTo(JmapMediaTypes.ProblemJson));
            Assert.That(Encoding.UTF8.GetString(response.BodyBytes!), Does.Not.Contain("unsafe"));
            Assert.That(content.WasDisposed, Is.True);
        }
    }

    [Test]
    public async Task EndpointMethodAuthenticationAndRouteValidationReturnHttpProblems()
    {
        FakeBlobService blobs = new FakeBlobService();
        IJmapServer processor = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        JmapEndpointRequest wrongMethod = UploadRequest(new MemoryStream(), true, 0);
        wrongMethod.Method = "GET";
        JmapEndpointRequest unauthenticated = DownloadRequest("application/octet-stream", "blob.bin");
        unauthenticated.Context.UserId = null;
        JmapEndpointRequest unsafeRoute = DownloadRequest("application/octet-stream", "../blob.bin");

        JmapEndpointResponse method = await processor.ProcessUploadEndpointAsync(wrongMethod, CancellationToken.None);
        JmapEndpointResponse auth = await processor.ProcessDownloadEndpointAsync(unauthenticated, CancellationToken.None);
        JmapEndpointResponse route = await processor.ProcessDownloadEndpointAsync(unsafeRoute, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(method.StatusCode, Is.EqualTo(405));
            Assert.That(method.Headers["Allow"], Is.EqualTo("POST"));
            Assert.That(auth.StatusCode, Is.EqualTo(401));
            Assert.That(route.StatusCode, Is.EqualTo(400));
        }
    }

    [Test]
    public async Task EmptyArbitraryMediaUploadUsesPublicServerAndRefreshesRetention()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        FakeBlobService blobs = new FakeBlobService();
        blobs.Stored = UploadResult("account_1", "empty", "image/avif; profile=hdr", 0);
        blobs.Unreferenced.Add(Unreferenced("account_1", "empty", 0, now.AddDays(-1)));
        IJmapServer server = Server(Options(10, 10, 1), blobs, now, true);
        JmapEndpointRequest request = UploadRequest(new MemoryStream(), false, 0);
        request.ContentType = "image/avif; profile=hdr";

        JmapEndpointResponse response = await server.ProcessUploadEndpointAsync(
            request, CancellationToken.None);
        BlobUploadResponse decoded = new JmapJsonCodec().Push.DecodeBlobUploadResponse(
            System.Text.Json.JsonDocument.Parse(response.BodyBytes!).RootElement);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(201));
            Assert.That(decoded.Type, Is.EqualTo("image/avif; profile=hdr"));
            Assert.That(decoded.Size, Is.Zero);
            Assert.That(blobs.LastUpload!.HasContentLength, Is.False);
            Assert.That(blobs.RefreshedBlobIds, Is.EqualTo(new[] { "empty" }));
            Assert.That(blobs.LastRetainUntil, Is.EqualTo(now.AddSeconds(3600)));
            Assert.That(blobs.EvictedBlobIds, Is.Empty);
        }
    }

    [Test]
    public void CanceledPublicUploadDoesNotEnterSessionOrBlobBackend()
    {
        FakeBlobService blobs = new FakeBlobService();
        IJmapServer server = Server(
            Options(10, 10, 1),
            blobs,
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero),
            true);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<JmapEndpointResponse> upload = server.ProcessUploadEndpointAsync(
            UploadRequest(new MemoryStream(new byte[] { 1 }), true, 1),
            cancellation.Token);

        Assert.That(async () => await upload, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.IsCanceled, Is.True);
            Assert.That(blobs.StoreCalls, Is.Zero);
            Assert.That(blobs.OpenCalls, Is.Zero);
        }
    }

    private static IJmapServer Server(
        JmapServerOptions options,
        FakeBlobService blobs,
        DateTimeOffset now,
        bool hasAccount)
    {
        JmapServerServices services = new JmapServerServices();
        services.BlobService = blobs;
        services.SessionService = new FakeSessionService(hasAccount);
        services.Clock = new FakeClock(now);
        return new JmapServer(options, services);
    }

    private static JmapServerOptions Options(long maxUpload, long quota, int concurrency)
    {
        JmapServerOptions options = new JmapServerOptions();
        options.MaxUploadBytes = maxUpload;
        options.UnreferencedBlobQuotaBytes = quota;
        options.MaxConcurrentUploads = concurrency;
        options.UnreferencedBlobMinimumLifetimeSeconds = 3600;
        return options;
    }

    private static JmapEndpointRequest UploadRequest(Stream body, bool hasLength, long length)
    {
        JmapEndpointRequest request = new JmapEndpointRequest();
        request.Method = "POST";
        request.ContentType = "application/octet-stream";
        request.Body = body;
        request.ContentLength = length;
        request.HasContentLength = hasLength;
        request.Context.UserId = "user_1";
        request.Context.CredentialId = "credential_1";
        request.RouteValues.Add(JmapProperties.AccountId, "account_1");
        return request;
    }

    private static JmapEndpointRequest DownloadRequest(string type, string name)
    {
        JmapEndpointRequest request = new JmapEndpointRequest();
        request.Method = "GET";
        request.Context.UserId = "user_1";
        request.Context.CredentialId = "credential_1";
        request.RouteValues.Add(JmapProperties.AccountId, "account_1");
        request.RouteValues.Add(JmapProperties.BlobId, "blob_1");
        request.QueryParameters.Add(JmapProperties.Type, type);
        request.QueryParameters.Add(JmapProperties.Name, name);
        return request;
    }

    private static BlobUploadResponse UploadResult(string accountId, string blobId, string type, long size)
    {
        BlobUploadResponse result = new BlobUploadResponse();
        result.AccountId = accountId;
        result.BlobId = blobId;
        result.Type = type;
        result.Size = size;
        return result;
    }

    private static JmapUnreferencedBlob Unreferenced(
        string accountId,
        string blobId,
        long size,
        DateTimeOffset uploadedAt)
    {
        JmapUnreferencedBlob blob = new JmapUnreferencedBlob();
        blob.AccountId = accountId;
        blob.BlobId = blobId;
        blob.Size = size;
        blob.UploadedAtUtc = uploadedAt;
        return blob;
    }

    private sealed class FakeSessionService : IJmapSessionService
    {
        public bool HasAccount;

        public FakeSessionService(bool hasAccount)
        {
            HasAccount = hasAccount;
        }

        public Task<JmapSession> GetServerSessionAsync(
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            JmapSession session = new JmapSession();
            if (HasAccount)
            {
                session.Accounts.Add("account_1", new JmapAccount());
            }

            return Task.FromResult(session);
        }
    }

    private sealed class FakeClock : IJmapClock
    {
        public DateTimeOffset Now;

        public FakeClock(DateTimeOffset now)
        {
            Now = now;
        }

        public DateTimeOffset GetCurrentUtcDateTime()
        {
            return Now;
        }
    }

    private sealed class FakeBlobService : IBlobService
    {
        public BlobUploadResponse Stored = UploadResult("account_1", "blob_1", "application/octet-stream", 0);
        public JmapDownloadContent? Download;
        public List<JmapUnreferencedBlob> Unreferenced = new List<JmapUnreferencedBlob>();
        public List<string> RefreshedBlobIds = new List<string>();
        public List<string> EvictedBlobIds = new List<string>();
        public BlobUploadRequest? LastUpload;
        public BlobDownloadRequest? LastDownload;
        public JmapRequestContext? LastContext;
        public DateTimeOffset LastRetainUntil;
        public TaskCompletionSource<BlobUploadResponse>? PendingStore;
        public Exception? StoreFailure;
        public ManualResetEventSlim StoreEntered = new ManualResetEventSlim();
        public int StoreCalls;
        public int OpenCalls;

        public Task<BlobUploadResponse> StoreUploadedBlobAsync(
            BlobUploadRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            StoreCalls = StoreCalls + 1;
            LastUpload = request;
            LastContext = context;
            StoreEntered.Set();
            if (StoreFailure != null)
            {
                return Task.FromException<BlobUploadResponse>(StoreFailure);
            }

            if (PendingStore != null)
            {
                return PendingStore.Task;
            }

            return Task.FromResult(Stored);
        }

        public Task<List<JmapUnreferencedBlob>> ListUnreferencedBlobsAsync(
            string accountId,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            LastContext = context;
            return Task.FromResult(Unreferenced);
        }

        public Task RefreshUnreferencedBlobAsync(
            string accountId,
            string blobId,
            DateTimeOffset retainUntilUtc,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            RefreshedBlobIds.Add(blobId);
            LastRetainUntil = retainUntilUtc;
            foreach (JmapUnreferencedBlob record in Unreferenced)
            {
                if (record.BlobId == blobId)
                {
                    record.UploadedAtUtc = retainUntilUtc.AddSeconds(-3600);
                }
            }

            return Task.CompletedTask;
        }

        public Task EvictUnreferencedBlobAsync(
            string accountId,
            string blobId,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            EvictedBlobIds.Add(blobId);
            return Task.CompletedTask;
        }

        public Task<JmapDownloadContent?> OpenStoredBlobAsync(
            BlobDownloadRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            OpenCalls = OpenCalls + 1;
            LastDownload = request;
            LastContext = context;
            return Task.FromResult(Download);
        }

        public Task<BlobCopyResponse> CopyStoredBlobsAsync(
            BlobCopyRequest request,
            JmapRequestContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new BlobCopyResponse());
        }
    }

    private sealed class TrackingStream : MemoryStream
    {
        public int ReadCount;
        public bool WasDisposed;

        public TrackingStream(byte[] bytes) : base(bytes)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount = ReadCount + 1;
            return base.Read(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
