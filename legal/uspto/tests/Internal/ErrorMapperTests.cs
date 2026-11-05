using System.Net;
using System.Net.Http.Headers;
using System.Text;
using uspto;

namespace uspto.Tests;

public class ErrorMapperTests
{
    [Test]
    public void OdpJsonErrorPreservesCodeDetailsAndBodyRequestIdentifier()
    {
        const string secret = "odp-secret";
        const string body = "{\"code\":400,\"error\":\"Bad Request\",\"errorDetails\":\"invalid apiKey=odp-secret\",\"requestIdentifier\":\"body-request\"}";
        using HttpResponseMessage response = CreateError(HttpStatusCode.BadRequest, body, UsptoMediaTypes.Json);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "header-request");

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, ApiService.Odp, null, new UsptoLimits(), new string[] { secret }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(400));
            Assert.That(exception.ErrorCode, Is.EqualTo("400"));
            Assert.That(exception.ErrorDetails, Does.Contain(SecretRedactor.Replacement));
            Assert.That(exception.ErrorDetails, Does.Not.Contain(secret));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("body-request"));
            Assert.That(exception.IsRetryable, Is.False);
            Assert.That(exception.ToString(), Does.Not.Contain(secret));
        });
    }

    [TestCase(HttpStatusCode.Unauthorized, ApiService.Odp, "rejected the request credential")]
    [TestCase(HttpStatusCode.Forbidden, ApiService.PatentsView, "refused authorization")]
    public void AuthenticationAndAuthorizationFailuresRemainDistinct(HttpStatusCode status, ApiService service, string messageFragment)
    {
        using HttpResponseMessage response = CreateError(status, "{\"detail\":\"invalid apiKey=hidden-secret\",\"requestId\":\"auth-request\"}", UsptoMediaTypes.Json);

        AuthenticationException exception = (AuthenticationException)ErrorMapper.MapResponse(response, service, null, new UsptoLimits(), new string[] { "hidden-secret" }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo((int)status));
            Assert.That(exception.Service, Is.EqualTo(service));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("auth-request"));
            Assert.That(exception.Message, Does.Contain(messageFragment));
            Assert.That(exception.ToString(), Does.Not.Contain("hidden-secret"));
        });
    }

    [Test]
    public void OdpEntitlementAndMissingResponsesUseDistinctTypedFailures()
    {
        using HttpResponseMessage forbidden = CreateError(
            HttpStatusCode.Forbidden,
            "{\"code\":403,\"error\":\"Forbidden\",\"errorDetails\":\"API entitlement required\",\"requestIdentifier\":\"entitlement-request\"}",
            UsptoMediaTypes.Json);
        using HttpResponseMessage missing = CreateError(
            HttpStatusCode.NotFound,
            "{\"code\":404,\"error\":\"Not Found\",\"errorDetails\":\"No matching records found\",\"requestIdentifier\":\"missing-request\"}",
            UsptoMediaTypes.Json);

        Exception entitlementFailure = ErrorMapper.MapResponse(forbidden, ApiService.Odp, "14412875", new UsptoLimits(), null, CancellationToken.None);
        Exception missingFailure = ErrorMapper.MapResponse(missing, ApiService.Odp, "14412875", new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(entitlementFailure, Is.TypeOf<AuthenticationException>());
            Assert.That(((AuthenticationException)entitlementFailure).StatusCode, Is.EqualTo(403));
            Assert.That(((AuthenticationException)entitlementFailure).RequestIdentifier, Is.EqualTo("entitlement-request"));
            Assert.That(missingFailure, Is.TypeOf<NotFoundException>());
            Assert.That(((NotFoundException)missingFailure).StatusCode, Is.EqualTo(404));
            Assert.That(((NotFoundException)missingFailure).ResourceIdentifier, Is.EqualTo("14412875"));
            Assert.That(((NotFoundException)missingFailure).RequestIdentifier, Is.EqualTo("missing-request"));
        });
    }

    [TestCase(HttpStatusCode.BadRequest, "HTTP_400", false, "rejected the request as invalid")]
    [TestCase(HttpStatusCode.NotAcceptable, "HTTP_406", false, "returned an error response")]
    [TestCase(HttpStatusCode.RequestTimeout, "HTTP_408", true, "returned an error response")]
    [TestCase(HttpStatusCode.RequestEntityTooLarge, "PAYLOAD_TOO_LARGE", false, "too large")]
    [TestCase(HttpStatusCode.UnprocessableEntity, "HTTP_422", false, "rejected the request as invalid")]
    [TestCase((HttpStatusCode)425, "HTTP_425", true, "returned an error response")]
    public void GeneralHttpFailuresPreserveStatusDetailsAndRetryClassification(HttpStatusCode status, string expectedCode, bool retryable, string messageFragment)
    {
        using HttpResponseMessage response = CreateError(status, "{\"message\":\"service detail\",\"requestId\":\"general-request\"}", UsptoMediaTypes.Json);

        ApiException failure = (ApiException)ErrorMapper.MapResponse(response, ApiService.OfficeAction, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.StatusCode, Is.EqualTo((int)status));
            Assert.That(failure.HasStatusCode, Is.True);
            Assert.That(failure.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(failure.ErrorDetails, Is.EqualTo("service detail"));
            Assert.That(failure.RequestIdentifier, Is.EqualTo("general-request"));
            Assert.That(failure.IsRetryable, Is.EqualTo(retryable));
            Assert.That(failure.Message, Does.Contain(messageFragment));
        });
    }

    [Test]
    public void NotFoundRetainsOnlyTheSafeResourceIdentifier()
    {
        const string secret = "identifier-secret";
        using HttpResponseMessage response = CreateError(HttpStatusCode.NotFound, string.Empty, UsptoMediaTypes.Json);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "not-found-request");

        NotFoundException exception = (NotFoundException)ErrorMapper.MapResponse(response, ApiService.Tsdr, "sn123/" + secret, new UsptoLimits(), new string[] { secret }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResourceIdentifier, Is.EqualTo("sn123/" + SecretRedactor.Replacement));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("not-found-request"));
            Assert.That(exception.StatusCode, Is.EqualTo(404));
        });
    }

    [Test]
    public void PayloadTooLargeUsesTheOdpCodeAndDetailedMessage()
    {
        const string body = "{\"code\":413,\"message\":\"Payload Too Large\",\"detailedMessage\":\"reduce page size\",\"requestIdentifier\":\"large-request\"}";
        using HttpResponseMessage response = CreateError(HttpStatusCode.RequestEntityTooLarge, body, UsptoMediaTypes.Json);

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, ApiService.Odp, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("413"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("reduce page size"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("large-request"));
            Assert.That(exception.Message, Does.Contain("too large"));
        });
    }

    [Test]
    public void PatentsViewRateLimitUsesReasonHeadersAndDeltaRetryAfter()
    {
        using HttpResponseMessage response = CreateError((HttpStatusCode)429, "{\"detail\":\"slow down\"}", UsptoMediaTypes.Json);
        response.Headers.TryAddWithoutValidation("X-Status-Reason-Code", "PV-429");
        response.Headers.TryAddWithoutValidation("X-Status-Reason", "quota exceeded");
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "12");
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "rate-request");

        RateLimitException exception = (RateLimitException)ErrorMapper.MapResponse(response, ApiService.PatentsView, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(429));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(12000));
            Assert.That(exception.HasRetryAfterMilliseconds, Is.True);
            Assert.That(exception.RequestIdentifier, Is.EqualTo("rate-request"));
        });
    }

    [Test]
    public void EmptyServiceUnavailableResponseStillMapsWithRetryMetadata()
    {
        using HttpResponseMessage response = CreateError(HttpStatusCode.ServiceUnavailable, string.Empty, UsptoMediaTypes.Html);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "3");
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RequestIdentifier, "request-identifier-that-is-bounded");
        UsptoLimits limits = new UsptoLimits { MaximumStringCharacters = 18 };

        ServiceUnavailableException exception = (ServiceUnavailableException)ErrorMapper.MapResponse(response, ApiService.Tsdr, null, limits, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(503));
            Assert.That(exception.RetryAfterMilliseconds, Is.EqualTo(3000));
            Assert.That(exception.HasRetryAfterMilliseconds, Is.True);
            Assert.That(exception.RequestIdentifier, Is.EqualTo("request-identifier"));
            Assert.That(exception.Message, Does.Contain("temporarily unavailable"));
        });
    }

    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    public void ServerFailureStatusesMapToServiceUnavailable(HttpStatusCode status)
    {
        using HttpResponseMessage response = CreateError(status, "{\"error\":\"upstream unavailable\",\"requestIdentifier\":\"service-request\"}", UsptoMediaTypes.Json);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "2");

        ServiceUnavailableException failure = (ServiceUnavailableException)ErrorMapper.MapResponse(response, ApiService.Odp, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.StatusCode, Is.EqualTo((int)status));
            Assert.That(failure.HasStatusCode, Is.True);
            Assert.That(failure.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(failure.RequestIdentifier, Is.EqualTo("service-request"));
            Assert.That(failure.RetryAfterMilliseconds, Is.EqualTo(2000L));
            Assert.That(failure.HasRetryAfterMilliseconds, Is.True);
        });
    }

    [TestCase(
        "{\"code\":400,\"description\":\"Bad Request\",\"detailedError\":\"ODP application detail\",\"requestIdentifier\":\"odp-request\"}",
        UsptoMediaTypes.Json,
        "400",
        "ODP application detail",
        "odp-request")]
    [TestCase(
        "{\"errorCode\":\"OA-400\",\"title\":\"Office Action request failed\",\"details\":\"invalid query literal\",\"correlationId\":\"oa-request\"}",
        UsptoMediaTypes.Json,
        "OA-400",
        "invalid query literal",
        "oa-request")]
    [TestCase(
        "{\"statusCode\":\"PV-400\",\"detail\":\"invalid PatentsView query\",\"traceId\":\"pv-request\"}",
        UsptoMediaTypes.Json,
        "PV-400",
        "invalid PatentsView query",
        "pv-request")]
    [TestCase(
        "<ns:errorResponse xmlns:ns=\"urn:uspto\"><ns:errorCode>TSDR-400</ns:errorCode><ns:detailedMessage>invalid trademark reference</ns:detailedMessage><ns:requestId>tsdr-request</ns:requestId></ns:errorResponse>",
        UsptoMediaTypes.Xml,
        "TSDR-400",
        "invalid trademark reference",
        "tsdr-request")]
    public void DocumentedUspToErrorShapesPreserveTypedContext(string body, string mediaType, string expectedCode, string expectedDetails, string expectedRequestIdentifier)
    {
        using HttpResponseMessage response = CreateError(HttpStatusCode.BadRequest, body, mediaType);

        ApiException failure = (ApiException)ErrorMapper.MapResponse(response, ApiService.Odp, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(failure.ErrorDetails, Is.EqualTo(expectedDetails));
            Assert.That(failure.RequestIdentifier, Is.EqualTo(expectedRequestIdentifier));
        });
    }

    [Test]
    public void PatentsViewBooleanErrorAndReasonHeadersHaveStableFallbacks()
    {
        using HttpResponseMessage booleanError = CreateError(HttpStatusCode.BadRequest, "{\"error\":true}", UsptoMediaTypes.Json);
        using HttpResponseMessage reasonError = CreateError(HttpStatusCode.BadRequest, "{}", UsptoMediaTypes.Json);
        reasonError.Headers.TryAddWithoutValidation("X-Status-Reason-Code", "PV-BAD-QUERY");
        reasonError.Headers.TryAddWithoutValidation("X-Status-Reason", "unsupported field");

        ApiException booleanFailure = (ApiException)ErrorMapper.MapResponse(booleanError, ApiService.PatentsView, null, new UsptoLimits(), null, CancellationToken.None);
        ApiException reasonFailure = (ApiException)ErrorMapper.MapResponse(reasonError, ApiService.PatentsView, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(booleanFailure.ErrorCode, Is.EqualTo("UNKNOWN_ERROR_RESPONSE"));
            Assert.That(booleanFailure.ErrorDetails, Is.Null);
            Assert.That(reasonFailure.ErrorCode, Is.EqualTo("PV-BAD-QUERY"));
            Assert.That(reasonFailure.ErrorDetails, Is.EqualTo("unsupported field"));
        });
    }

    [Test]
    public void TsdrXmlErrorsAreParsedWithoutRequiringAJsonShape()
    {
        const string body = "<errorResponse><code>TSDR-400</code><message>Invalid case</message><detail><![CDATA[bad serial &amp; format]]></detail><requestIdentifier>tsdr-request</requestIdentifier></errorResponse>";
        using HttpResponseMessage response = CreateError(HttpStatusCode.BadRequest, body, UsptoMediaTypes.Xml);

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, ApiService.Tsdr, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("TSDR-400"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("bad serial & format"));
            Assert.That(exception.RequestIdentifier, Is.EqualTo("tsdr-request"));
        });
    }

    [TestCase(ApiService.Tsdr)]
    [TestCase(ApiService.TsdrPdfAndZip)]
    public void TsdrNotAcceptableHasAStableContentNegotiationError(ApiService service)
    {
        using HttpResponseMessage response = CreateError(HttpStatusCode.NotAcceptable, "<error><code>406</code><detail>JSON is unavailable</detail></error>", UsptoMediaTypes.Xml);

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, service, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(406));
            Assert.That(exception.ErrorCode, Is.EqualTo("CONTENT_NEGOTIATION_NOT_SUPPORTED"));
            Assert.That(exception.ErrorDetails, Is.EqualTo("JSON is unavailable"));
            Assert.That(exception.Message, Does.Contain("content negotiation"));
            Assert.That(exception.IsRetryable, Is.False);
        });
    }

    [TestCase("{\"message\":\"unterminated", "MALFORMED_ERROR_RESPONSE")]
    [TestCase("{not-json}", "MALFORMED_ERROR_RESPONSE")]
    [TestCase("<error><message>unclosed</error>", "MALFORMED_ERROR_RESPONSE")]
    [TestCase("   ", "EMPTY_ERROR_RESPONSE")]
    [TestCase("{\"unexpected\":\"shape\"}", "UNKNOWN_ERROR_RESPONSE")]
    public void MalformedEmptyAndUnknownBodiesHaveStableFallbackCodes(string body, string expectedCode)
    {
        using HttpResponseMessage response = CreateError(HttpStatusCode.BadRequest, body, UsptoMediaTypes.Json);

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, ApiService.OfficeAction, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(expectedCode));
            Assert.That(exception.StatusCode, Is.EqualTo(400));
        });
    }

    [Test]
    public void ErrorBodyCaptureIsBoundedAndRedactedBeforeExposure()
    {
        const string secret = "must-not-escape";
        string body = "plain failure apiKey=" + secret + " " + new string('x', 200);
        using HttpResponseMessage response = CreateError((HttpStatusCode)418, body, UsptoMediaTypes.Text);
        UsptoLimits limits = new UsptoLimits
        {
            MaximumRawResponseBytes = 80,
            MaximumResponseBytes = 80,
            MaximumStringCharacters = 40
        };

        ApiException exception = (ApiException)ErrorMapper.MapResponse(response, ApiService.PatentsView, null, limits, new string[] { secret }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("TRUNCATED_ERROR_RESPONSE"));
            Assert.That(exception.ErrorDetails?.Length, Is.LessThanOrEqualTo(40));
            Assert.That(exception.ErrorDetails, Does.Not.Contain(secret));
            Assert.That(exception.ToString(), Does.Not.Contain(secret));
        });
    }

    [Test]
    public void TruncatedUtf8ErrorStopsBeforeAnIncompleteCodePoint()
    {
        const string body = "failure café plus details";
        int limit = Encoding.UTF8.GetByteCount("failure caf") + 1;
        using HttpResponseMessage response = CreateError((HttpStatusCode)418, body, UsptoMediaTypes.Text);
        UsptoLimits limits = new UsptoLimits
        {
            MaximumRawResponseBytes = limit,
            MaximumResponseBytes = limit,
            MaximumStringCharacters = 100
        };

        ApiException failure = (ApiException)ErrorMapper.MapResponse(response, ApiService.Odp, null, limits, null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo("TRUNCATED_ERROR_RESPONSE"));
            Assert.That(failure.ErrorDetails, Is.EqualTo("failure caf"));
            Assert.That(failure.ErrorDetails, Does.Not.Contain("�"));
        });
    }

    [Test]
    public void ErrorCaptureHonorsSupportedCharsetWithoutBomAndIgnoresUnsupportedCharset()
    {
        const string xml = "<error><code>TSDR-é</code><detail>échec sûr</detail></error>";
        using HttpResponseMessage encoded = new HttpResponseMessage(HttpStatusCode.BadRequest);
        encoded.Content = new ByteArrayContent(Encoding.BigEndianUnicode.GetBytes(xml));
        encoded.Content.Headers.ContentType = new MediaTypeHeaderValue(UsptoMediaTypes.Xml) { CharSet = "utf-16be" };
        using HttpResponseMessage unsupported = CreateError(HttpStatusCode.BadRequest, "plain fallback", UsptoMediaTypes.Text);
        unsupported.Content.Headers.ContentType!.CharSet = "unsupported-charset";

        ApiException encodedFailure = (ApiException)ErrorMapper.MapResponse(encoded, ApiService.Tsdr, null, new UsptoLimits(), null, CancellationToken.None);
        ApiException fallbackFailure = (ApiException)ErrorMapper.MapResponse(unsupported, ApiService.Odp, null, new UsptoLimits(), null, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(encodedFailure.ErrorCode, Is.EqualTo("TSDR-é"));
            Assert.That(encodedFailure.ErrorDetails, Is.EqualTo("échec sûr"));
            Assert.That(fallbackFailure.ErrorDetails, Is.EqualTo("plain fallback"));
        });
    }

    [Test]
    public void InvalidLimitsAreRejectedBeforeReadingTheBody()
    {
        using HttpResponseMessage response = CreateError(HttpStatusCode.BadRequest, "error", UsptoMediaTypes.Text);
        UsptoLimits limits = new UsptoLimits { MaximumRawResponseBytes = 0 };

        ValidationException exception = Assert.Throws<ValidationException>(() => ErrorMapper.MapResponse(
            response,
            ApiService.Odp,
            null,
            limits,
            null,
            CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_LIMIT"));
    }

    private static HttpResponseMessage CreateError(HttpStatusCode status, string body, string mediaType)
    {
        HttpResponseMessage response = new HttpResponseMessage(status);
        response.Content = new StringContent(body, Encoding.UTF8, mediaType);
        return response;
    }
}
