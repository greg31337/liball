using websocket;

namespace websocket.Tests;

public class ExtendedConnectValidatorTests
{
    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void AcceptsCompleteWebSocketExtendedConnectRequests(
        WebSocketHandshakeTransport transport)
    {
        var request = ValidRequest(transport);
        request.Fields =
        [
            Field("origin", "https://example.com"),
            Field("sec-websocket-version", "13"),
            Field("sec-websocket-protocol", "chat, superchat"),
            Field("sec-websocket-extensions", "x-test; mode=fast"),
            Field("cookie", "session=abc"),
            Field("te", "TRAILERS")
        ];

        var result = ExtendedConnectValidator.ValidateRequest(request, transport);

        AssertSuccess(result);
    }

    [TestCase("method", "extended-connect-method-invalid")]
    [TestCase("request-target", "extended-connect-http1-metadata-invalid")]
    [TestCase("http-version", "extended-connect-http1-metadata-invalid")]
    [TestCase("scheme", "extended-connect-scheme-invalid")]
    [TestCase("authority", "extended-connect-authority-invalid")]
    [TestCase("path", "extended-connect-path-invalid")]
    [TestCase("protocol", "extended-connect-protocol-invalid")]
    public void RejectsInvalidOrInconsistentRequestControlMetadata(
        string mutation,
        string identifier)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        MutateControlMetadata(request, mutation);

