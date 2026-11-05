using System.Text;

namespace http.Tests;

public class Http2ConnectMessageTests
{
    [Test]
    public void SuccessfulStandardConnectActivatesBidirectionalTunnelData()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult request = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "CONNECT"),
                Field(":authority", "server.example:443")),
            false,
            false);
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            1, Headers(Field(":status", "200")), false);
        byte[] clientBytes = { 0x01, 0x00, 0xff };
        byte[] serverBytes = { 0x02, 0xfe, 0x00 };
        Http2MessageResult clientData = codec.DecodeHttp2Content(
            1, HttpMessageKind.Request, clientBytes, true);
        Http2MessageResult serverData = codec.DecodeHttp2Content(
            1, HttpMessageKind.Response, serverBytes, true);

        clientBytes[0] = 0x7f;
        serverBytes[0] = 0x7f;

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(request.IsTunnel, Is.False);
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.IsTunnel, Is.True);
            AssertTunnelData(
                clientData, new byte[] { 0x01, 0x00, 0xff });
            AssertTunnelData(
                serverData, new byte[] { 0x02, 0xfe, 0x00 });
            Assert.That(request.Request!.Content, Is.Empty);
            Assert.That(response.Response!.Content, Is.Empty);
        });
    }

    [Test]
    public void StandardConnectTunnelRejectsTrailersAndClosedDirections()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        codec.DecodeHttp2RequestHeaders(
            3,
            Headers(
                Field(":method", "CONNECT"),
                Field(":authority", "server.example:443")),
            false,
            false);
        codec.DecodeHttp2ResponseHeaders(
            3, Headers(Field(":status", "204")), false);
        Http2MessageResult trailers = codec.DecodeHttp2Trailers(
            3, Headers(Field("x-last", "value")), true);
        Http2MessageResult completed = codec.DecodeHttp2Content(
            3, HttpMessageKind.Request, Array.Empty<byte>(), true);
        Http2MessageResult afterCompletion = codec.DecodeHttp2Content(
            3, HttpMessageKind.Request, new byte[] { 1 }, false);
        Http2MessageResult otherDirection = codec.DecodeHttp2Content(
            3, HttpMessageKind.Response, new byte[] { 2 }, false);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                trailers, "http2-tunnel-trailers-not-allowed");
            Assert.That(completed.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.IsComplete, Is.True);
            Assert.That(completed.IsTunnel, Is.True);
            AssertMalformed(
                afterCompletion, "http2-message-already-complete");
            Assert.That(otherDirection.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(otherDirection.IsTunnel, Is.True);
        });
    }

    [Test]
    public void UnsuccessfulStandardConnectRetainsHttpResponseSemantics()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        codec.DecodeHttp2RequestHeaders(
            5,
            Headers(
                Field(":method", "CONNECT"),
                Field(":authority", "server.example:443")),
            false,
            false);
        Http2MessageResult response = codec.DecodeHttp2ResponseHeaders(
            5,
            Headers(
                Field(":status", "403"),
                Field("content-length", "3")),
            false);
        Http2MessageResult content = codec.DecodeHttp2Content(
            5,
            HttpMessageKind.Response,
            Encoding.ASCII.GetBytes("no!"),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.IsTunnel, Is.False);
            Assert.That(content.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(content.IsTunnel, Is.False);
            Assert.That(Encoding.ASCII.GetString(content.Response!.Content),
                Is.EqualTo("no!"));
        });
    }

    [Test]
    public void PeerMessageFailuresAndLocalEncodingFailuresRemainDistinct()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult malformed = codec.DecodeHttp2RequestHeaders(
            7,
            Headers(
                Field(":method", "GET"),
                Field(":scheme", "https"),
                Field(":authority", "server.example"),
                Field(":path", "/"),
                Field("connection", "close")),
            true,
            false);
        HttpRequest local = new HttpRequest();
        local.Method = HttpMethods.Get;
        local.Scheme = "https";
        local.Authority = "server.example";
        local.Target = "/";
        local.Headers = Headers(Field("Connection", "close"));
        Http2MessageResult rejected = codec.EncodeHttp2RequestHeaders(
            9, local, true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                malformed, "http2-connection-specific-field");
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCodeName,
                Is.EqualTo("http2-connection-specific-field"));
            Assert.That(rejected.IsMalformed, Is.False);
            Assert.That(rejected.ErrorScope,
                Is.EqualTo(Http2ErrorScope.None));
            Assert.That(rejected.HasErrorCode, Is.False);
            Assert.That(rejected.ErrorCodeValue, Is.Zero);
            Assert.That(rejected.IsKnownErrorCode, Is.False);
        });
    }

    static void AssertTunnelData(
        Http2MessageResult result,
        byte[] expected)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        Assert.That(result.IsTunnel, Is.True);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Content, Is.EqualTo(expected));
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
        Assert.That(result.ErrorCodeValue,
            Is.EqualTo((uint)Http2ErrorCode.ProtocolError));
        Assert.That(result.IsKnownErrorCode, Is.True);
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
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
