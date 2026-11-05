using System.Net;
using System.Net.Http;

namespace jmap.Tests;

public class SessionRedirectHandlerTests
{
    [Test]
    public async Task SameOriginRelativeRedirectsRetainAuthorizationAndResolveLocations()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        script.Responses.Enqueue(Redirect(HttpStatusCode.Found, "../next?step=1"));
        script.Responses.Enqueue(Redirect(HttpStatusCode.TemporaryRedirect, "https://EXAMPLE.test/final"));
        script.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        RecordingAuthorizationProvider authorization = new RecordingAuthorizationProvider();
        SessionRedirectHandler redirects = Handler(script, authorization);
        JmapDiscoveryOptions options = new JmapDiscoveryOptions { MaximumRedirects = 2 };

        using HttpResponseMessage response = await redirects.GetSessionResponseAsync(
            "https://example.test/discovery/session",
            options,
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(script.RequestUris, Is.EqualTo(new[]
            {
                "https://example.test/discovery/session",
                "https://example.test/next?step=1",
                "https://example.test/final"
            }));
            Assert.That(script.AuthorizationValues, Is.EqualTo(new[]
            {
                "Bearer secret",
                "Bearer secret",
                "Bearer secret"
            }));
            Assert.That(authorization.ResourceUrls, Is.EqualTo(new[] { "https://example.test/discovery/session" }));
        }
    }

    [Test]
    public async Task AuthorizationIsPermanentlyRemovedAfterCrossOriginRedirect()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        script.Responses.Enqueue(Redirect(HttpStatusCode.MovedPermanently, "https://other.test/session"));
        script.Responses.Enqueue(Redirect(HttpStatusCode.SeeOther, "https://example.test/final"));
        script.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        SessionRedirectHandler redirects = Handler(script, new RecordingAuthorizationProvider());

        using HttpResponseMessage response = await redirects.GetSessionResponseAsync(
            "https://example.test/session",
            new JmapDiscoveryOptions { MaximumRedirects = 3 },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(script.AuthorizationValues, Is.EqualTo(new string?[]
            {
                "Bearer secret",
                null,
                null
            }));
        }
    }

    [Test]
    public void RedirectLimitIsExactAndIntermediateResponsesAreDisposed()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        TrackingContent firstContent = new TrackingContent();
        TrackingContent secondContent = new TrackingContent();
        HttpResponseMessage first = Redirect(HttpStatusCode.Found, "/second");
        first.Content = firstContent;
        HttpResponseMessage second = Redirect(HttpStatusCode.Found, "/third");
        second.Content = secondContent;
        script.Responses.Enqueue(first);
        script.Responses.Enqueue(second);
        SessionRedirectHandler redirects = Handler(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await redirects.GetSessionResponseAsync(
                "https://example.test/first",
                new JmapDiscoveryOptions { MaximumRedirects = 1 },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(script.RequestUris, Has.Count.EqualTo(2));
            Assert.That(firstContent.WasDisposed, Is.True);
            Assert.That(secondContent.WasDisposed, Is.True);
            Assert.That(error.HttpStatus, Is.EqualTo(302));
            Assert.That(error.HasHttpStatus, Is.True);
            Assert.That(error.Endpoint, Is.EqualTo("https://example.test/second"));
        }
    }

    [Test]
    public void RedirectLoopIsRejectedBeforeRepeatingARequest()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        script.Responses.Enqueue(Redirect(HttpStatusCode.PermanentRedirect, "/session"));
        SessionRedirectHandler redirects = Handler(script, null);

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await redirects.GetSessionResponseAsync(
                "https://example.test/session",
                new JmapDiscoveryOptions { MaximumRedirects = 5 },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Message, Does.Contain("loop"));
            Assert.That(script.RequestUris, Has.Count.EqualTo(1));
        }
    }

    [TestCase("http://example.test/insecure")]
    [TestCase("ftp://example.test/session")]
    [TestCase("https://user:password@example.test/session")]
    [TestCase("https://example.test/session#fragment")]
    public void UnsafeRedirectTargetsAreRejected(string location)
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        script.Responses.Enqueue(Redirect(HttpStatusCode.Found, location));
        SessionRedirectHandler redirects = Handler(script, new RecordingAuthorizationProvider());
        JmapDiscoveryOptions options = new JmapDiscoveryOptions
        {
            MaximumRedirects = 2,
            AllowInsecureHttp = true
        };

        JmapTransportException error = Assert.ThrowsAsync<JmapTransportException>(
            async () => await redirects.GetSessionResponseAsync(
                "https://example.test/session",
                options,
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.IsRetryable, Is.False);
            Assert.That(error.Endpoint, Does.Not.Contain("password"));
            Assert.That(error.Endpoint, Does.Not.Contain("fragment"));
            Assert.That(script.RequestUris, Has.Count.EqualTo(1));
            Assert.That(script.AuthorizationValues[0], Is.EqualTo("Bearer secret"));
        }
    }

    [Test]
    public async Task InsecureHttpIsAllowedOnlyWhenExplicitlyConfigured()
    {
        ScriptedHttpHandler allowedScript = new ScriptedHttpHandler();
        allowedScript.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK));
        SessionRedirectHandler allowed = Handler(allowedScript, null);

        using HttpResponseMessage response = await allowed.GetSessionResponseAsync(
            "http://localhost/session",
            new JmapDiscoveryOptions { AllowInsecureHttp = true },
            CancellationToken.None);
        SessionRedirectHandler rejected = Handler(new ScriptedHttpHandler(), null);
        JmapTransportException error = Assert.Throws<JmapTransportException>(
            () => rejected.GetSessionResponseAsync(
                "http://localhost/session",
                new JmapDiscoveryOptions(),
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(error.Endpoint, Is.EqualTo("http://localhost/session"));
        }
    }

    [Test]
    public async Task NonRedirectStatusIsReturnedEvenWhenLocationHeaderIsPresent()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        HttpResponseMessage notModified = new HttpResponseMessage(HttpStatusCode.NotModified);
        notModified.Headers.Location = new Uri("https://other.test/session");
        script.Responses.Enqueue(notModified);
        SessionRedirectHandler redirects = Handler(script, null);

        using HttpResponseMessage response = await redirects.GetSessionResponseAsync(
            "https://example.test/session",
            new JmapDiscoveryOptions { MaximumRedirects = 5 },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response, Is.SameAs(notModified));
            Assert.That(script.RequestUris, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void MissingLocationAndNegativeLimitAreRejected()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        script.Responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Found));
        SessionRedirectHandler redirects = Handler(script, null);

        JmapTransportException missingLocation = Assert.ThrowsAsync<JmapTransportException>(
            async () => await redirects.GetSessionResponseAsync(
                "https://example.test/session",
                new JmapDiscoveryOptions { MaximumRedirects = 1 },
                CancellationToken.None))!;
        JmapValidationException negativeLimit = Assert.Throws<JmapValidationException>(
            () => redirects.GetSessionResponseAsync(
                "https://example.test/session",
                new JmapDiscoveryOptions { MaximumRedirects = -1 },
                CancellationToken.None))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missingLocation.HttpStatus, Is.EqualTo(302));
            Assert.That(negativeLimit.InvalidProperties, Is.EqualTo(new[] { "maximumRedirects" }));
        }
    }

    [Test]
    public void CancellationBeforeDispatchReturnsCanceledTask()
    {
        ScriptedHttpHandler script = new ScriptedHttpHandler();
        SessionRedirectHandler redirects = Handler(script, new RecordingAuthorizationProvider());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<HttpResponseMessage> request = redirects.GetSessionResponseAsync(
            "https://example.test/session",
            new JmapDiscoveryOptions(),
            cancellation.Token);

        Assert.That(async () => await request, Throws.InstanceOf<OperationCanceledException>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.IsCanceled, Is.True);
            Assert.That(script.RequestUris, Is.Empty);
        }
    }

    private static SessionRedirectHandler Handler(
        ScriptedHttpHandler script,
        IJmapAuthorizationProvider? authorizationProvider)
    {
        HttpClient client = new HttpClient(script);
        return new SessionRedirectHandler(client, authorizationProvider);
    }

    private static HttpResponseMessage Redirect(HttpStatusCode status, string location)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }

    private sealed class ScriptedHttpHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses = new Queue<HttpResponseMessage>();
        public List<string> RequestUris = new List<string>();
        public List<string?> AuthorizationValues = new List<string?>();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.AbsoluteUri);
            AuthorizationValues.Add(request.Headers.Authorization?.ToString());
            return Task.FromResult(Responses.Dequeue());
        }
    }

    private sealed class RecordingAuthorizationProvider : IJmapAuthorizationProvider
    {
        public List<string> ResourceUrls = new List<string>();

        public Task<string> GetAuthorizationValueAsync(string resourceUrl, CancellationToken cancellationToken)
        {
            ResourceUrls.Add(resourceUrl);
            return Task.FromResult("Bearer secret");
        }
    }

    private sealed class TrackingContent : ByteArrayContent
    {
        public bool WasDisposed;

        public TrackingContent() : base(new byte[] { 1 })
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
