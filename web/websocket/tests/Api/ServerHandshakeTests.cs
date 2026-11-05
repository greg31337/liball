using System.Text;
using websocket;

namespace websocket.Tests;

public class ServerHandshakeTests
{
    const string ValidKey = "dGhlIHNhbXBsZSBub25jZQ==";
    const string ExpectedAccept = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

    [Test]
    public void AcceptsHttp1RequestThroughThePublicContract()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat"],
            SupportedExtensions = [Offer("x-mode", Parameter("speed", "fast"))],
            ExtraFields = [Field("X-Server", "websocket-tests")]
        };
        var request = ValidRequest(
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("Sec-WebSocket-Extensions", "x-mode; speed=fast"));

        var result = handshake.EvaluateHttp1Request(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.Request, Is.Not.SameAs(request));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(101));
            Assert.That(Value(result.Response, "Sec-WebSocket-Accept"),
                Is.EqualTo(ExpectedAccept));
            Assert.That(Value(result.Response, "X-Server"), Is.EqualTo("websocket-tests"));
            Assert.That(result.HasClientKey, Is.True);
            Assert.That(result.ClientKey, Is.EqualTo(ValidKey));
            Assert.That(result.HasSelectedSubprotocol, Is.True);
            Assert.That(result.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(result.Extensions, Has.Length.EqualTo(1));
            Assert.That(result.Extensions[0].Name, Is.EqualTo("x-mode"));
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HasError, Is.False);
        });
    }

    [TestCase("metadata", "websocket-request-metadata-invalid")]
    [TestCase("method", "websocket-request-line-invalid")]
    [TestCase("host", "websocket-request-host-invalid")]
    [TestCase("upgrade", "websocket-request-upgrade-invalid")]
    [TestCase("connection", "websocket-request-connection-invalid")]
    [TestCase("key", "websocket-request-key-invalid")]
    [TestCase("version", "websocket-request-version-invalid")]
    [TestCase("protocol", "websocket-request-subprotocol-invalid")]
    [TestCase("extension", "websocket-request-extensions-invalid")]
    [TestCase("origin", "websocket-request-origin-invalid")]
    public void ReturnsDeterministic400Identifiers(string mutation, string identifier)
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        Mutate(request, mutation);

        var result = handshake.EvaluateHttp1Request(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.HasHttpStatusCode, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(400));
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HasSelectedSubprotocol, Is.False);
            Assert.That(result.Extensions, Is.Empty);
        });
    }

    [Test]
    public void UnsupportedVersionReturnsDeterministic426Response()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Version", "8");

        var result = handshake.EvaluateHttp1Request(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-version-unsupported"));
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(426));
            Assert.That(result.Response.StatusCode, Is.EqualTo(426));
            Assert.That(Value(result.Response, "Sec-WebSocket-Version"), Is.EqualTo("13"));
        });
    }

    [Test]
    public void ApplicationPolicyRejectionReturnsConfiguredMetadata()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var body = Encoding.UTF8.GetBytes("not found");
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new DenyingServerOriginPolicy(),
            RejectionStatusCode = 404,
            RejectionFields = [Field("Content-Type", "text/plain")],
            HasRejectionBody = true,
            RejectionBody = body
        };

        var result = handshake.EvaluateHttp1Request(ValidRequest(), options);
        body[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-origin-rejected"));
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(404));
            Assert.That(result.Response.StatusCode, Is.EqualTo(404));
            Assert.That(Value(result.Response, "Content-Type"), Is.EqualTo("text/plain"));
            Assert.That(Value(result.Response, "Content-Length"), Is.EqualTo("9"));
            Assert.That(Encoding.UTF8.GetString(result.Response.Body), Is.EqualTo("not found"));
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void RejectionDoesNotLeakNegotiationOrResponseStateIntoNextEvaluation()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var rejectedOptions = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new DenyingServerOriginPolicy(),
            RejectionStatusCode = 404,
            RejectionFields = [Field("X-Rejection", "first")],
            HasRejectionBody = true,
            RejectionBody = [1, 2, 3]
        };
        var rejected = handshake.EvaluateHttp1Request(ValidRequest(), rejectedOptions);

        var accepted = handshake.EvaluateHttp1Request(
            ValidRequest(),
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(accepted.HasError, Is.False);
            Assert.That(accepted.HasSelectedSubprotocol, Is.False);
            Assert.That(accepted.Extensions, Is.Empty);
            Assert.That(accepted.Response.StatusCode, Is.EqualTo(101));
            Assert.That(accepted.Response.HasBody, Is.False);
            Assert.That(accepted.Response.Fields.Any(field =>
                field.Name == "X-Rejection"), Is.False);
            Assert.That(accepted.Response.Fields.Any(field =>
                field.Name == "Content-Length"), Is.False);
            Assert.That(rejected.Response.StatusCode, Is.EqualTo(404));
            Assert.That(rejected.Response.Body, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ResultsDoNotShareMutableRequestOrOptionObjects()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = ValidRequest();
        var extra = Field("X-Server", "original");
        var options = new WebSocketServerHandshakeOptions
        {
            ExtraFields = [extra]
        };

        var result = handshake.EvaluateHttp1Request(request, options);
        request.Fields[0].Value = "mutated.example";
        extra.Value = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Request.Fields[0].Value, Is.EqualTo("example.com"));
            Assert.That(Value(result.Response, "X-Server"), Is.EqualTo("original"));
        });
    }

    [Test]
    public void NonExtendedTransportIsRejectedAtExtendedConnectBoundary()
    {
        IWebSocketServerHandshake handshake = new WebSocketServerHandshake();
        var request = new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedTransports = [WebSocketHandshakeTransport.Http2ExtendedConnect],
            ExtendedConnectEnabled = true
        };

        Assert.Throws<WebSocketHandshakeException>(() =>
            handshake.EvaluateExtendedConnectRequest(request, options));
    }

    static WebSocketHttpRequest ValidRequest(params WebSocketHeaderField[] additionalFields)
    {
        var fields = new List<WebSocketHeaderField>
        {
            Field("Host", "example.com"),
            Field("Upgrade", "websocket"),
            Field("Connection", "Upgrade"),
            Field("Sec-WebSocket-Key", ValidKey),
            Field("Sec-WebSocket-Version", "13")
        };
        fields.AddRange(additionalFields);
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/chat",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1",
            Fields = fields.ToArray()
        };
    }

    static void Mutate(WebSocketHttpRequest request, string mutation)
    {
        if (mutation == "metadata")
        {
            request.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;
        }
        else if (mutation == "method")
        {
            request.Method = "POST";
        }
        else if (mutation == "host")
        {
            Remove(request, "Host");
        }
        else if (mutation == "upgrade")
        {
            Replace(request, "Upgrade", "h2c");
        }
        else if (mutation == "connection")
        {
            Replace(request, "Connection", "keep-alive");
        }
        else if (mutation == "key")
        {
            Replace(request, "Sec-WebSocket-Key", "not-base64");
        }
        else if (mutation == "version")
        {
            Replace(request, "Sec-WebSocket-Version", "013");
        }
        else if (mutation == "protocol")
        {
            request.Fields = request.Fields.Append(
                Field("Sec-WebSocket-Protocol", "chat, bad protocol")).ToArray();
        }
        else if (mutation == "extension")
        {
            request.Fields = request.Fields.Append(
                Field("Sec-WebSocket-Extensions", "x-test; =bad")).ToArray();
        }
        else
        {
            request.Fields = request.Fields.Append(
                Field("Origin", "https://example.com/path")).ToArray();
        }
    }

    static void Remove(WebSocketHttpRequest request, string fieldName)
    {
        request.Fields = request.Fields.Where(field => !string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    static void Replace(WebSocketHttpRequest request, string fieldName, string value)
    {
        request.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value = value;
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
}

internal class DenyingServerOriginPolicy : IWebSocketOriginPolicy
{
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        return false;
    }
}
