using System.Net;
using uspto;

namespace uspto.Tests;

public class RequestHeaderPolicyTests
{
    [TestCase(ApiService.Odp, UsptoHeaders.OdpApiKey, "odp-key")]
    [TestCase(ApiService.OfficeAction, UsptoHeaders.OdpApiKey, "odp-key")]
    [TestCase(ApiService.Tsdr, UsptoHeaders.TsdrApiKey, "tsdr-key")]
    [TestCase(ApiService.TsdrPdfAndZip, UsptoHeaders.TsdrApiKey, "tsdr-key")]
    [TestCase(ApiService.PatentsView, UsptoHeaders.PatentsViewApiKey, "patentsview-key")]
    public void ExplicitCredentialsUseTheServiceSpecificHeader(ApiService service, string expectedHeader, string expectedKey)
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpApiKey = "odp-key",
            TsdrApiKey = "tsdr-key",
            PatentsViewApiKey = "patentsview-key"
        };
        DocumentFormat format = service == ApiService.TsdrPdfAndZip ? DocumentFormat.Pdf : DocumentFormat.Json;
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        RequestHeaderPolicy.Apply(request, service, options, false, format);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.GetValues(expectedHeader).Single(), Is.EqualTo(expectedKey));
            Assert.That(request.Headers.Accept.Single().MediaType, Is.EqualTo(format == DocumentFormat.Pdf ? UsptoMediaTypes.Pdf : UsptoMediaTypes.Json));
            Assert.That(request.Headers.UserAgent.ToString(), Is.EqualTo(options.UserAgent));
        });
    }

    [Test]
    public void CredentialProviderIsConsultedForEveryRequestSoKeysCanRotate()
    {
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = "first-key" };
        UsptoClientOptions options = new UsptoClientOptions { ApiKeyProvider = provider };
        using HttpRequestMessage first = new HttpRequestMessage(HttpMethod.Get, "https://example.test/first");
        using HttpRequestMessage second = new HttpRequestMessage(HttpMethod.Get, "https://example.test/second");

        RequestHeaderPolicy.Apply(first, ApiService.Odp, options, false, DocumentFormat.Json);
        provider.CurrentKey = "rotated-key";
        RequestHeaderPolicy.Apply(second, ApiService.Odp, options, false, DocumentFormat.Json);

        Assert.Multiple(() =>
        {
            Assert.That(first.Headers.GetValues(UsptoHeaders.OdpApiKey).Single(), Is.EqualTo("first-key"));
            Assert.That(second.Headers.GetValues(UsptoHeaders.OdpApiKey).Single(), Is.EqualTo("rotated-key"));
            Assert.That(provider.CallCount, Is.EqualTo(2));
            Assert.That(provider.LastRequest?.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(provider.LastRequest?.AllowAnonymous, Is.False);
        });
    }

    [TestCase(ApiService.Odp, UsptoHeaders.OdpApiKey, DocumentFormat.Json)]
    [TestCase(ApiService.OfficeAction, UsptoHeaders.OdpApiKey, DocumentFormat.Json)]
    [TestCase(ApiService.Tsdr, UsptoHeaders.TsdrApiKey, DocumentFormat.Json)]
    [TestCase(ApiService.TsdrPdfAndZip, UsptoHeaders.TsdrApiKey, DocumentFormat.Pdf)]
    [TestCase(ApiService.PatentsView, UsptoHeaders.PatentsViewApiKey, DocumentFormat.Json)]
    public void ProviderCredentialsUseTheExactHeaderForEveryService(ApiService service, string expectedHeader, DocumentFormat format)
    {
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = "provider-key" };
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        RequestHeaderPolicy.Apply(request, service, new UsptoClientOptions { ApiKeyProvider = provider }, false, format);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.GetValues(expectedHeader).Single(), Is.EqualTo("provider-key"));
            Assert.That(request.Headers.Count(header => header.Key == UsptoHeaders.OdpApiKey || header.Key == UsptoHeaders.TsdrApiKey || header.Key == UsptoHeaders.PatentsViewApiKey), Is.EqualTo(1));
            Assert.That(provider.LastRequest?.Service, Is.EqualTo(service));
            Assert.That(provider.LastRequest?.AllowAnonymous, Is.False);
        });
    }

    [Test]
    public void ProviderCredentialIsRedactedFromSuccessMetadataAndRetainedRawResponse()
    {
        const string secret = "rotated-provider-secret";
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = secret };
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource?note=" + Uri.EscapeDataString(secret));
        RequestHeaderPolicy.Apply(request, ApiService.Odp, new UsptoClientOptions { ApiKeyProvider = provider }, false, DocumentFormat.Json);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            ReasonPhrase = "accepted " + secret,
            Content = new StringContent("{\"message\":\"" + secret + "\"}", System.Text.Encoding.UTF8, UsptoMediaTypes.Json)
        };
        response.Headers.TryAddWithoutValidation("X-Diagnostic", "credential " + secret);
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            RetainRawResponse = true,
            MaximumRawResponseBytes = 4096
        };

        BufferedHttpResponse buffered = ResponseReader.ReadTextResponse(response, DocumentFormat.Json, context, new UsptoLimits(), Array.Empty<string>());

        Assert.Multiple(() =>
        {
            Assert.That(buffered.Text, Does.Contain(secret));
            Assert.That(buffered.RawText, Does.Contain(SecretRedactor.Replacement));
            Assert.That(buffered.RawText, Does.Not.Contain(secret));
            Assert.That(buffered.Metadata.ReasonPhrase, Does.Not.Contain(secret));
            Assert.That(buffered.Metadata.Headers["X-Diagnostic"].Single(), Does.Not.Contain(secret));
            Assert.That(buffered.Metadata.RequestUri?.AbsoluteUri, Does.Not.Contain(Uri.EscapeDataString(secret)));
        });
    }

    [Test]
    public void ProviderCredentialIsRedactedFromEveryErrorField()
    {
        const string secret = "environment-provider-secret";
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = secret };
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource/" + secret);
        RequestHeaderPolicy.Apply(request, ApiService.PatentsView, new UsptoClientOptions { ApiKeyProvider = provider }, false, DocumentFormat.Json);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            RequestMessage = request,
            ReasonPhrase = "rejected " + secret,
            Content = new StringContent("{\"code\":\"" + secret + "\",\"message\":\"" + secret + "\",\"details\":\"" + secret + "\",\"requestId\":\"" + secret + "\"}")
        };
        response.Headers.TryAddWithoutValidation("X-Diagnostic", secret);

        ApiException failure = (ApiException)ErrorMapper.MapResponse(response, ApiService.PatentsView, "resource-" + secret, new UsptoLimits(), Array.Empty<string>(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(failure.ErrorDetails, Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(failure.RequestIdentifier, Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(failure.ToString(), Does.Not.Contain(secret));
        });
    }

    [Test]
    public void ExplicitCredentialTakesPrecedenceOverProvider()
    {
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider { CurrentKey = "provider-key" };
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpApiKey = "explicit-key",
            ApiKeyProvider = provider
        };
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        RequestHeaderPolicy.Apply(request, ApiService.OfficeAction, options, false, DocumentFormat.Json);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.GetValues(UsptoHeaders.OdpApiKey).Single(), Is.EqualTo("explicit-key"));
            Assert.That(provider.CallCount, Is.Zero);
        });
    }

    [Test]
    public void AnonymousRequestOmitsCredentialsAndReplacesStaleServiceHeaders()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        request.Headers.TryAddWithoutValidation(UsptoHeaders.OdpApiKey, "stale-odp-key");
        request.Headers.TryAddWithoutValidation(UsptoHeaders.TsdrApiKey, "stale-tsdr-key");
        request.Headers.TryAddWithoutValidation(UsptoHeaders.UserAgent, "stale-agent/1.0");
        request.Headers.Accept.ParseAdd(UsptoMediaTypes.Text);

        RequestHeaderPolicy.Apply(request, ApiService.Tsdr, options, true, DocumentFormat.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.Contains(UsptoHeaders.OdpApiKey), Is.False);
            Assert.That(request.Headers.Contains(UsptoHeaders.TsdrApiKey), Is.False);
            Assert.That(request.Headers.Accept.Single().MediaType, Is.EqualTo(UsptoMediaTypes.Xml));
            Assert.That(request.Headers.UserAgent.ToString(), Is.EqualTo(options.UserAgent));
        });
    }

    [TestCase(ApiService.Odp, DocumentFormat.Unknown, UsptoMediaTypes.Json)]
    [TestCase(ApiService.OfficeAction, DocumentFormat.Json, UsptoMediaTypes.Json)]
    [TestCase(ApiService.Tsdr, DocumentFormat.Unknown, UsptoMediaTypes.Json)]
    [TestCase(ApiService.Tsdr, DocumentFormat.Xml, UsptoMediaTypes.Xml)]
    [TestCase(ApiService.Tsdr, DocumentFormat.Html, UsptoMediaTypes.Html)]
    [TestCase(ApiService.TsdrPdfAndZip, DocumentFormat.Pdf, UsptoMediaTypes.Pdf)]
    [TestCase(ApiService.TsdrPdfAndZip, DocumentFormat.Zip, UsptoMediaTypes.Zip)]
    [TestCase(ApiService.PatentsView, DocumentFormat.Unknown, UsptoMediaTypes.Json)]
    public void ContentNegotiationUsesTheRepresentationSupportedByEachService(ApiService service, DocumentFormat format, string expectedMediaType)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        RequestHeaderPolicy.Apply(request, service, new UsptoClientOptions(), true, format);

        Assert.That(request.Headers.Accept.Single().MediaType, Is.EqualTo(expectedMediaType));
    }

    [TestCase(ApiService.Odp, DocumentFormat.Xml)]
    [TestCase(ApiService.OfficeAction, DocumentFormat.Text)]
    [TestCase(ApiService.Tsdr, DocumentFormat.Pdf)]
    [TestCase(ApiService.TsdrPdfAndZip, DocumentFormat.Json)]
    [TestCase(ApiService.PatentsView, DocumentFormat.Csv)]
    public void UnsupportedServiceFormatPairsFailBeforeChangingTheRequest(ApiService service, DocumentFormat format)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        request.Headers.Accept.ParseAdd(UsptoMediaTypes.Text);
        request.Headers.TryAddWithoutValidation(UsptoHeaders.UserAgent, "existing-agent/1.0");

        ValidationException exception = Assert.Throws<ValidationException>(() => RequestHeaderPolicy.Apply(
            request,
            service,
            new UsptoClientOptions(),
            true,
            format));

        Assert.Multiple(() =>
        {
            Assert.That(exception.FieldName, Is.EqualTo("responseFormat"));
            Assert.That(exception.ErrorCode, Is.EqualTo("UNSUPPORTED_FORMAT"));
            Assert.That(request.Headers.Accept.Single().MediaType, Is.EqualTo(UsptoMediaTypes.Text));
            Assert.That(request.Headers.UserAgent.ToString(), Is.EqualTo("existing-agent/1.0"));
        });
    }

    [Test]
    public async Task CredentialIsAddedOnlyToHeadersAndRequestContentIsPreserved()
    {
        const string secretMarker = "header-only-secret";
        const string json = "{\"query\":\"battery\"}";
        UsptoClientOptions options = new UsptoClientOptions { PatentsViewApiKey = secretMarker };
        using HttpRequestMessage request = RequestBuilder.CreateJsonPostRequest(
            new Uri("https://example.test/"),
            new string[] { "api", "v1", "patent" },
            new Dictionary<string, string?> { ["size"] = "25" },
            json);

        RequestHeaderPolicy.Apply(request, ApiService.PatentsView, options, false, DocumentFormat.Json);
        string body = await request.Content!.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(request.Headers.GetValues(UsptoHeaders.PatentsViewApiKey).Single(), Is.EqualTo(secretMarker));
            Assert.That(request.RequestUri?.AbsoluteUri, Is.EqualTo("https://example.test/api/v1/patent?size=25"));
            Assert.That(request.RequestUri?.AbsoluteUri, Does.Not.Contain(secretMarker));
            Assert.That(body, Is.EqualTo(json));
            Assert.That(body, Does.Not.Contain(secretMarker));
            Assert.That(request.Content.Headers.ContentType?.MediaType, Is.EqualTo(UsptoMediaTypes.Json));
        });
    }

    [Test]
    public void MissingBlankAndExpiredCredentialsFailWithoutDisclosingSecrets()
    {
        const string secretMarker = "must-not-appear";
        using HttpRequestMessage missingRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/missing");
        using HttpRequestMessage blankRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/blank");
        using HttpRequestMessage expiredRequest = new HttpRequestMessage(HttpMethod.Get, "https://example.test/expired");
        RotatingApiKeyProvider provider = new RotatingApiKeyProvider
        {
            CurrentKey = secretMarker,
            HasExpiresAt = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        AuthenticationException missing = Assert.Throws<AuthenticationException>(() => RequestHeaderPolicy.Apply(
            missingRequest,
            ApiService.Odp,
            new UsptoClientOptions(),
            false,
            DocumentFormat.Json));
        AuthenticationException blank = Assert.Throws<AuthenticationException>(() => RequestHeaderPolicy.Apply(
            blankRequest,
            ApiService.Tsdr,
            new UsptoClientOptions { TsdrApiKey = " \t " },
            false,
            DocumentFormat.Json));
        AuthenticationException expired = Assert.Throws<AuthenticationException>(() => RequestHeaderPolicy.Apply(
            expiredRequest,
            ApiService.PatentsView,
            new UsptoClientOptions { ApiKeyProvider = provider },
            false,
            DocumentFormat.Json));

        Assert.Multiple(() =>
        {
            Assert.That(missing.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(blank.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(expired.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(missing.ToString(), Does.Not.Contain(secretMarker));
            Assert.That(blank.ToString(), Does.Not.Contain(secretMarker));
            Assert.That(expired.ToString(), Does.Not.Contain(secretMarker));
            Assert.That(expiredRequest.Headers.Contains(UsptoHeaders.PatentsViewApiKey), Is.False);
        });
    }
}

internal class RotatingApiKeyProvider : IApiKeyProvider
{
    internal string CurrentKey = "provider-key";
    internal int CallCount = 0;
    internal ApiCredentialRequest? LastRequest = null;
    internal bool HasExpiresAt = false;
    internal DateTimeOffset ExpiresAt = default(DateTimeOffset);

    public ApiCredentialResult GetApiKey(ApiCredentialRequest request)
    {
        CallCount++;
        LastRequest = request;
        return new ApiCredentialResult
        {
            Request = request,
            ApiKey = CurrentKey,
            Source = "test",
            IsAvailable = true,
            HasExpiresAt = HasExpiresAt,
            ExpiresAt = ExpiresAt
        };
    }
}