        var result = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        AssertFailure(result, identifier);
    }

    [TestCase("null-method", "extended-connect-method-invalid")]
    [TestCase("missing-scheme", "extended-connect-scheme-invalid")]
    [TestCase("null-scheme", "extended-connect-scheme-invalid")]
    [TestCase("missing-authority", "extended-connect-authority-invalid")]
    [TestCase("null-authority", "extended-connect-authority-invalid")]
    [TestCase("missing-path", "extended-connect-path-invalid")]
    [TestCase("null-path", "extended-connect-path-invalid")]
    [TestCase("missing-protocol", "extended-connect-protocol-invalid")]
    [TestCase("null-protocol", "extended-connect-protocol-invalid")]
    [TestCase("unflagged-request-target", "extended-connect-http1-metadata-invalid")]
    [TestCase("flagged-empty-request-target", "extended-connect-http1-metadata-invalid")]
    [TestCase("unflagged-http-version", "extended-connect-http1-metadata-invalid")]
    [TestCase("flagged-empty-http-version", "extended-connect-http1-metadata-invalid")]
    public void RejectsEveryMissingNullOrInconsistentPseudoFieldForm(
        string mutation,
        string identifier)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        MutatePseudoFieldShape(request, mutation);

        var result = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        AssertFailure(result, identifier);
    }

    [TestCase("http", "example.com:80", "/", true)]
    [TestCase("https", "example.com:443", "/chat?room=blue", true)]
    [TestCase("https", "[2001:db8::1]:8443", "/encoded/%7Euser", true)]
    [TestCase("https", "example.com", "*", false)]
    [TestCase("https", "example.com", "/chat#fragment", false)]
    public void ValidatesSchemeAuthorityAndPathPseudoFieldVariants(
        string scheme,
        string authority,
        string path,
        bool expectedValid)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        request.Scheme = scheme;
        request.Authority = authority;
        request.Path = path;

        var result = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        Assert.That(result.Valid, Is.EqualTo(expectedValid));
    }

    [Test]
    public void RejectsWrongOrNonExtendedTransportExpectations()
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);

        var wrongTransport = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http3ExtendedConnect);
        var http1Expectation = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http1Upgrade);
        var nullRequest = ExtendedConnectValidator.ValidateRequest(
            null,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        Assert.Multiple(() =>
        {
            AssertFailure(wrongTransport, "extended-connect-transport-invalid");
            AssertFailure(http1Expectation, "extended-connect-transport-invalid");
            AssertFailure(nullRequest, "extended-connect-transport-invalid");
        });
    }

    [TestCase("connection")]
    [TestCase("proxy-connection")]
    [TestCase("keep-alive")]
    [TestCase("transfer-encoding")]
    [TestCase("upgrade")]
    [TestCase("host")]
    [TestCase("sec-websocket-key")]
    [TestCase("sec-websocket-accept")]
    public void RejectsHttp1AndConnectionSpecificFieldsInBothDirections(string fieldName)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        request.Fields = [Field(fieldName, "value")];
        var response = ValidResponse(WebSocketHandshakeTransport.Http2ExtendedConnect);
        response.Fields = [Field(fieldName, "value")];

        var requestResult = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http2ExtendedConnect);
        var responseResult = ExtendedConnectValidator.ValidateResponse(
            response,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        Assert.Multiple(() =>
        {
            AssertFailure(requestResult, "extended-connect-field-forbidden");
            AssertFailure(responseResult, "extended-connect-field-forbidden");
        });
    }

    [TestCase("Origin", "https://example.com", "extended-connect-field-name-not-lowercase")]
    [TestCase(":method", "CONNECT", "extended-connect-field-invalid")]
    [TestCase("bad name", "value", "extended-connect-field-invalid")]
    [TestCase("x-test", " bad", "extended-connect-field-invalid")]
    [TestCase("x-test", "bad\t", "extended-connect-field-invalid")]
    [TestCase("x-test", "bad\rvalue", "extended-connect-field-invalid")]
    public void RejectsInvalidRegularFieldNamesAndValues(
        string fieldName,
        string fieldValue,
        string identifier)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http3ExtendedConnect);
        request.Fields = [Field(fieldName, fieldValue)];

        var result = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http3ExtendedConnect);

        AssertFailure(result, identifier);
    }

    [TestCase("")]
    [TestCase("gzip")]
    [TestCase("trailers, gzip")]
    public void RejectsTeValuesOtherThanTrailers(string value)
    {
        var request = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        request.Fields = [Field("te", value)];

        var result = ExtendedConnectValidator.ValidateRequest(
            request,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        AssertFailure(result, "extended-connect-te-invalid");
    }

    [Test]
    public void RejectsNullRegularFieldCollectionsAndEntries()
    {
        var nullCollection = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        nullCollection.Fields = null!;
        var nullEntry = ValidRequest(WebSocketHandshakeTransport.Http2ExtendedConnect);
        nullEntry.Fields = [null!];

        var collectionResult = ExtendedConnectValidator.ValidateRequest(
            nullCollection,
            WebSocketHandshakeTransport.Http2ExtendedConnect);
        var entryResult = ExtendedConnectValidator.ValidateRequest(
            nullEntry,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        Assert.Multiple(() =>
        {
            AssertFailure(collectionResult, "extended-connect-fields-invalid");
            AssertFailure(entryResult, "extended-connect-field-invalid");
        });
    }

    [TestCase(WebSocketHandshakeTransport.Http2ExtendedConnect)]
    [TestCase(WebSocketHandshakeTransport.Http3ExtendedConnect)]
    public void AcceptsValidResponseControlMetadataAndRegularFields(
        WebSocketHandshakeTransport transport)
    {
        var success = ValidResponse(transport);
        success.Fields =
        [
            Field("sec-websocket-protocol", "chat"),
            Field("sec-websocket-extensions", "x-test"),
            Field("te", "trailers")
        ];
        var rejection = ValidResponse(transport);
        rejection.StatusCode = 404;
        rejection.Fields = [Field("content-type", "text/plain")];
        rejection.HasBody = true;
        rejection.Body = [1, 2, 3];

        var successResult = ExtendedConnectValidator.ValidateResponse(success, transport);
        var rejectionResult = ExtendedConnectValidator.ValidateResponse(rejection, transport);

        Assert.Multiple(() =>
        {
            AssertSuccess(successResult);
            AssertSuccess(rejectionResult);
        });
    }

    [TestCase("transport", "extended-connect-response-transport-invalid")]
    [TestCase("status", "extended-connect-response-metadata-invalid")]
    [TestCase("reason", "extended-connect-response-metadata-invalid")]
    [TestCase("unflagged-body", "extended-connect-response-metadata-invalid")]
    [TestCase("informational-body", "extended-connect-response-metadata-invalid")]
    [TestCase("success-body", "extended-connect-response-metadata-invalid")]
    public void RejectsInvalidResponseControlMetadata(string mutation, string identifier)
    {
        var response = ValidResponse(WebSocketHandshakeTransport.Http2ExtendedConnect);
        MutateResponseMetadata(response, mutation);

        var result = ExtendedConnectValidator.ValidateResponse(
            response,
            WebSocketHandshakeTransport.Http2ExtendedConnect);

        AssertFailure(result, identifier);
    }

    [Test]
    public void ResponseRegularFieldsUseTheSameLowercaseAndTeRules()
    {
        var uppercase = ValidResponse(WebSocketHandshakeTransport.Http3ExtendedConnect);
        uppercase.Fields = [Field("X-Test", "value")];
        var invalidTe = ValidResponse(WebSocketHandshakeTransport.Http3ExtendedConnect);
        invalidTe.Fields = [Field("te", "gzip")];

        var uppercaseResult = ExtendedConnectValidator.ValidateResponse(
            uppercase,
            WebSocketHandshakeTransport.Http3ExtendedConnect);
        var teResult = ExtendedConnectValidator.ValidateResponse(
            invalidTe,
            WebSocketHandshakeTransport.Http3ExtendedConnect);

        Assert.Multiple(() =>
        {
            AssertFailure(uppercaseResult, "extended-connect-field-name-not-lowercase");
            AssertFailure(teResult, "extended-connect-te-invalid");
        });
    }

    static WebSocketHttpRequest ValidRequest(WebSocketHandshakeTransport transport)
    {
        return new WebSocketHttpRequest
        {
            Transport = transport,
            Method = "CONNECT",
            HasScheme = true,
            Scheme = "https",
            HasAuthority = true,
            Authority = "example.com:443",
            HasPath = true,
            Path = "/chat?room=blue",
            HasProtocol = true,
            Protocol = "websocket"
        };
    }

    static WebSocketHttpResponse ValidResponse(WebSocketHandshakeTransport transport)
    {
        return new WebSocketHttpResponse
        {
            Transport = transport,
            StatusCode = 200
        };
    }

    static void MutateControlMetadata(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "method")
        {
            request.Method = "connect";
        }
        else if (mutation == "request-target")
        {
            request.HasRequestTarget = true;
            request.RequestTarget = "/chat";
        }
        else if (mutation == "http-version")
        {
            request.HttpVersion = "HTTP/2.0";
        }
        else if (mutation == "scheme")
        {
            request.Scheme = "HTTPS";
        }
        else if (mutation == "authority")
        {
            request.Authority = "user@example.com";
        }
        else if (mutation == "path")
        {
            request.Path = "chat#fragment";
        }
        else
        {
            request.Protocol = "WebSocket";
        }
    }

    static void MutatePseudoFieldShape(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "null-method")
        {
            request.Method = null!;
        }
        else if (mutation == "missing-scheme")
        {
            request.HasScheme = false;
        }
        else if (mutation == "null-scheme")
        {
            request.Scheme = null!;
        }
        else if (mutation == "missing-authority")
        {
            request.HasAuthority = false;
        }
        else if (mutation == "null-authority")
        {
            request.Authority = null!;
        }
        else if (mutation == "missing-path")
        {
            request.HasPath = false;
        }
        else if (mutation == "null-path")
        {
            request.Path = null!;
        }
        else if (mutation == "missing-protocol")
        {
            request.HasProtocol = false;
        }
        else if (mutation == "null-protocol")
        {
            request.Protocol = null!;
        }
        else if (mutation == "unflagged-request-target")
        {
            request.RequestTarget = "/chat";
        }
        else if (mutation == "flagged-empty-request-target")
        {
            request.HasRequestTarget = true;
        }
        else if (mutation == "unflagged-http-version")
        {
            request.HttpVersion = "HTTP/2.0";
        }
        else
        {
            request.HasHttpVersion = true;
        }
    }

    static void MutateResponseMetadata(WebSocketHttpResponse response, string mutation)
    {
        if (mutation == "transport")
        {
            response.Transport = WebSocketHandshakeTransport.Http3ExtendedConnect;
        }
        else if (mutation == "status")
        {
            response.StatusCode = 99;
        }
        else if (mutation == "reason")
        {
            response.HasReasonPhrase = true;
            response.ReasonPhrase = "OK";
        }
        else if (mutation == "unflagged-body")
        {
            response.Body = [1];
        }
        else if (mutation == "informational-body")
        {
            response.StatusCode = 103;
            response.HasBody = true;
            response.Body = [1];
        }
        else
        {
            response.HasBody = true;
            response.Body = [1];
        }
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static void AssertSuccess(ExtendedConnectValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Error.Identifier, Is.Empty);
        });
    }

    static void AssertFailure(ExtendedConnectValidationResult result, string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.Diagnostic, Is.Not.Empty);
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
        });
    }
}
