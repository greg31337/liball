using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class RedirectPolicyTests
{
    [Test]
    public void AutomaticHandlerRedirectsAreDisabled()
    {
        using SocketsHttpHandler handler = new SocketsHttpHandler { AllowAutoRedirect = true };

        RedirectPolicy.DisableAutomaticRedirects(handler);

        Assert.That(handler.AllowAutoRedirect, Is.False);
    }

    [Test]
    public void RelativeLocationIsResolvedAgainstTheCurrentRequest()
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/v1/folder/item?old=1");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.Found, "../next?new=2");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.RequestUri, Is.EqualTo(new Uri("https://api.example.test/v1/next?new=2")));
            Assert.That(state.CurrentUri, Is.EqualTo(next.RequestUri));
            Assert.That(state.HopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SameOriginRedirectRetainsCredentialsAndSafeHeaders()
    {
        using HttpRequestMessage current = CreateCredentialedRequest("https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.TemporaryRedirect, "/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.Headers.Authorization?.Parameter, Is.EqualTo("bearer-secret"));
            Assert.That(next.Headers.Contains("Proxy-Authorization"), Is.True);
            Assert.That(next.Headers.Contains("Cookie"), Is.True);
            Assert.That(next.Headers.Contains(UsptoHeaders.OdpApiKey), Is.True);
            Assert.That(next.Headers.Contains("X-Correlation-ID"), Is.True);
        });
    }

    [Test]
    public void CrossHostRedirectStripsCredentialsButRetainsSafeHeaders()
    {
        using HttpRequestMessage current = CreateCredentialedRequest("https://api.example.test/start");
        current.Headers.Host = "api.example.test";
        current.Content = new StringContent("payload", Encoding.UTF8, UsptoMediaTypes.Json);
        current.Content.Headers.TryAddWithoutValidation(UsptoHeaders.TsdrApiKey, "content-secret");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.TemporaryRedirect, "https://download.example.test/file");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.Headers.Authorization, Is.Null);
            Assert.That(next.Headers.Contains("Proxy-Authorization"), Is.False);
            Assert.That(next.Headers.Contains("Cookie"), Is.False);
            Assert.That(next.Headers.Contains(UsptoHeaders.OdpApiKey), Is.False);
            Assert.That(next.Headers.Contains("X-Correlation-ID"), Is.True);
            Assert.That(next.Headers.Host, Is.Null);
            Assert.That(next.Content!.Headers.Contains(UsptoHeaders.TsdrApiKey), Is.False);
        });
    }

    [Test]
    public void SchemeRelativeCrossHostRedirectStripsCredentials()
    {
        using HttpRequestMessage current = CreateCredentialedRequest("https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.Found, "//download.example.test/file");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.RequestUri, Is.EqualTo(new Uri("https://download.example.test/file")));
            Assert.That(next.Headers.Authorization, Is.Null);
            Assert.That(next.Headers.Contains("Proxy-Authorization"), Is.False);
            Assert.That(next.Headers.Contains("Cookie"), Is.False);
            Assert.That(next.Headers.Contains(UsptoHeaders.OdpApiKey), Is.False);
            Assert.That(next.Headers.Contains("X-Correlation-ID"), Is.True);
            Assert.That(state.HopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PortChangeIsTreatedAsACrossOriginRedirect()
    {
        using HttpRequestMessage current = CreateCredentialedRequest("https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.TemporaryRedirect, "https://api.example.test:8443/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.That(next.Headers.Authorization, Is.Null);
    }

    [Test]
    public void HttpsDowngradeIsRejectedWithoutAdvancingTheChain()
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.Found, "http://api.example.test/insecure");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        ApiException exception = Assert.Throws<ApiException>(() => RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_HTTPS_REQUIRED"));
            Assert.That(exception.IsRetryable, Is.False);
            Assert.That(state.HopCount, Is.Zero);
        });
    }

    [Test]
    public void InitialRequestMustUseHttps()
    {
        Uri initial = new Uri("http://api.example.test/start");

        ApiException exception = Assert.Throws<ApiException>(() => new RedirectState(ApiService.Odp, initial, 5));

        Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_HTTPS_REQUIRED"));
    }

    [Test]
    public void HopLimitIsEnforcedAfterTheConfiguredNumberOfRedirects()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/one");
        RedirectState state = new RedirectState(ApiService.Odp, first.RequestUri!, 1);
        using HttpResponseMessage firstResponse = CreateRedirect(HttpStatusCode.Found, "/two");
        using HttpRequestMessage second = RedirectPolicy.CreateRedirectRequest(first, firstResponse, state, CancellationToken.None);
        using HttpResponseMessage secondResponse = CreateRedirect(HttpStatusCode.Found, "/three");

        ApiException exception = Assert.Throws<ApiException>(() => RedirectPolicy.CreateRedirectRequest(second, secondResponse, state, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_LIMIT_EXCEEDED"));
            Assert.That(exception.IsRetryable, Is.False);
            Assert.That(state.HopCount, Is.EqualTo(1));
            Assert.That(state.CurrentUri, Is.EqualTo(second.RequestUri));
        });
    }

    [Test]
    public void RedirectLoopIsDetectedBeforeAnotherRequestIsCreated()
    {
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/one");
        RedirectState state = new RedirectState(ApiService.Odp, first.RequestUri!, 5);
        using HttpResponseMessage firstResponse = CreateRedirect(HttpStatusCode.Found, "/two");
        using HttpRequestMessage second = RedirectPolicy.CreateRedirectRequest(first, firstResponse, state, CancellationToken.None);
        using HttpResponseMessage secondResponse = CreateRedirect(HttpStatusCode.Found, "/one");

        ApiException exception = Assert.Throws<ApiException>(() => RedirectPolicy.CreateRedirectRequest(second, secondResponse, state, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_LOOP"));
            Assert.That(exception.IsRetryable, Is.False);
            Assert.That(state.HopCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RedirectWithoutLocationIsRejected()
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/start");
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.Found);
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        ApiException exception = Assert.Throws<ApiException>(() => RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_LOCATION_MISSING"));
    }

    [Test]
    public void UnsupportedRedirectStatusIsRejected()
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.NotModified, "/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        ValidationException exception = Assert.Throws<ValidationException>(() => RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_REDIRECT_STATUS"));
    }

    [TestCase(HttpStatusCode.MovedPermanently)]
    [TestCase(HttpStatusCode.Found)]
    [TestCase(HttpStatusCode.SeeOther)]
    public void PostIsConvertedToGetForRedirectStatusesThatRequireIt(HttpStatusCode status)
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Post, "https://api.example.test/start");
        current.Content = new StringContent("request body", Encoding.UTF8, UsptoMediaTypes.Json);
        using HttpResponseMessage response = CreateRedirect(status, "/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(next.Content, Is.Null);
        });
    }

    [TestCase(HttpStatusCode.TemporaryRedirect)]
    [TestCase(HttpStatusCode.PermanentRedirect)]
    public void MethodPreservingRedirectsCloneTheRequestBody(HttpStatusCode status)
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Post, "https://api.example.test/start");
        current.Content = new StringContent("request body", Encoding.UTF8, UsptoMediaTypes.Json);
        current.Content.Headers.TryAddWithoutValidation("X-Content-Metadata", "preserved");
        using HttpResponseMessage response = CreateRedirect(status, "/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);

        using HttpRequestMessage next = RedirectPolicy.CreateRedirectRequest(current, response, state, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(next.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(next.Content, Is.Not.SameAs(current.Content));
            Assert.That(next.Content!.ReadAsStringAsync().GetAwaiter().GetResult(), Is.EqualTo("request body"));
            Assert.That(next.Content.Headers.Contains("X-Content-Metadata"), Is.True);
        });
    }

    [Test]
    public void CancellationBeforeRedirectCreationPreservesTheCallerTokenAndState()
    {
        using HttpRequestMessage current = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/start");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.Found, "/next");
        RedirectState state = new RedirectState(ApiService.Odp, current.RequestUri!, 5);
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => RedirectPolicy.CreateRedirectRequest(current, response, state, caller.Token));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(state.HopCount, Is.Zero);
        });
    }

    [Test]
    public void RedirectStateCannotBeReusedForAnUnrelatedRequest()
    {
        RedirectState state = new RedirectState(ApiService.Odp, new Uri("https://api.example.test/start"), 5);
        using HttpRequestMessage unrelated = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/other");
        using HttpResponseMessage response = CreateRedirect(HttpStatusCode.Found, "/next");

        ValidationException exception = Assert.Throws<ValidationException>(() => RedirectPolicy.CreateRedirectRequest(unrelated, response, state, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("REDIRECT_STATE_MISMATCH"));
    }

    [TestCase(300, false)]
    [TestCase(301, true)]
    [TestCase(302, true)]
    [TestCase(303, true)]
    [TestCase(304, false)]
    [TestCase(307, true)]
    [TestCase(308, true)]
    public void OnlySupportedStatusCodesAreClassifiedAsRedirects(int statusCode, bool expected)
    {
        Assert.That(RedirectPolicy.IsRedirectStatus(statusCode), Is.EqualTo(expected));
    }

    private static HttpRequestMessage CreateCredentialedRequest(string uri)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "bearer-secret");
        request.Headers.TryAddWithoutValidation("Proxy-Authorization", "Basic proxy-secret");
        request.Headers.TryAddWithoutValidation("Cookie", "session=cookie-secret");
        request.Headers.TryAddWithoutValidation(UsptoHeaders.OdpApiKey, "api-secret");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "safe-correlation");
        return request;
    }

    private static HttpResponseMessage CreateRedirect(HttpStatusCode status, string location)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    }
}
