using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using token;

namespace token.Tests;

public class RemoteJwksResolverPublicTests
{
    [Test]
    public async Task PublicResolverRequiresExplicitOptInAndExactHttpsHost()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy disabledPolicy = new TokenPolicy();
        TokenPolicy enabledPolicy = Policy();
        enabledPolicy.AllowedRemoteKeyHosts = new string[] { "KEYS.EXAMPLE.TEST" };

        JwksResult disabled = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            disabledPolicy);
        JwksResult http = await resolver.Resolve(
            new Uri("http://keys.example.test/jwks"),
            enabledPolicy);
        JwksResult relative = await resolver.Resolve(
            new Uri("jwks.json", UriKind.Relative),
            enabledPolicy);
        JwksResult otherHost = await resolver.Resolve(
            new Uri("https://other.example.test/jwks"),
            enabledPolicy);
        JwksResult subdomain = await resolver.Resolve(
            new Uri("https://sub.keys.example.test/jwks"),
            enabledPolicy);
        handler.ResponseUri = new Uri("https://redirect.example.test/jwks");
        JwksResult redirected = await resolver.Resolve(
            new Uri("https://keys.example.test/redirect"),
            enabledPolicy);
        handler.ResponseUri = null;
        JwksResult allowed = await resolver.Resolve(
            new Uri("https://keys.example.test/allowed"),
            enabledPolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(disabled, "remote_keys_disabled");
            AssertFailure(http, "invalid_remote_jwks_uri");
            AssertFailure(relative, "invalid_remote_jwks_uri");
            AssertFailure(otherHost, "remote_jwks_host_not_allowed");
            AssertFailure(subdomain, "remote_jwks_host_not_allowed");
            AssertFailure(redirected, "remote_jwks_host_not_allowed");
            Assert.That(allowed.Success, Is.True);
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        allowed.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverCachesIndependentResultsAndSupportsExplicitClear()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.Body = KeySet("first", 3);
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy policy = Policy();
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        byte[] firstSecret = first.Set!.Keys[0].SymmetricKey!;
        first.Set.ReleaseOwnedMaterial();
        JwksResult cached = await resolver.Resolve(uri, policy);
        resolver.ClearCache();
        handler.Body = KeySet("second", 4);
        JwksResult refreshed = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(firstSecret, Is.All.Zero);
            Assert.That(cached.Success, Is.True);
            Assert.That(cached.Set, Is.Not.SameAs(first.Set));
            Assert.That(cached.Set!.Keys[0].KeyId, Is.EqualTo("first"));
            Assert.That(cached.Set.Keys[0].SymmetricKey, Is.Not.All.Zero);
            Assert.That(refreshed.Set!.Keys[0].KeyId, Is.EqualTo("second"));
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        cached.Set!.ReleaseOwnedMaterial();
        refreshed.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverUsesStaleKeysOnlyInsideErrorFallbackPolicy()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.Body = KeySet("stale", 5);
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 0;
        policy.RemoteKeyStaleIfErrorSeconds = 30;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        handler.Fault = true;
        JwksResult stale = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Set!.Keys[0].KeyId, Is.EqualTo("stale"));
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        first.Set!.ReleaseOwnedMaterial();
        stale.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverDoesNotUseStaleKeysAfterTheConfiguredWindow()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.Body = KeySet("expired", 7);
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 0;
        policy.RemoteKeyStaleIfErrorSeconds = 0;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult first = await resolver.Resolve(uri, policy);
        await Task.Delay(20);
        handler.Fault = true;
        JwksResult expired = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            AssertFailure(expired, "remote_jwks_request_failed");
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        first.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverEnforcesResponseAndCacheLimits()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.Body = KeySet("oversized", 6);
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy sizePolicy = Policy();
        sizePolicy.MaximumRemoteKeySetSizeInBytes = 24;
        TokenPolicy noCachePolicy = Policy();
        noCachePolicy.MaximumRemoteKeyCacheEntries = 0;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult oversized = await resolver.Resolve(uri, sizePolicy);
        handler.UnknownLength = true;
        JwksResult streamedOversized = await resolver.Resolve(uri, sizePolicy);
        handler.UnknownLength = false;
        JwksResult first = await resolver.Resolve(uri, noCachePolicy);
        JwksResult second = await resolver.Resolve(uri, noCachePolicy);

        Assert.Multiple(() =>
        {
            AssertFailure(oversized, "remote_jwks_size_exceeded");
            AssertFailure(streamedOversized, "remote_jwks_size_exceeded");
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(handler.RequestCount, Is.EqualTo(4));
        });

        first.Set!.ReleaseOwnedMaterial();
        second.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverUsesLeastRecentlyUsedCacheEviction()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
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

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(firstHit.Success, Is.True);
            Assert.That(third.Success, Is.True);
            Assert.That(secondAgain.Success, Is.True);
            Assert.That(handler.RequestCount, Is.EqualTo(4));
        });

        first.Set!.ReleaseOwnedMaterial();
        second.Set!.ReleaseOwnedMaterial();
        firstHit.Set!.ReleaseOwnedMaterial();
        third.Set!.ReleaseOwnedMaterial();
        secondAgain.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverRefreshesRotatedKeysAndExposesAmbiguousSelection()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.Body = KeySet("old", 1);
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        ITokenKeyService keys = new TokenKeyService();
        TokenPolicy policy = Policy();
        policy.RemoteKeyRefreshSeconds = 0;
        Uri uri = new Uri("https://keys.example.test/jwks");

        JwksResult oldSet = await resolver.Resolve(uri, policy);
        handler.Body = KeySet("new", 2);
        JwksResult newSet = await resolver.Resolve(uri, policy);
        handler.Body = AmbiguousKeySet("shared", 3, 4);
        JwksResult ambiguousSet = await resolver.Resolve(uri, policy);
        TokenKeyQuery query = new TokenKeyQuery();
        query.KeyId = "shared";
        TokenKeyResult ambiguous = keys.SelectKey(ambiguousSet.Set!, query);

        Assert.Multiple(() =>
        {
            Assert.That(oldSet.Set!.Keys[0].KeyId, Is.EqualTo("old"));
            Assert.That(newSet.Set!.Keys[0].KeyId, Is.EqualTo("new"));
            Assert.That(ambiguousSet.Set!.Keys, Has.Length.EqualTo(2));
            Assert.That(ambiguous.Error!.Code, Is.EqualTo("ambiguous_jwks_key"));
            Assert.That(handler.RequestCount, Is.EqualTo(3));
        });

        oldSet.Set!.ReleaseOwnedMaterial();
        newSet.Set!.ReleaseOwnedMaterial();
        ambiguousSet.Set!.ReleaseOwnedMaterial();
        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverReturnsStableTimeoutAndHttpFailures()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        handler.DelayMilliseconds = 5000;
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy policy = Policy();
        policy.RemoteKeyTimeoutSeconds = 1;

        JwksResult timeout = await resolver.Resolve(
            new Uri("https://keys.example.test/timeout"),
            policy);
        handler.DelayMilliseconds = 0;
        handler.StatusCode = HttpStatusCode.ServiceUnavailable;
        JwksResult http = await resolver.Resolve(
            new Uri("https://keys.example.test/http-error"),
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(timeout, "remote_jwks_timeout");
            AssertFailure(http, "remote_jwks_http_error");
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });

        resolver.ClearCache();
    }

    [Test]
    public async Task PublicResolverRejectsInvalidPoliciesBeforeNetworkAccess()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        TokenPolicy[] policies = new TokenPolicy[6];
        int index = 0;
        while (index < policies.Length)
        {
            policies[index] = Policy();
            index++;
        }

        policies[0].AllowedRemoteKeyHosts = null;
        policies[1].MaximumRemoteKeySetSizeInBytes = 0;
        policies[2].MaximumRemoteKeyCacheEntries = -1;
        policies[3].RemoteKeyRefreshSeconds = -1;
        policies[4].RemoteKeyStaleIfErrorSeconds = -1;
        policies[5].RemoteKeyTimeoutSeconds = 0;

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
    public async Task PublicResolverReturnsStableCancellationAndRequestFailures()
    {
        PublicJwksHandler handler = new PublicJwksHandler();
        using HttpClient client = new HttpClient(handler);
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        JwksResult canceled = await resolver.ResolveWithCancellation(
            new Uri("https://keys.example.test/jwks"),
            Policy(),
            source.Token);
        handler.Fault = true;
        JwksResult faulted = await resolver.Resolve(
            new Uri("https://keys.example.test/other"),
            Policy());

        Assert.Multiple(() =>
        {
            AssertFailure(canceled, "remote_jwks_canceled");
            AssertFailure(faulted, "remote_jwks_request_failed");
        });

        source.Dispose();
        resolver.ClearCache();
    }

    [Test]
    public void PublicResolverRejectsNullArguments()
    {
        using HttpClient client = new HttpClient(new PublicJwksHandler());
        IRemoteJwksResolver resolver = new RemoteJwksResolver(client);

        Assert.Multiple(() =>
        {
            Assert.That(() => new RemoteJwksResolver(null!), Throws.ArgumentNullException);
            Assert.That(
                () => resolver.Resolve(null!, Policy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => resolver.Resolve(
                    new Uri("https://keys.example.test/jwks"),
                    null!),
                Throws.ArgumentNullException);
        });
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
        byte[] keyBytes = new byte[32];
        int index = 0;
        while (index < keyBytes.Length)
        {
            keyBytes[index] = value;
            index++;
        }

        string key = Convert.ToBase64String(keyBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "{\"keys\":[{\"kty\":\"oct\",\"k\":\""
            + key + "\",\"kid\":\"" + keyId + "\"}]}";
    }

    static string AmbiguousKeySet(string keyId, byte firstValue, byte secondValue)
    {
        string first = Convert.ToBase64String(
                Enumerable.Repeat(firstValue, 32).ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string second = Convert.ToBase64String(
                Enumerable.Repeat(secondValue, 32).ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "{\"keys\":[{\"kty\":\"oct\",\"k\":\""
            + first + "\",\"kid\":\"" + keyId + "\"},{\"kty\":\"oct\",\"k\":\""
            + second + "\",\"kid\":\"" + keyId + "\"}]}";
    }

    static void AssertFailure(JwksResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Set, Is.Null);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.Code, Is.EqualTo(code));
        });
    }

    class PublicJwksHandler : HttpMessageHandler
    {
        public int RequestCount;
        public string Body = "{\"keys\":[]}";
        public bool Fault;
        public bool UnknownLength;
        public int DelayMilliseconds;
        public HttpStatusCode StatusCode = HttpStatusCode.OK;
        public Uri? ResponseUri;

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
                throw new HttpRequestException("simulated request failure");
            }

            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage response = new HttpResponseMessage(StatusCode);
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
            TransportContext? context)
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
