using System.Text.Json;

namespace jmap.Tests;

internal sealed class FakeJmapAuthorizationProvider : IJmapAuthorizationProvider
{
    public string Value = "Bearer transport-secret";
    public Exception? Failure = null;
    public List<string> ResourceUrls = new List<string>();

    public Task<string> GetAuthorizationValueAsync(
        string resourceUrl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ResourceUrls.Add(resourceUrl);
        if (Failure != null)
        {
            return Task.FromException<string>(Failure);
        }

        return Task.FromResult(Value);
    }
}

internal sealed class FakeJmapClock : IJmapClock
{
    public DateTimeOffset CurrentUtcDateTime = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    public DateTimeOffset GetCurrentUtcDateTime()
    {
        return CurrentUtcDateTime;
    }

    public void Advance(TimeSpan duration)
    {
        CurrentUtcDateTime = CurrentUtcDateTime.Add(duration);
    }
}

internal sealed class FakeJmapStatePublisher : IJmapStatePublisher
{
    public List<JmapStatePublication> Publications = new List<JmapStatePublication>();
    public Exception? Failure = null;

    public Task PublishStateChangeAsync(
        JmapStatePublication publication,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Publications.Add(publication);
        if (Failure != null)
        {
            return Task.FromException(Failure);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeJmapSessionService : IJmapSessionService
{
    public JmapSession Session;
    public List<JmapRequestContext> Contexts = new List<JmapRequestContext>();
    public Exception? Failure = null;

    public FakeJmapSessionService(JmapSession session)
    {
        Session = session;
    }

    public Task<JmapSession> GetServerSessionAsync(
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Contexts.Add(context);
        if (Failure != null)
        {
            return Task.FromException<JmapSession>(Failure);
        }

        return Task.FromResult(Session);
    }
}

internal sealed class FakeJmapCoreService : IJmapCoreService
{
    public List<JsonElement> Arguments = new List<JsonElement>();
    public List<JmapRequestContext> Contexts = new List<JmapRequestContext>();
    public Func<JsonElement, JmapRequestContext, JsonElement>? Handler = null;

    public JsonElement ExecuteCoreEcho(JsonElement arguments, JmapRequestContext context)
    {
        Arguments.Add(arguments.Clone());
        Contexts.Add(context);
        if (Handler != null)
        {
            return Handler(arguments, context);
        }

        return arguments.Clone();
    }
}

internal sealed class FakeJmapBlobService : IBlobService
{
    public BlobUploadResponse UploadResponse = new BlobUploadResponse();
    public List<JmapUnreferencedBlob> UnreferencedBlobs = new List<JmapUnreferencedBlob>();
    public JmapDownloadContent? DownloadContent = null;
    public BlobCopyResponse CopyResponse = new BlobCopyResponse();
    public List<BlobUploadRequest> UploadRequests = new List<BlobUploadRequest>();
    public List<BlobDownloadRequest> DownloadRequests = new List<BlobDownloadRequest>();
    public List<BlobCopyRequest> CopyRequests = new List<BlobCopyRequest>();
    public List<string> RefreshedBlobIds = new List<string>();
    public List<string> EvictedBlobIds = new List<string>();
    public Exception? Failure = null;

    public Task<BlobUploadResponse> StoreUploadedBlobAsync(
        BlobUploadRequest request,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UploadRequests.Add(request);
        if (Failure != null)
        {
            return Task.FromException<BlobUploadResponse>(Failure);
        }

        return Task.FromResult(UploadResponse);
    }

    public Task<List<JmapUnreferencedBlob>> ListUnreferencedBlobsAsync(
        string accountId,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure != null)
        {
            return Task.FromException<List<JmapUnreferencedBlob>>(Failure);
        }

        return Task.FromResult(new List<JmapUnreferencedBlob>(UnreferencedBlobs));
    }

    public Task RefreshUnreferencedBlobAsync(
        string accountId,
        string blobId,
        DateTimeOffset retainUntilUtc,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RefreshedBlobIds.Add(blobId);
        return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
    }

    public Task EvictUnreferencedBlobAsync(
        string accountId,
        string blobId,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EvictedBlobIds.Add(blobId);
        return Failure == null ? Task.CompletedTask : Task.FromException(Failure);
    }

    public Task<JmapDownloadContent?> OpenStoredBlobAsync(
        BlobDownloadRequest request,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DownloadRequests.Add(request);
        if (Failure != null)
        {
            return Task.FromException<JmapDownloadContent?>(Failure);
        }

        if (DownloadContent != null && DownloadContent.Content != null && DownloadContent.Content.CanSeek)
        {
            DownloadContent.Content.Position = 0;
        }

        return Task.FromResult(DownloadContent);
    }

    public Task<BlobCopyResponse> CopyStoredBlobsAsync(
        BlobCopyRequest request,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CopyRequests.Add(request);
        if (Failure != null)
        {
            return Task.FromException<BlobCopyResponse>(Failure);
        }

        return Task.FromResult(CopyResponse);
    }
}
