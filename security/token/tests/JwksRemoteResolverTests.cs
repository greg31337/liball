using System.Net;
using System.Net.Http;
using System.Text;
using token;

namespace token.Tests;

public class JwksRemoteResolverTests
{
    [Test]
    public async Task ResolveFetchesAndParsesJwksWhenExplicitlyEnabled()
    {
        RecordingHandler handler = new RecordingHandler();
        handler.Body = "{\"keys\":[{\"kty\":\"oct\",\"k\":\""
            + B64(Enumerable.Repeat((byte)7, 32).ToArray())
            + "\",\"kid\":\"remote\"}]}";
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy policy = EnabledPolicy();
        Uri uri = new Uri("https://keys.example.test/jwks.json");

        JwksResult result = await resolver.Resolve(uri, policy);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Set.Keys, Has.Length.EqualTo(1));
            Assert.That(result.Set.Keys[0].KeyId, Is.EqualTo("remote"));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(handler.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(handler.Uri, Is.EqualTo(uri));
        });

        result.Set.ReleaseOwnedMaterial();
    }

    [Test]
    public async Task ResolveDoesNotSendRequestUnlessPolicyOptsIn()
    {
        RecordingHandler handler = new RecordingHandler();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        TokenPolicy policy = new TokenPolicy();

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            policy);

        Assert.Multiple(() =>
        {
            AssertFailure(result, "remote_keys_disabled");
            Assert.That(handler.RequestCount, Is.Zero);
        });
    }

    [TestCase("http://keys.example.test/jwks")]
    [TestCase("jwks.json")]
    public async Task ResolveRejectsNonHttpsOrRelativeUrisWithoutRequest(string value)
    {
        RecordingHandler handler = new RecordingHandler();
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);
        Uri uri = new Uri(value, UriKind.RelativeOrAbsolute);

        JwksResult result = await resolver.Resolve(uri, EnabledPolicy());

        Assert.Multiple(() =>
        {
            AssertFailure(result, "invalid_remote_jwks_uri");
            Assert.That(handler.RequestCount, Is.Zero);
        });
    }

    [Test]
    public async Task ResolveReturnsStableFailureForHttpErrors()
    {
        RecordingHandler handler = new RecordingHandler();
        handler.StatusCode = HttpStatusCode.ServiceUnavailable;
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            EnabledPolicy());

        AssertFailure(result, "remote_jwks_http_error");
    }

    [Test]
    public async Task ResolveReturnsStableFailureForRequestFaults()
    {
        RecordingHandler handler = new RecordingHandler();
        handler.Fault = true;
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            EnabledPolicy());

        AssertFailure(result, "remote_jwks_request_failed");
    }

    [Test]
    public async Task ResolveReturnsStableFailureForCancellation()
    {
        RecordingHandler handler = new RecordingHandler();
        handler.Cancel = true;
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        JwksResult result = await resolver.ResolveWithCancellation(
            new Uri("https://keys.example.test/jwks"),
            EnabledPolicy(),
            new CancellationToken(true));

        AssertFailure(result, "remote_jwks_canceled");
    }

    [Test]
    public async Task ResolvePropagatesStrictJwksParseFailures()
    {
        RecordingHandler handler = new RecordingHandler();
        handler.Body = "{\"keys\":[],\"keys\":[]}";
        using HttpClient client = new HttpClient(handler);
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        JwksResult result = await resolver.Resolve(
            new Uri("https://keys.example.test/jwks"),
            EnabledPolicy());

        AssertFailure(result, "duplicate_json_member");
    }

    [Test]
    public void ConstructorAndResolveRejectNullArguments()
    {
        using HttpClient client = new HttpClient(new RecordingHandler());
        JwksRemoteResolver resolver = new JwksRemoteResolver(client);

        Assert.Multiple(() =>
        {
            Assert.That(() => new JwksRemoteResolver(null), Throws.ArgumentNullException);
            Assert.That(() => JwksRemoteResolver.Create(client, null), Throws.ArgumentNullException);
            Assert.That(
                () => resolver.Resolve(null, EnabledPolicy()),
                Throws.ArgumentNullException);
            Assert.That(
                () => resolver.Resolve(new Uri("https://keys.example.test"), null),
                Throws.ArgumentNullException);
        });
    }

    static TokenPolicy EnabledPolicy()
    {
        TokenPolicy policy = new TokenPolicy();
        policy.AllowRemoteKeys = true;
        policy.AllowedRemoteKeyHosts = new string[] { "keys.example.test" };
        return policy;
    }

    static string B64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
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

    class RecordingHandler : HttpMessageHandler
    {
        public int RequestCount;
        public HttpMethod Method;
        public Uri Uri;
        public string Body = "{\"keys\":[]}";
        public HttpStatusCode StatusCode = HttpStatusCode.OK;
        public bool Fault;
        public bool Cancel;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            Uri = request.RequestUri;

            if (Fault)
            {
                return Task.FromException<HttpResponseMessage>(
                    new HttpRequestException("simulated request failure"));
            }

            if (Cancel || cancellationToken.IsCancellationRequested)
            {
                CancellationToken canceled = new CancellationToken(true);
                return Task.FromCanceled<HttpResponseMessage>(canceled);
            }

            HttpResponseMessage response = new HttpResponseMessage(StatusCode);
            response.Content = new StringContent(
                Body,
                Encoding.UTF8,
                "application/jwk-set+json");
            return Task.FromResult(response);
        }
    }
}
