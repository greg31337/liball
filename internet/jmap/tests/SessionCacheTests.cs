using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jmap.Tests;

public class SessionCacheTests
{
    [Test]
    public async Task DirectDiscoveryUsesConfiguredUrlAndCachesOnlyCompletedSession()
    {
        RecordingTransport transport = new RecordingTransport();
        TaskCompletionSource<JmapSession> pending = new TaskCompletionSource<JmapSession>();
        transport.SessionResults.Enqueue(pending.Task);
        JmapClientOptions options = Options("https://jmap.example.test/session");
        SessionCache cache = new SessionCache(options, transport);
        CancellationTokenSource cancellation = new CancellationTokenSource();

        Task<JmapSession> discovery = cache.DiscoverSessionAsync(options.DiscoveryOptions, cancellation.Token);

        Assert.That(cache.GetSession(), Is.Null);
        pending.SetResult(ValidSession("state-1"));
        JmapSession result = await discovery;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.State, Is.EqualTo("state-1"));
            Assert.That(cache.GetSession(), Is.SameAs(result));
            Assert.That(cache.IsSessionStale(), Is.False);
            Assert.That(transport.SessionUrls, Is.EqualTo(new[] { "https://jmap.example.test/session" }));
            Assert.That(transport.CancellationTokens[0], Is.EqualTo(cancellation.Token));
        }
    }

    [Test]
    public void DirectDiscoveryRequiresHttpsUnlessInsecureHttpIsExplicitlyAllowed()
    {
        RecordingTransport transport = new RecordingTransport();
        SessionCache cache = new SessionCache(new JmapClientOptions(), transport);
        JmapDiscoveryOptions missing = new JmapDiscoveryOptions();
        JmapDiscoveryOptions insecure = new JmapDiscoveryOptions { SessionUrl = "http://localhost/session" };
        JmapDiscoveryOptions fragmented = new JmapDiscoveryOptions { SessionUrl = "https://example.test/session#part" };

        JmapValidationException missingError = Assert.Throws<JmapValidationException>(
            () => cache.DiscoverSessionAsync(missing, CancellationToken.None))!;
        JmapValidationException insecureError = Assert.Throws<JmapValidationException>(
            () => cache.DiscoverSessionAsync(insecure, CancellationToken.None))!;
        JmapValidationException fragmentError = Assert.Throws<JmapValidationException>(
            () => cache.DiscoverSessionAsync(fragmented, CancellationToken.None))!;

        insecure.AllowInsecureHttp = true;
        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("local")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingError.InvalidProperties, Is.EqualTo(new[] { "sessionUrl" }));
            Assert.That(insecureError.InvalidProperties, Is.EqualTo(new[] { "sessionUrl" }));
            Assert.That(fragmentError.InvalidProperties, Is.EqualTo(new[] { "sessionUrl" }));
            Assert.That(async () => await cache.DiscoverSessionAsync(insecure, CancellationToken.None), Throws.Nothing);
        }
    }

    [Test]
    public async Task InvalidRefreshDoesNotReplaceTheLastValidatedSessionOrConfiguration()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("state-1")));
        JmapClientOptions options = Options("https://one.example.test/session");
        SessionCache cache = new SessionCache(options, transport);
        JmapSession original = await cache.RefreshSessionAsync(CancellationToken.None);
        JmapSession invalid = ValidSession("state-2");
        invalid.ApiUrl = null;
        transport.SessionResults.Enqueue(Task.FromResult(invalid));
        JmapDiscoveryOptions replacement = new JmapDiscoveryOptions
        {
            SessionUrl = "https://two.example.test/session"
        };

        JmapValidationException error = Assert.ThrowsAsync<JmapValidationException>(
            async () => await cache.DiscoverSessionAsync(replacement, CancellationToken.None))!;

        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("state-3")));
        JmapSession refreshed = await cache.RefreshSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.InvalidProperties, Does.Contain("apiUrl"));
            Assert.That(original.State, Is.EqualTo("state-1"));
            Assert.That(refreshed.State, Is.EqualTo("state-3"));
            Assert.That(transport.SessionUrls, Is.EqualTo(new[]
            {
                "https://one.example.test/session",
                "https://two.example.test/session",
                "https://one.example.test/session"
            }));
        }
    }

    [Test]
    public async Task ChangedResponseStateMarksCacheStaleAndRefreshesBeforeNextUse()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapSession first = ValidSession("state-1");
        JmapSession second = ValidSession("state-2");
        transport.SessionResults.Enqueue(Task.FromResult(first));
        transport.SessionResults.Enqueue(Task.FromResult(second));
        SessionCache cache = new SessionCache(Options("https://example.test/session"), transport);

        JmapSession initial = await cache.GetUsableSessionAsync(CancellationToken.None);
        cache.ObserveResponseSessionState("state-1");
        bool staleAfterSameState = cache.IsSessionStale();
        cache.ObserveResponseSessionState("state-2");
        bool staleAfterChange = cache.IsSessionStale();
        JmapSession refreshed = await cache.GetUsableSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(initial, Is.SameAs(first));
            Assert.That(staleAfterSameState, Is.False);
            Assert.That(staleAfterChange, Is.True);
            Assert.That(refreshed, Is.SameAs(second));
            Assert.That(cache.IsSessionStale(), Is.False);
            Assert.That(transport.SessionUrls, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task DisabledAutomaticRefreshKeepsStaleSessionButExplicitRefreshStillFetches()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapSession first = ValidSession("state-1");
        JmapSession second = ValidSession("state-2");
        transport.SessionResults.Enqueue(Task.FromResult(first));
        transport.SessionResults.Enqueue(Task.FromResult(second));
        JmapClientOptions options = Options("https://example.test/session");
        options.RefreshSessionOnStateChange = false;
        SessionCache cache = new SessionCache(options, transport);

        await cache.GetUsableSessionAsync(CancellationToken.None);
        cache.ObserveResponseSessionState("state-2");
        JmapSession cached = await cache.GetUsableSessionAsync(CancellationToken.None);
        JmapSession refreshed = await cache.RefreshSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cached, Is.SameAs(first));
            Assert.That(refreshed, Is.SameAs(second));
            Assert.That(cache.IsSessionStale(), Is.False);
            Assert.That(transport.SessionUrls, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task RefreshThatStillHasAnOlderStateRemainsStale()
    {
        RecordingTransport transport = new RecordingTransport();
        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("state-1")));
        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("state-1")));
        transport.SessionResults.Enqueue(Task.FromResult(ValidSession("state-2")));
        SessionCache cache = new SessionCache(Options("https://example.test/session"), transport);

        await cache.RefreshSessionAsync(CancellationToken.None);
        cache.ObserveResponseSessionState("state-2");
        await cache.GetUsableSessionAsync(CancellationToken.None);
        bool stillStale = cache.IsSessionStale();
        JmapSession current = await cache.GetUsableSessionAsync(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stillStale, Is.True);
            Assert.That(current.State, Is.EqualTo("state-2"));
            Assert.That(cache.IsSessionStale(), Is.False);
            Assert.That(transport.SessionUrls, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public async Task FailedAndCanceledRefreshesPreserveTheCachedSession()
    {
        RecordingTransport transport = new RecordingTransport();
        JmapSession original = ValidSession("state-1");
        transport.SessionResults.Enqueue(Task.FromResult(original));
        SessionCache cache = new SessionCache(Options("https://example.test/session"), transport);
        await cache.RefreshSessionAsync(CancellationToken.None);
        System.InvalidOperationException failure = new System.InvalidOperationException("offline");
        transport.SessionResults.Enqueue(Task.FromException<JmapSession>(failure));

        System.InvalidOperationException observedFailure = Assert.ThrowsAsync<System.InvalidOperationException>(
            async () => await cache.RefreshSessionAsync(CancellationToken.None))!;

        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        transport.SessionResults.Enqueue(Task.FromCanceled<JmapSession>(cancellation.Token));
        Task<JmapSession> canceledRefresh = cache.RefreshSessionAsync(cancellation.Token);
        Assert.That(async () => await canceledRefresh, Throws.TypeOf<TaskCanceledException>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedFailure, Is.SameAs(failure));
            Assert.That(canceledRefresh.IsCanceled, Is.True);
            Assert.That(cache.GetSession(), Is.SameAs(original));
            Assert.That(transport.SessionUrls, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public void MissingResponseSessionStateIsAProtocolFailure()
    {
        SessionCache cache = new SessionCache(
            Options("https://example.test/session"),
            new RecordingTransport());

        JmapProtocolException error = Assert.Throws<JmapProtocolException>(
            () => cache.ObserveResponseSessionState(null))!;

        Assert.That(error.Path, Is.EqualTo("sessionState"));
    }

    private static JmapClientOptions Options(string sessionUrl)
    {
        JmapClientOptions options = new JmapClientOptions();
        options.DiscoveryOptions.SessionUrl = sessionUrl;
        return options;
    }

    private static JmapSession ValidSession(string state)
    {
        JmapSession session = new JmapSession();
        session.Username = "user@example.test";
        session.ApiUrl = "https://jmap.example.test/api";
        session.DownloadUrl = "https://jmap.example.test/download/{accountId}/{blobId}/{name}?type={type}";
        session.UploadUrl = "https://jmap.example.test/upload/{accountId}";
        session.EventSourceUrl = "https://jmap.example.test/events?types={types}";
        session.State = state;
        session.Capabilities[JmapCapabilities.Core] = new JmapCapability
        {
            Core = new JmapCoreCapabilities()
        };
        return session;
    }

    private sealed class RecordingTransport : IJmapTransport
    {
        public Queue<Task<JmapSession>> SessionResults = new Queue<Task<JmapSession>>();
        public List<string> SessionUrls = new List<string>();
        public List<CancellationToken> CancellationTokens = new List<CancellationToken>();

        public Task<JmapSession> GetTransportSessionAsync(string sessionUrl, CancellationToken cancellationToken)
        {
            SessionUrls.Add(sessionUrl);
            CancellationTokens.Add(cancellationToken);
            return SessionResults.Dequeue();
        }

        public Task<JmapResponse> SendTransportApiRequestAsync(
            string apiUrl,
            JmapRequest request,
            CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }

        public Task<BlobUploadResponse> SendTransportUploadAsync(
            string uploadUrl,
            BlobUploadRequest request,
            CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }

        public Task<BlobDownloadResponse> SendTransportDownloadAsync(
            string downloadUrl,
            BlobDownloadRequest request,
            CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }

        public Task<Stream> OpenTransportEventSourceAsync(
            string eventSourceUrl,
            string? lastEventId,
            CancellationToken cancellationToken)
        {
            throw new System.NotSupportedException();
        }
    }
}
