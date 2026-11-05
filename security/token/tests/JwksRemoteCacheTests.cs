using System.Net;
using System.Net.Http;
using System.Text;
using token;

namespace token.Tests;

public class JwksRemoteCacheTests
{
    [Test]
    public async Task FreshCacheHitAvoidsNetworkAndReturnsIndependentKeys()
    {
        CacheHandler handler = new CacheHandler();
        handler.Body = KeySet("cached", 4);
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy policy = Policy();
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        byte[] firstSecret = first.Set.Keys[0].SymmetricKey;
        first.Set.ReleaseOwnedMaterial();
        JwksResult second = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(firstSecret, Is.All.Zero);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Set, Is.Not.SameAs(first.Set));
            Assert.That(second.Set.Keys[0].KeyId, Is.EqualTo("cached"));
            Assert.That(second.Set.Keys[0].SymmetricKey, Is.Not.All.Zero);
        });

        second.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task ExpiredFreshEntryIsRefreshed()
    {
        CacheHandler handler = new CacheHandler();
        handler.Body = KeySet("first", 1);
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 10;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        clock.Advance(11);
        handler.Body = KeySet("refreshed", 2);
        JwksResult refreshed = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(handler.RequestCount, Is.EqualTo(2));
            Assert.That(first.Set.Keys[0].KeyId, Is.EqualTo("first"));
            Assert.That(refreshed.Set.Keys[0].KeyId, Is.EqualTo("refreshed"));
        });

        first.Set.ReleaseOwnedMaterial();
        refreshed.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task RefreshErrorUsesStaleEntryOnlyWithinConfiguredWindow()
    {
        CacheHandler handler = new CacheHandler();
        handler.Body = KeySet("stale", 3);
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 10;
        policy.RemoteKeyStaleIfErrorSeconds = 20;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        clock.Advance(11);
        handler.Fault = true;
        JwksResult stale = await resolver.Resolve(uri, policy);
        clock.Advance(20);
        JwksResult expired = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Set.Keys[0].KeyId, Is.EqualTo("stale"));
            AssertFailure(expired, "remote_jwks_request_failed");
            Assert.That(handler.RequestCount, Is.EqualTo(3));
        });

        first.Set.ReleaseOwnedMaterial();
        stale.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task InvalidRefreshBodyFallsBackToStaleEntry()
    {
        CacheHandler handler = new CacheHandler();
        handler.Body = KeySet("valid", 5);
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 1;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        clock.Advance(2);
        handler.Body = "{\"keys\":[],\"keys\":[]}";
        JwksResult stale = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Set.Keys[0].KeyId, Is.EqualTo("valid"));
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        first.Set.ReleaseOwnedMaterial();
        stale.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task CacheEntryLimitUsesLeastRecentlyUsedEviction()
    {
        CacheHandler handler = new CacheHandler();
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy policy = Policy();
        policy.MaximumRemoteKeyCacheEntries = 2;
        Uri firstUri = new Uri("https://keys.example.test/one");
        Uri secondUri = new Uri("https://keys.example.test/two");
        Uri thirdUri = new Uri("https://keys.example.test/three");

        JwksResult first = await resolver.Resolve(firstUri, policy);
        JwksResult second = await resolver.Resolve(secondUri, policy);
        JwksResult firstHit = await resolver.Resolve(firstUri, policy);
        JwksResult third = await resolver.Resolve(thirdUri, policy);
        JwksResult secondAgain = await resolver.Resolve(secondUri, policy);

        Assert.That(handler.RequestCount, Is.EqualTo(4));

        first.Set.ReleaseOwnedMaterial();
        second.Set.ReleaseOwnedMaterial();
        firstHit.Set.ReleaseOwnedMaterial();
        third.Set.ReleaseOwnedMaterial();
        secondAgain.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task ZeroCacheLimitAndExplicitClearBothForceNewRequests()
    {
        CacheHandler handler = new CacheHandler();
        TestClock clock = new TestClock();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = JwksRemoteResolver.Create(client, clock);
        TokenPolicy disabled = Policy();
        disabled.MaximumRemoteKeyCacheEntries = 0;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, disabled);
        JwksResult second = await resolver.Resolve(uri, disabled);
        TokenPolicy enabled = Policy();
        JwksResult third = await resolver.Resolve(uri, enabled);
        resolver.ClearCache();
        resolver.ClearCache();
        JwksResult fourth = await resolver.Resolve(uri, enabled);

        Assert.That(handler.RequestCount, Is.EqualTo(4));

        first.Set.ReleaseOwnedMaterial();
        second.Set.ReleaseOwnedMaterial();
        third.Set.ReleaseOwnedMaterial();
        fourth.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task HostAllowlistUsesExactCaseInsensitiveDnsMatch()
    {
        CacheHandler handler = new CacheHandler();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy policy = Policy();
        policy.AllowedRemoteKeyHosts = new string[] { "KEYS.EXAMPLE.TEST" };

        JwksResult allowed = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            policy);
        JwksResult subdomain = await resolver.Resolve(
            new Uri("https://sub.keys.example.test/jwks"),
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True);
            AssertFailure(subdomain, "remote_jwks_host_not_allowed");
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        });

        allowed.Set.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task ResponseFromDifferentHostIsRejected()
    {
        CacheHandler handler = new CacheHandler();
        handler.ResponseUri = new Uri("https://untrusted.example.test/jwks");
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            Policy());

        AssertFailure(result, "remote_jwks_host_not_allowed");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ResponseSizeLimitRejectsKnownAndStreamingLengths(bool knownLength)
    {
        CacheHandler handler = new CacheHandler();
        handler.Body = KeySet("oversized", 8);
        handler.UnknownLength = !knownLength;
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy policy = Policy();
        policy.MaximumRemoteKeySetSizeInBytes = 24;

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            policy);

        AssertFailure(result, "remote_jwks_size_exceeded");
    }

    [Test]
    public async Task InvalidLimitPolicyFailsBeforeNetworkAccess()
    {
        CacheHandler handler = new CacheHandler();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy[] policies = new TokenPolicy[6];
        int index = 0;
        while (index < policies.Length)
        {
            policies[index] = Policy();
            index++;
        }

        policies[0].MaximumRemoteKeySetSizeInBytes = 0;
        policies[1].MaximumRemoteKeyCacheEntries = -1;
        policies[2].RemoteKeyRefreshSeconds = -1;
        policies[3].RemoteKeyStaleIfErrorSeconds = -1;
        policies[4].RemoteKeyTimeoutSeconds = 0;
        policies[5].AllowedRemoteKeyHosts = null;

        foreach (TokenPolicy policy in policies)
        {
            JwksResult result = await resolver.Resolve(
                new Uri("https://keys.example.test/jwks"),
                policy);
            AssertFailure(result, "invalid_remote_jwks_policy");
        }

        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public async Task RequestTimeoutReturnsStableFailure()
    {
        CacheHandler handler = new CacheHandler();
        handler.DelayMilliseconds = 5000;
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy policy = Policy();
        policy.RemoteKeyTimeoutSeconds = 1;

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            policy);

        AssertFailure(result, "remote_jwks_timeout");
    }

    static TokenPolicy Policy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowRemoteKeys = true;
        policy.AllowedRemoteKeyHosts = new string[] { "keys.example.test" };
        return policy;
    }

    static string KeySet(string keyId, byte value)
    {
        string key = Convert.ToBase64String(Enumerable.Repeat(value, 32).ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "{\"keys\":[{\"kty\":\"oct\",\"k\":\""
            + key + "\",\"kid\":\"" + keyId + "\"}]}";
    }

    static void AssertFailure(JwksResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Set, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
        });
    }

    class TestClock : ITokenClock
    {
        public DateTimeOffset Now = new DateTimeOffset(
            2026,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);

        public void Advance(int seconds)
        {
            Now = Now.AddSeconds(seconds);
        }

        public DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    class CacheHandler : HttpMessageHandler
    {
        public int RequestCount;
        public string Body = "{\"keys\":[]}";
        public bool Fault;
        public bool UnknownLength;
        public int DelayMilliseconds;
        public Uri ResponseUri;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (DelayMilliseconds > 0)
            {
                await Task.Delay(DelayMilliseconds, cancellationToken);
            }

            if (Fault)
            {
                throw new HttpRequestException("simulated refresh failure");
            }

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            if (ResponseUri != null)
            {
                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, ResponseUri);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(Body);
            if (UnknownLength)
            {
                response.Content = new UnknownLengthContent(bytes);
            }
            else
            {
                response.Content = new ByteArrayContent(bytes);
            }

            return response;
        }
    }

    class UnknownLengthContent : HttpContent
    {
        byte[] bytes;

        public UnknownLengthContent(byte[] bytes)
        {
            this.bytes = bytes;
        }

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext context)
        {
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
