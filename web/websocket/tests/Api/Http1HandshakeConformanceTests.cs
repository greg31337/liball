using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1HandshakeConformanceTests
{
    const string RfcClientKey = "dGhlIHNhbXBsZSBub25jZQ==";
    const string RfcAccept = "s3pPLMBiTxaQ9kYGzzhZRbK+xOo=";

    [Test]
    public void Rfc6455ExampleParsesEvaluatesAndSerializesExactly()
    {
        const string requestText =
            "GET /chat HTTP/1.1\r\n" +
            "Host: server.example.com\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key: " + RfcClientKey + "\r\n" +
            "Origin: http://example.com\r\n" +
            "Sec-WebSocket-Protocol: chat, superchat\r\n" +
            "Sec-WebSocket-Version: 13\r\n\r\n";
        const string responseText =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + RfcAccept + "\r\n" +
            "Sec-WebSocket-Protocol: chat\r\n\r\n";
        IWebSocketHttp1Codec codec = CreateCodec();
        WebSocketHttp1ReadResult parsed = codec.ReadClientRequest(Bytes(requestText));
        IWebSocketServerHandshake server = new WebSocketServerHandshake();
        WebSocketServerHandshakeOptions options = new WebSocketServerHandshakeOptions
        {
            HasOriginPolicy = true,
            OriginPolicy = new AllowAllConformanceOrigins(),
            SupportedSubprotocols = ["chat", "superchat"]
        };

        WebSocketHandshakeResult evaluated = server.EvaluateHttp1Request(parsed.Request, options);
        byte[] response = new byte[codec.MeasureServerResponse(evaluated.Response)];
        int produced = codec.WriteServerResponse(evaluated.Response, response);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(parsed.ConsumedCount, Is.EqualTo(Bytes(requestText).Length));
            Assert.That(evaluated.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(evaluated.ClientKey, Is.EqualTo(RfcClientKey));
            Assert.That(evaluated.SelectedSubprotocol, Is.EqualTo("chat"));
            Assert.That(FieldValue(evaluated.Response, "Sec-WebSocket-Accept"),
                Is.EqualTo(RfcAccept));
            Assert.That(produced, Is.EqualTo(response.Length));
            Assert.That(Encoding.ASCII.GetString(response), Is.EqualTo(responseText));
        });
    }

    [Test]
    public void GeneratedHandshakeRoundTripsWithoutConsumingFirstFrameBytes()
    {
        IWebSocketClientHandshake client = new WebSocketClientHandshake();
        IWebSocketServerHandshake server = new WebSocketServerHandshake();
        IWebSocketHttp1Codec codec = CreateCodec();
        WebSocketClientHandshakeOptions clientOptions = new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat?room=blue",
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            HasOrigin = true,
            Origin = "https://example.com",
            HasUserAgent = true,
            UserAgent = "websocket-tests/1.0",
            ExtraFields = [Field("Cookie", "session=abc")]
        };
        WebSocketHandshakeResult created = client.CreateHttp1Request(clientOptions);
        byte[] encodedRequest = new byte[codec.MeasureClientRequest(created.Request)];
        codec.WriteClientRequest(created.Request, encodedRequest);
        byte[] clientFrame = [0x81, 0x80, 0x00, 0x00, 0x00, 0x00];
        byte[] requestSource = encodedRequest.Concat(clientFrame).ToArray();

        WebSocketHttp1ReadResult parsedRequest = codec.ReadClientRequest(requestSource);
        WebSocketHandshakeResult evaluated = server.EvaluateHttp1Request(
            parsedRequest.Request,
            new WebSocketServerHandshakeOptions());
        byte[] encodedResponse = new byte[codec.MeasureServerResponse(evaluated.Response)];
        codec.WriteServerResponse(evaluated.Response, encodedResponse);
        byte[] serverFrame = [0x81, 0x02, (byte)'o', (byte)'k'];
        byte[] responseSource = encodedResponse.Concat(serverFrame).ToArray();

        WebSocketHttp1ReadResult parsedResponse = codec.ReadServerResponse(responseSource);
        WebSocketHandshakeResult accepted = client.ValidateHttp1Response(parsedResponse.Response);
        WebSocketHandshakeValueResult expectedAccept =
            WebSocketHandshakeCryptography.ComputeAccept(created.ClientKey);

        Assert.Multiple(() =>
        {
            Assert.That(parsedRequest.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(parsedRequest.ConsumedCount, Is.EqualTo(encodedRequest.Length));
            Assert.That(requestSource.AsSpan(parsedRequest.ConsumedCount).ToArray(),
                Is.EqualTo(clientFrame));
            Assert.That(evaluated.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(FieldValue(evaluated.Response, "Sec-WebSocket-Accept"),
                Is.EqualTo(expectedAccept.Value));
            Assert.That(parsedResponse.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(parsedResponse.ConsumedCount, Is.EqualTo(encodedResponse.Length));
            Assert.That(responseSource.AsSpan(parsedResponse.ConsumedCount).ToArray(),
                Is.EqualTo(serverFrame));
            Assert.That(accepted.Outcome, Is.EqualTo(WebSocketHandshakeOutcome.Accepted));
            Assert.That(accepted.ClientKey, Is.EqualTo(created.ClientKey));
            Assert.That(accepted.Request.RequestTarget, Is.EqualTo("/chat?room=blue"));
            Assert.That(FieldValue(accepted.Request, "Origin"),
                Is.EqualTo("https://example.com"));
            Assert.That(FieldValue(accepted.Request, "User-Agent"),
                Is.EqualTo("websocket-tests/1.0"));
            Assert.That(FieldValue(accepted.Request, "Cookie"),
                Is.EqualTo("session=abc"));
        });
    }

    [TestCase("ws://example.com", "/", "example.com")]
    [TestCase("wss://example.com/chat?room=blue", "/chat?room=blue", "example.com")]
    [TestCase("ws://192.0.2.10:8000/socket", "/socket", "192.0.2.10:8000")]
    [TestCase("wss://[2001:0DB8:0:0:0:0:0:1]:8443/chat", "/chat", "[2001:db8::1]:8443")]
    [TestCase("wss://BÜCHER.example:443/a/../chat?q=naïve", "/chat?q=na%C3%AFve", "xn--bcher-kva.example:443")]
    public void GeneratedRequestsUseCanonicalOriginFormAndHost(
        string uri,
        string expectedTarget,
        string expectedHost)
    {
        IWebSocketClientHandshake handshake = new WebSocketClientHandshake();
        WebSocketClientHandshakeOptions options = new WebSocketClientHandshakeOptions
        {
            Uri = uri,
            Transport = WebSocketHandshakeTransport.Http1Upgrade
        };

        WebSocketHandshakeResult result = handshake.CreateHttp1Request(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.Request.RequestTarget, Is.EqualTo(expectedTarget));
            Assert.That(result.Request.HttpVersion, Is.EqualTo("HTTP/1.1"));
            Assert.That(FieldValue(result.Request, "Host"), Is.EqualTo(expectedHost));
            Assert.That(FieldValue(result.Request, "Upgrade"), Is.EqualTo("websocket"));
            Assert.That(FieldValue(result.Request, "Connection"), Is.EqualTo("Upgrade"));
            Assert.That(FieldValue(result.Request, "Sec-WebSocket-Version"), Is.EqualTo("13"));
            Assert.That(FieldValue(result.Request, "Sec-WebSocket-Key"),
                Is.EqualTo(result.ClientKey));
        });
    }

    static IWebSocketHttp1Codec CreateCodec()
    {
        return new WebSocketHttp1Codec(4096, 32, 1024);
    }

    static string FieldValue(WebSocketHttpRequest request, string name)
    {
        return request.Fields.Single(field => string.Equals(
            field.Name,
            name,
            StringComparison.OrdinalIgnoreCase)).Value;
    }

    static string FieldValue(WebSocketHttpResponse response, string name)
    {
        return response.Fields.Single(field => string.Equals(
            field.Name,
            name,
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

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }
}

internal class AllowAllConformanceOrigins : IWebSocketOriginPolicy
{
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        return true;
    }
}
