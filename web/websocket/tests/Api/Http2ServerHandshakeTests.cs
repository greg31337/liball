using System.Text;
using websocket;

namespace websocket.Tests;

public class Http2ServerHandshakeTests
{
    [Test]
    public void AcceptsValidHttp2ExtendedConnectWithA200Response()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request, Is.Not.SameAs(request));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.Transport,
                Is.EqualTo(WebSocketHandshakeTransport.Http2ExtendedConnect));
            Assert.That(result.Response.StatusCode, Is.EqualTo(200));
            Assert.That(result.Response.HasReasonPhrase, Is.False);
            Assert.That(result.Response.ReasonPhrase, Is.Empty);
            Assert.That(result.Response.HasBody, Is.False);
            Assert.That(result.HasClientKey, Is.False);
            Assert.That(result.HasSelectedSubprotocol, Is.False);
            Assert.That(result.Extensions, Is.Empty);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Response.Fields.Any(field =>
                field.Name == "sec-websocket-accept"), Is.False);
            Assert.That(result.Response.Fields.Any(field =>
                field.Name == "sec-websocket-key"), Is.False);
            Assert.That(result.Response.Fields.Any(field =>
                field.Name == "connection" ||
                field.Name == "upgrade" ||
                field.Name == "host"), Is.False);
            Assert.That(ExtendedConnectValidator.ValidateResponse(
                result.Response,
                WebSocketHandshakeTransport.Http2ExtendedConnect).Valid, Is.True);
        });
    }

    [Test]
    public void DisabledHttp2ServiceReturnsStructured501Rejection()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();

        var result = handshake.EvaluateExtendedConnectRequest(
            ValidRequest(),
            new WebSocketServerHandshakeOptions());

        AssertRejection(
            result,
            501,
            "extended-connect-not-supported");
    }

    [Test]
    public void UnsupportedProtocolReturnsStructured501Rejection()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        request.Protocol = "connect-udp";

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        AssertRejection(
            result,
            501,
            "extended-connect-protocol-unsupported");
    }

    [TestCase("method", "extended-connect-method-invalid")]
    [TestCase("request-target", "extended-connect-http1-metadata-invalid")]
    [TestCase("scheme", "extended-connect-scheme-invalid")]
    [TestCase("authority", "extended-connect-authority-invalid")]
    [TestCase("path", "extended-connect-path-invalid")]
    [TestCase("protocol", "extended-connect-protocol-invalid")]
    [TestCase("uppercase-field", "extended-connect-field-name-not-lowercase")]
    [TestCase("forbidden-field", "extended-connect-field-forbidden")]
    public void MalformedControlOrRegularFieldsReturnStructured400(
        string mutation,
        string identifier)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        MutateCommonRequest(request, mutation);

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        Assert.Multiple(() =>
        {
            AssertFailure(result, 400, identifier);
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(ExtendedConnectValidator.ValidateResponse(
                result.Response,
                WebSocketHandshakeTransport.Http2ExtendedConnect).Valid, Is.True);
        });
    }

    [TestCase("missing")]
    [TestCase("repeated")]
    [TestCase("malformed")]
    public void InvalidVersionFieldReturns400(string mutation)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        if (mutation == "missing")
        {
            request.Fields = [];
        }
        else if (mutation == "repeated")
        {
            request.Fields =
            [
                Field("sec-websocket-version", "13"),
                Field("sec-websocket-version", "13")
            ];
        }
        else
        {
            request.Fields = [Field("sec-websocket-version", "013")];
        }

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        AssertFailure(result, 400, "extended-connect-version-invalid");
    }

    [Test]
    public void UnsupportedVersionReturns426WithVersionAdvertisement()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        request.Fields[0].Value = "8";

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        Assert.Multiple(() =>
        {
            AssertRejection(result, 426, "extended-connect-version-unsupported");
            Assert.That(Value(result.Response, "sec-websocket-version"), Is.EqualTo("13"));
            Assert.That(result.Response.Fields, Has.Length.EqualTo(1));
        });
    }

    [TestCase("repeated-origin", "extended-connect-origin-invalid")]
    [TestCase("malformed-origin", "extended-connect-origin-invalid")]
    [TestCase("malformed-protocol", "extended-connect-subprotocol-invalid")]
    [TestCase("duplicate-protocol", "extended-connect-subprotocol-invalid")]
    [TestCase("malformed-extension", "extended-connect-extensions-invalid")]
    public void RejectsMalformedWebSocketRegularFieldSemantics(
        string mutation,
        string identifier)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        MutateWebSocketFields(request, mutation);

        var result = handshake.EvaluateExtendedConnectRequest(request, Options());

        AssertFailure(result, 400, identifier);
    }

    [Test]
    public void NegotiatesSubprotocolAndExtensionsIntoLowercaseResponseFields()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        request.Fields = request.Fields.Concat(new[]
        {
            Field("sec-websocket-protocol", "video, chat"),
            Field("sec-websocket-extensions", "x-mode; speed=fast")
        }).ToArray();
        var options = Options();
        options.SupportedSubprotocols = ["chat", "video"];
        options.SupportedExtensions = [Offer("x-mode", Parameter("speed", "fast"))];
        options.ExtraFields = [Field("x-server", "websocket-tests")];

        var result = handshake.EvaluateExtendedConnectRequest(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("video"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(result.Response.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "sec-websocket-protocol",
                "sec-websocket-extensions",
                "x-server"
            }));
            Assert.That(Value(result.Response, "sec-websocket-protocol"),
                Is.EqualTo("video"));
            Assert.That(Value(result.Response, "sec-websocket-extensions"),
                Is.EqualTo("x-mode; speed=fast"));
        });
    }

    [Test]
    public void DefaultOriginPolicyAllowsSameOriginAndRejectsCrossOrigin()
    {
        IWebSocketServerHandshake sameHandshake = new WebSocketServerHandshake();
        var sameRequest = ValidRequest();
        sameRequest.Fields = sameRequest.Fields.Append(
            Field("origin", "https://EXAMPLE.com:443")).ToArray();
        IWebSocketServerHandshake crossHandshake = new WebSocketServerHandshake();
        var crossRequest = ValidRequest();
        crossRequest.Fields = crossRequest.Fields.Append(
            Field("origin", "https://other.example")).ToArray();

        var same = sameHandshake.EvaluateExtendedConnectRequest(sameRequest, Options());
        var cross = crossHandshake.EvaluateExtendedConnectRequest(crossRequest, Options());

        Assert.Multiple(() =>
        {
            Assert.That(same.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            AssertRejection(cross, 403, "extended-connect-origin-rejected");
        });
    }

    [Test]
    public void ApplicationPolicyReturnsConfiguredRejectionFieldsAndBody()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var body = Encoding.UTF8.GetBytes("not found");
        var options = Options();
        options.HasOriginPolicy = true;
        options.OriginPolicy = new RejectingHttp2OriginPolicy();
        options.RejectionStatusCode = 404;
        options.RejectionFields = [Field("content-type", "text/plain")];
        options.HasRejectionBody = true;
        options.RejectionBody = body;

        var result = handshake.EvaluateExtendedConnectRequest(ValidRequest(), options);
        body[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            AssertRejection(result, 404, "extended-connect-origin-rejected");
            Assert.That(Value(result.Response, "content-type"), Is.EqualTo("text/plain"));
            Assert.That(Value(result.Response, "content-length"), Is.EqualTo("9"));
            Assert.That(Encoding.UTF8.GetString(result.Response.Body), Is.EqualTo("not found"));
            Assert.That(ExtendedConnectValidator.ValidateResponse(
                result.Response,
                WebSocketHandshakeTransport.Http2ExtendedConnect).Valid, Is.True);
        });
    }

    [TestCase("Origin")]
    [TestCase("host")]
    [TestCase("sec-websocket-version")]
    [TestCase("sec-websocket-protocol")]
    [TestCase("sec-websocket-extensions")]
    [TestCase("content-length")]
    public void RejectsUnsafeForbiddenOrCriticalCallerResponseFields(string fieldName)
    {
        var successOptions = Options();
        successOptions.ExtraFields = [Field(fieldName, "value")];
        var rejectionOptions = Options();
        rejectionOptions.RejectionFields = [Field(fieldName, "value")];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                new WebSocketServerHandshake().EvaluateExtendedConnectRequest(
                    ValidRequest(),
                    successOptions));
            Assert.Throws<WebSocketHandshakeException>(() =>
                new WebSocketServerHandshake().EvaluateExtendedConnectRequest(
                    ValidRequest(),
                    rejectionOptions));
        });
    }

    [Test]
    public void RejectedEvaluationDoesNotLeakStateIntoNextAcceptance()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var rejectedOptions = Options();
        rejectedOptions.HasOriginPolicy = true;
        rejectedOptions.OriginPolicy = new RejectingHttp2OriginPolicy();
        rejectedOptions.RejectionStatusCode = 404;
        rejectedOptions.RejectionFields = [Field("x-rejection", "first")];
        rejectedOptions.HasRejectionBody = true;
        rejectedOptions.RejectionBody = [1, 2];
        var rejected = handshake.EvaluateExtendedConnectRequest(
            ValidRequest(),
            rejectedOptions);

        var accepted = handshake.EvaluateExtendedConnectRequest(ValidRequest(), Options());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(accepted.Response.StatusCode, Is.EqualTo(200));
            Assert.That(accepted.Response.HasBody, Is.False);
            Assert.That(accepted.Response.Fields, Is.Empty);
            Assert.That(accepted.HasError, Is.False);
            Assert.That(rejected.Response.Body, Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    static WebSocketServerHandshakeOptions Options()
    {
        return new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [WebSocketHandshakeTransport.Http2ExtendedConnect],
            ExtendedConnectEnabled = true
        };
    }

    static WebSocketHttpRequest ValidRequest()
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http2ExtendedConnect,
            Method = "CONNECT",
            HasScheme = true,
            Scheme = "https",
            HasAuthority = true,
            Authority = "example.com:443",
            HasPath = true,
            Path = "/chat",
            HasProtocol = true,
            Protocol = "websocket",
            Fields = [Field("sec-websocket-version", "13")]
        };
    }

    static void MutateCommonRequest(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "method")
        {
            request.Method = "GET";
        }
        else if (mutation == "request-target")
        {
            request.HasRequestTarget = true;
            request.RequestTarget = "/chat";
        }
        else if (mutation == "scheme")
        {
            request.Scheme = "ws";
        }
        else if (mutation == "authority")
        {
            request.Authority = "user@example.com";
        }
        else if (mutation == "path")
        {
            request.Path = "chat";
        }
        else if (mutation == "protocol")
        {
            request.HasProtocol = false;
            request.Protocol = "";
        }
        else if (mutation == "uppercase-field")
        {
            request.Fields = [Field("Sec-WebSocket-Version", "13")];
        }
        else
        {
            request.Fields = request.Fields.Append(Field("upgrade", "websocket")).ToArray();
        }
    }

    static void MutateWebSocketFields(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "repeated-origin")
        {
            request.Fields = request.Fields.Concat(new[]
            {
                Field("origin", "https://example.com"),
                Field("origin", "https://example.com")
            }).ToArray();
        }
        else if (mutation == "malformed-origin")
        {
            request.Fields = request.Fields.Append(
                Field("origin", "https://example.com/path")).ToArray();
        }
        else if (mutation == "malformed-protocol")
        {
            request.Fields = request.Fields.Append(
                Field("sec-websocket-protocol", "chat, bad protocol")).ToArray();
        }
        else if (mutation == "duplicate-protocol")
        {
            request.Fields = request.Fields.Concat(new[]
            {
                Field("sec-websocket-protocol", "chat"),
                Field("sec-websocket-protocol", "chat")
            }).ToArray();
        }
        else
        {
            request.Fields = request.Fields.Append(
                Field("sec-websocket-extensions", "x-test; =bad")).ToArray();
        }
    }

    static string Value(WebSocketHttpResponse response, string fieldName)
    {
        return response.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value;
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }

    static WebSocketExtensionOffer Offer(
        string name,
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = name,
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Parameter(string name, string value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = true,
            Value = value
        };
    }

    static void AssertFailure(
        WebSocketHandshakeResult result,
        int statusCode,
        string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.HasHttpStatusCode, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(statusCode));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    static void AssertRejection(
        WebSocketHandshakeResult result,
        int statusCode,
        string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(statusCode));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
        });
    }
}

internal class RejectingHttp2OriginPolicy : IWebSocketOriginPolicy
{
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        return false;
    }
}
