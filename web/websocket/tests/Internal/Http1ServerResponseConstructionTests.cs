using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1ServerResponseConstructionTests
{
    const string ValidKey = "dGhlIHNhbXBsZSBub25jZQ==";
    const string ExpectedAccept = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

    [Test]
    public void ConstructsCanonical101ResponseWithNegotiatedMetadata()
    {
        var state = new Http1ServerHandshakeState();
        var options = new WebSocketServerHandshakeOptions
        {
            SupportedSubprotocols = ["chat"],
            SupportedExtensions = [Offer("x-mode", Parameter("speed", "fast"))],
            ExtraFields = [Field("X-Server", "websocket-tests")]
        };
        var request = ValidRequest(
            Field("Sec-WebSocket-Protocol", "chat"),
            Field("Sec-WebSocket-Extensions", "x-mode; speed=fast"));

        var result = state.EvaluateRequest(request, options);
        var response = result.Response;

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(response.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(response.StatusCode, Is.EqualTo(101));
            Assert.That(response.HasReasonPhrase, Is.True);
            Assert.That(response.ReasonPhrase, Is.EqualTo("Switching Protocols"));
            Assert.That(response.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "Upgrade",
                "Connection",
                "Sec-WebSocket-Accept",
                "Sec-WebSocket-Protocol",
                "Sec-WebSocket-Extensions",
                "X-Server"
            }));
            Assert.That(Value(response, "Upgrade"), Is.EqualTo("websocket"));
            Assert.That(Value(response, "Connection"), Is.EqualTo("Upgrade"));
            Assert.That(Value(response, "Sec-WebSocket-Accept"), Is.EqualTo(ExpectedAccept));
            Assert.That(Value(response, "Sec-WebSocket-Protocol"), Is.EqualTo("chat"));
            Assert.That(Value(response, "Sec-WebSocket-Extensions"),
                Is.EqualTo("x-mode; speed=fast"));
            Assert.That(response.HasBody, Is.False);
            Assert.That(response.Body, Is.Empty);
            Assert.That(Http1Writer.MeasureResponse(response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void SerializesTheCanonical101Response()
    {
        var result = new Http1ServerHandshakeState().EvaluateRequest(
            ValidRequest(),
            new WebSocketServerHandshakeOptions());

        var text = Serialize(result.Response);

        Assert.That(text, Is.EqualTo(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {ExpectedAccept}\r\n" +
            "\r\n"));
    }

    [Test]
    public void ConstructsSafe400ResponseForMalformedRequests()
    {
        var request = ValidRequest();
        request.Fields = request.Fields.Where(field => field.Name != "Host").ToArray();

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Failed));
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.Response.StatusCode, Is.EqualTo(400));
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo("Bad Request"));
            Assert.That(result.Response.Fields, Is.Empty);
            Assert.That(result.Response.HasBody, Is.False);
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(Serialize(result.Response), Is.EqualTo(
                "HTTP/1.1 400 Bad Request\r\n\r\n"));
        });
    }

    [Test]
    public void Constructs426ResponseWithSupportedVersionAdvertisement()
    {
        var state = new Http1ServerHandshakeState();
        var request = ValidRequest();
        Replace(request, "Sec-WebSocket-Version", "8");

        var result = state.EvaluateRequest(request, new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(426));
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo("Upgrade Required"));
            Assert.That(result.Response.Fields, Has.Length.EqualTo(1));
            Assert.That(result.Response.Fields[0].Name, Is.EqualTo("Sec-WebSocket-Version"));
            Assert.That(result.Response.Fields[0].Value, Is.EqualTo("13"));
            Assert.That(state.ResponseFields[0].Value, Is.EqualTo("13"));
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(Serialize(result.Response), Is.EqualTo(
                "HTTP/1.1 426 Upgrade Required\r\n" +
                "Sec-WebSocket-Version: 13\r\n\r\n"));
        });
    }

    [Test]
    public void ConstructsCanonicalSafe403ResponseForDefaultOriginRejection()
    {
        var request = ValidRequest(Field("Origin", "https://other.example"));

        var result = new Http1ServerHandshakeState().EvaluateRequest(
            request,
            new WebSocketServerHandshakeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Error.Identifier, Is.EqualTo("websocket-origin-rejected"));
            Assert.That(result.Response.StatusCode, Is.EqualTo(403));
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo("Forbidden"));
            Assert.That(result.Response.Fields, Is.Empty);
            Assert.That(result.Response.HasBody, Is.False);
            Assert.That(Serialize(result.Response), Is.EqualTo(
                "HTTP/1.1 403 Forbidden\r\n\r\n"));
        });
    }

    [TestCase(403, "Forbidden", true)]
    [TestCase(404, "Not Found", true)]
    [TestCase(451, "", false)]
    public void ConstructsConfiguredApplicationRejectionStatus(
        int statusCode,
        string reasonPhrase,
        bool hasReasonPhrase)
    {
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new RecordingOriginPolicy(false),
            RejectionStatusCode = statusCode
        };

        var result = new Http1ServerHandshakeState().EvaluateRequest(ValidRequest(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Rejected));
            Assert.That(result.Response.StatusCode, Is.EqualTo(statusCode));
            Assert.That(result.Response.HasReasonPhrase, Is.EqualTo(hasReasonPhrase));
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo(reasonPhrase));
            Assert.That(result.Response.Fields, Is.Empty);
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [Test]
    public void AddsCanonicalContentLengthForApplicationRejectionBody()
    {
        var state = new Http1ServerHandshakeState();
        var body = Encoding.UTF8.GetBytes("not found");
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new RecordingOriginPolicy(false),
            RejectionStatusCode = 404,
            RejectionFields = [Field("Content-Type", "text/plain")],
            HasRejectionBody = true,
            RejectionBody = body
        };

        var result = state.EvaluateRequest(ValidRequest(), options);
        body[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Response.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "Content-Type", "Content-Length" }));
            Assert.That(Value(result.Response, "Content-Length"), Is.EqualTo("9"));
            Assert.That(result.Response.HasBody, Is.True);
            Assert.That(Encoding.UTF8.GetString(result.Response.Body), Is.EqualTo("not found"));
            Assert.That(Encoding.UTF8.GetString(state.ResponseBody), Is.EqualTo("not found"));
            Assert.That(Http1Writer.MeasureResponse(result.Response).Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(Serialize(result.Response), Is.EqualTo(
                "HTTP/1.1 404 Not Found\r\n" +
                "Content-Type: text/plain\r\n" +
                "Content-Length: 9\r\n\r\n" +
                "not found"));
        });
    }

    [Test]
    public void ResponseAndRetainedStateDoNotShareMutableFieldsOrBodies()
    {
        var state = new Http1ServerHandshakeState();
        var options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new RecordingOriginPolicy(false),
            RejectionFields = [Field("X-Reason", "original")],
            HasRejectionBody = true,
            RejectionBody = [1, 2]
        };

        var result = state.EvaluateRequest(ValidRequest(), options);
        result.Response.Fields[0].Value = "mutated-result";
        result.Response.Body[0] = 9;
        state.ResponseFields[1].Value = "mutated-state";
        state.ResponseBody[1] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(state.ResponseFields[0].Value, Is.EqualTo("original"));
            Assert.That(state.ResponseBody, Is.EqualTo(new byte[] { 1, 8 }));
            Assert.That(result.Response.Fields[1].Value, Is.EqualTo("2"));
            Assert.That(result.Response.Body, Is.EqualTo(new byte[] { 9, 2 }));
        });
    }

    static string Serialize(WebSocketHttpResponse response)
    {
        var measurement = Http1Writer.MeasureResponse(response);
        var bytes = new byte[measurement.RequiredLength];
        var write = Http1Writer.WriteResponse(response, bytes);
        Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        return Encoding.ASCII.GetString(bytes);
    }

    static string Value(WebSocketHttpResponse response, string fieldName)
    {
        return response.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value;
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

    static void Replace(WebSocketHttpRequest request, string fieldName, string value)
    {
        request.Fields.Single(field => string.Equals(
            field.Name,
            fieldName,
            StringComparison.OrdinalIgnoreCase)).Value = value;
    }
}
