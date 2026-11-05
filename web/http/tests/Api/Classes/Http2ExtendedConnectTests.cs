using System.Text;

namespace http.Tests;

public class Http2ExtendedConnectTests
{
    [Test]
    public void NegotiatedExtendedConnectMapsAllControlData()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                1,
                Headers(
                    Field(":method", "CONNECT"),
                    Field(":protocol", "websocket"),
                    Field(":scheme", "https"),
                    Field(":authority", "server.example"),
                    Field(":path", "/chat?room=one"),
                    Field("sec-websocket-version", "13")),
                false,
                true);
        HttpRequest request = result.Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(result.IsTunnel, Is.True);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(request.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(request.Method, Is.EqualTo(HttpMethods.Connect));
            Assert.That(request.ConnectProtocol,
                Is.EqualTo(HttpProtocolNames.WebSocket));
            Assert.That(request.Scheme, Is.EqualTo("https"));
            Assert.That(request.Authority, Is.EqualTo("server.example"));
            Assert.That(request.Target, Is.EqualTo("/chat?room=one"));
            Assert.That(request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(request.Path, Is.EqualTo("/chat"));
            Assert.That(request.HasQuery, Is.True);
            Assert.That(request.Query, Is.EqualTo("room=one"));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ProtocolRequiresNegotiationAndTheConnectMethod()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult disabled = codec.DecodeHttp2RequestHeaders(
            3, ExtendedHeaders("websocket"), false, false);
        Http2MessageResult wrongMethod = codec.DecodeHttp2RequestHeaders(
            5,
            Headers(
                Field(":method", "GET"),
                Field(":protocol", "websocket"),
                Field(":scheme", "https"),
                Field(":authority", "server.example"),
                Field(":path", "/chat")),
            false,
            true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                disabled, "http2-connect-protocol-not-enabled");
            AssertMalformed(
                wrongMethod, "http2-protocol-requires-connect");
        });
    }

    [Test]
    public void ExtendedConnectRequiresUniqueValidProtocolAndTargetFields()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult duplicate = codec.DecodeHttp2RequestHeaders(
            7,
            Headers(
                Field(":method", "CONNECT"),
                Field(":protocol", "websocket"),
                Field(":protocol", "bytestream"),
                Field(":scheme", "https"),
                Field(":authority", "server.example"),
                Field(":path", "/chat")),
            false,
            true);
        string[] invalidProtocols = { "", "web/socket", "web socket" };

        Assert.Multiple(() =>
        {
            AssertMalformed(
                duplicate, "duplicate-http2-request-pseudo-header");
            int index = 0;
            while (index < invalidProtocols.Length)
            {
                Http2MessageResult invalid = codec.DecodeHttp2RequestHeaders(
                    9 + index * 2,
                    ExtendedHeaders(invalidProtocols[index]),
                    false,
                    true);
                AssertMalformed(
                    invalid, "invalid-http2-connect-protocol");
                index = index + 1;
            }
        });
    }

    [Test]
    public void ExtendedConnectRequiresSchemeAuthorityAndPath()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult missingScheme = codec.DecodeHttp2RequestHeaders(
            15,
            Headers(
                Field(":method", "CONNECT"),
                Field(":protocol", "websocket"),
                Field(":authority", "server.example"),
                Field(":path", "/chat")),
            false,
            true);
        Http2MessageResult missingAuthority = codec.DecodeHttp2RequestHeaders(
            17,
            Headers(
                Field(":method", "CONNECT"),
                Field(":protocol", "websocket"),
                Field(":scheme", "https"),
                Field(":path", "/chat")),
            false,
            true);
        Http2MessageResult missingPath = codec.DecodeHttp2RequestHeaders(
            19,
            Headers(
                Field(":method", "CONNECT"),
                Field(":protocol", "websocket"),
                Field(":scheme", "https"),
                Field(":authority", "server.example")),
            false,
            true);
        Http2MessageResult malformedPath = codec.DecodeHttp2RequestHeaders(
            21,
            Headers(
                Field(":method", "CONNECT"),
                Field(":protocol", "websocket"),
                Field(":scheme", "https"),
                Field(":authority", "server.example"),
                Field(":path", "chat")),
            false,
            true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                missingScheme,
                "invalid-http2-extended-connect-pseudo-headers");
            AssertMalformed(
                missingAuthority,
                "invalid-http2-extended-connect-pseudo-headers");
            AssertMalformed(
                missingPath,
                "invalid-http2-extended-connect-pseudo-headers");
            AssertMalformed(
                malformedPath,
                "invalid-http2-extended-connect-pseudo-headers");
        });
    }

    [Test]
    public void StandardConnectRemainsDistinctWhenTheExtensionIsEnabled()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                23,
                Headers(
                    Field(":method", "CONNECT"),
                    Field(":authority", "server.example:443")),
                false,
                true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTunnel, Is.False);
            Assert.That(result.Request!.ConnectProtocol, Is.Null);
            Assert.That(result.Request.Target,
                Is.EqualTo("server.example:443"));
            Assert.That(result.Request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Authority));
        });
    }

    [Test]
    public void SuccessfulResponseActivatesRawBidirectionalTunnelData()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult request = codec.DecodeHttp2RequestHeaders(
            25, ExtendedHeaders("websocket"), false, true);
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            25, Headers(Field(":status", "200")), false);
        byte[] clientBytes = { 0x81, 0x00, 0xff };
        byte[] serverBytes = { 0x00, 0xfe, 0x7f };
        Http2MessageResult clientData = codec.DecodeHttp2Content(
            25, HttpMessageKind.Request, clientBytes, false);
        Http2MessageResult serverData = codec.DecodeHttp2Content(
            25, HttpMessageKind.Response, serverBytes, false);
        Http2MessageResult trailers = codec.DecodeHttp2Trailers(
            25, Headers(Field("x-last", "value")), true);

        clientBytes[0] = 0;
        serverBytes[0] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(request.IsTunnel, Is.True);
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.IsTunnel, Is.True);
            Assert.That(clientData.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(clientData.IsTunnel, Is.True);
            Assert.That(clientData.Content,
                Is.EqualTo(new byte[] { 0x81, 0x00, 0xff }));
            Assert.That(serverData.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serverData.IsTunnel, Is.True);
            Assert.That(serverData.Content,
                Is.EqualTo(new byte[] { 0x00, 0xfe, 0x7f }));
            Assert.That(request.Request!.Content, Is.Empty);
            Assert.That(response.Response!.Content, Is.Empty);
            AssertMalformed(
                trailers, "http2-tunnel-trailers-not-allowed");
        });
    }

    [Test]
    public void RejectedExtendedConnectResponseRemainsAnHttpResponse()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        codec.DecodeHttp2RequestHeaders(
            27, ExtendedHeaders("custom-protocol"), false, true);
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            27,
            Headers(
                Field(":status", "403"),
                Field("content-length", "3")),
            false);
        Http2MessageResult content = codec.DecodeHttp2Content(
            27,
            HttpMessageKind.Response,
            Encoding.ASCII.GetBytes("no!"),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.IsTunnel, Is.False);
            Assert.That(content.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(content.IsTunnel, Is.False);
            Assert.That(Text(content.Response!.Content), Is.EqualTo("no!"));
        });
    }

    [Test]
    public void SuccessfulTunnelResponseRejectsContentLengthMetadata()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        codec.DecodeHttp2RequestHeaders(
            29, ExtendedHeaders("websocket"), false, true);
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            29,
            Headers(
                Field(":status", "200"),
                Field("content-length", "0")),
            false);

        AssertMalformed(response, "http2-content-length-not-allowed");
    }

    static HttpHeaders ExtendedHeaders(string protocol)
    {
        return Headers(
            Field(":method", "CONNECT"),
            Field(":protocol", protocol),
            Field(":scheme", "https"),
            Field(":authority", "server.example"),
            Field(":path", "/chat"));
    }

    static void AssertMalformed(
        Http2MessageResult result,
        string errorCodeName)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.IsMalformed, Is.True);
        Assert.That(result.ErrorScope, Is.EqualTo(Http2ErrorScope.Stream));
        Assert.That(result.HasErrorCode, Is.True);
        Assert.That(result.ErrorCode,
            Is.EqualTo(Http2ErrorCode.ProtocolError));
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }

    static HttpHeaders Headers(params HttpHeaderField[] fields)
    {
        HttpHeaders headers = new HttpHeaders();
        headers.Fields.AddRange(fields);
        return headers;
    }

    static HttpHeaderField Field(string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        return field;
    }
}
