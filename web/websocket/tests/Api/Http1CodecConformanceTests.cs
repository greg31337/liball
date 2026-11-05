using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1CodecConformanceTests
{
    [Test]
    public void ReadsRepeatedFieldsAndPreservesQuotedAndOpaqueValues()
    {
        const string requestText =
            "GET /chat HTTP/1.1\r\n" +
            "X-List: one\r\n" +
            "x-list: two\r\n" +
            "X-Quoted: token, \"a,b;\\\"c\"\r\n" +
            "X-Obs: \u0080\u00ff\r\n\r\n";
        const string responseText =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Set-Cookie: one=1\r\n" +
            "Set-Cookie: two=2\r\n\r\n";
        byte[] requestBytes = Bytes(requestText);
        byte[] responseBytes = Bytes(responseText);
        byte[] requestSource = requestBytes.Concat(new byte[] { 0x81, 0x00 }).ToArray();
        byte[] responseSource = responseBytes.Concat(new byte[] { 0x89, 0x00 }).ToArray();
        WebSocketHttp1Codec codec = CreateCodec();

        WebSocketHttp1ReadResult request = codec.ReadClientRequest(requestSource);
        WebSocketHttp1ReadResult response = codec.ReadServerResponse(responseSource);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(request.ConsumedCount, Is.EqualTo(requestBytes.Length));
            Assert.That(request.Request.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "X-List", "x-list", "X-Quoted", "X-Obs" }));
            Assert.That(request.Request.Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "one", "two", "token, \"a,b;\\\"c\"", "\u0080\u00ff" }));
            Assert.That(response.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(response.ConsumedCount, Is.EqualTo(responseBytes.Length));
            Assert.That(response.Response.Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "one=1", "two=2" }));
        });
    }

    [Test]
    public void RequestAndResponseReadsCompleteAtEveryInputSplit()
    {
        byte[] request = Bytes(
            "GET /chat HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "Connection: keep-alive, Upgrade\r\n\r\n");
        byte[] response = Bytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n\r\n");

        for (int split = 0; split <= request.Length; split++)
        {
            WebSocketHttp1Codec codec = CreateCodec();
            WebSocketHttp1ReadResult first = codec.ReadClientRequest(request.AsSpan(0, split));
            WebSocketHttp1ReadResult final = first;
            int consumed = first.ConsumedCount;
            if (first.Status != WebSocketParseStatus.Complete)
            {
                final = codec.ReadClientRequest(request.AsSpan(split));
                consumed += final.ConsumedCount;
            }

            Assert.Multiple(() =>
            {
                Assert.That(first.ConsumedCount, Is.EqualTo(split), $"request split {split}");
                Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete), $"request split {split}");
                Assert.That(consumed, Is.EqualTo(request.Length), $"request split {split}");
                Assert.That(final.Request.RequestTarget, Is.EqualTo("/chat"), $"request split {split}");
            });
        }

        for (int split = 0; split <= response.Length; split++)
        {
            WebSocketHttp1Codec codec = CreateCodec();
            WebSocketHttp1ReadResult first = codec.ReadServerResponse(response.AsSpan(0, split));
            WebSocketHttp1ReadResult final = first;
            int consumed = first.ConsumedCount;
            if (first.Status != WebSocketParseStatus.Complete)
            {
                final = codec.ReadServerResponse(response.AsSpan(split));
                consumed += final.ConsumedCount;
            }

            Assert.Multiple(() =>
            {
                Assert.That(first.ConsumedCount, Is.EqualTo(split), $"response split {split}");
                Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete), $"response split {split}");
                Assert.That(consumed, Is.EqualTo(response.Length), $"response split {split}");
                Assert.That(final.Response.StatusCode, Is.EqualTo(101), $"response split {split}");
            });
        }
    }

    [Test]
    public void RejectsBareNewlinesObsFoldAndUnsafeFieldOctets()
    {
        string[][] requestCases =
        [
            ["GET / HTTP/1.1\n\n", "http-line-ending-invalid"],
            ["GET / HTTP/1.1\r\nX: one\r\n two\r\n\r\n", "http-header-obs-fold"],
            ["GET / HTTP/1.1\r\nX: a\0b\r\n\r\n", "http-header-value-invalid"],
            ["GET / HTTP/1.1\r\nX: a\u0001b\r\n\r\n", "http-header-value-invalid"],
            ["GET / HTTP/1.1\r\nX: safe\nInjected: yes\r\n\r\n", "http-line-ending-invalid"]
        ];
        string[][] responseCases =
        [
            ["HTTP/1.1 200 OK\n\n", "http-line-ending-invalid"],
            ["HTTP/1.1 200 OK\r\nX: one\r\n\ttwo\r\n\r\n", "http-header-obs-fold"],
            ["HTTP/1.1 200 OK\r\nX: a\0b\r\n\r\n", "http-header-value-invalid"],
            ["HTTP/1.1 200 OK\r\nX: a\u001fb\r\n\r\n", "http-header-value-invalid"],
            ["HTTP/1.1 200 OK\r\nX: safe\nInjected: yes\r\n\r\n", "http-line-ending-invalid"]
        ];

        foreach (string[] testCase in requestCases)
        {
            WebSocketHttp1ReadResult result = CreateCodec().ReadClientRequest(Bytes(testCase[0]));
            AssertFailure(result, testCase[1], 400);
        }

        foreach (string[] testCase in responseCases)
        {
            WebSocketHttp1ReadResult result = CreateCodec().ReadServerResponse(Bytes(testCase[0]));
            AssertFailure(result, testCase[1], 502);
        }
    }

    [Test]
    public void PublicCodecAcceptsExactLimitsAndRejectsTheFirstExcess()
    {
        byte[] requestWithoutFields = Bytes("GET / HTTP/1.1\r\n\r\n");
        byte[] responseWithoutFields = Bytes("HTTP/1.1 200 \r\n\r\n");
        WebSocketHttp1ReadResult exactRequestBytes =
            new WebSocketHttp1Codec(requestWithoutFields.Length, 1, 14)
                .ReadClientRequest(requestWithoutFields);
        WebSocketHttp1ReadResult excessRequestBytes =
            new WebSocketHttp1Codec(requestWithoutFields.Length - 1, 1, 14)
                .ReadClientRequest(requestWithoutFields);
        WebSocketHttp1ReadResult exactResponseBytes =
            new WebSocketHttp1Codec(responseWithoutFields.Length, 1, 13)
                .ReadServerResponse(responseWithoutFields);
        WebSocketHttp1ReadResult excessResponseBytes =
            new WebSocketHttp1Codec(responseWithoutFields.Length - 1, 1, 13)
                .ReadServerResponse(responseWithoutFields);

        byte[] requestWithFields = Bytes(
            "GET / HTTP/1.1\r\nOne: 1\r\nTwo: 2\r\n\r\n");
        byte[] responseWithFields = Bytes(
            "HTTP/1.1 200 OK\r\nOne: 1\r\nTwo: 2\r\n\r\n");
        WebSocketHttp1ReadResult exactRequestCount =
            new WebSocketHttp1Codec(128, 2, 64).ReadClientRequest(requestWithFields);
        WebSocketHttp1ReadResult excessRequestCount =
            new WebSocketHttp1Codec(128, 1, 64).ReadClientRequest(requestWithFields);
        WebSocketHttp1ReadResult exactResponseCount =
            new WebSocketHttp1Codec(128, 2, 64).ReadServerResponse(responseWithFields);
        WebSocketHttp1ReadResult excessResponseCount =
            new WebSocketHttp1Codec(128, 1, 64).ReadServerResponse(responseWithFields);

        Assert.Multiple(() =>
        {
            Assert.That(exactRequestBytes.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exactResponseBytes.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exactRequestCount.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exactRequestCount.Request.Fields, Has.Length.EqualTo(2));
            Assert.That(exactResponseCount.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exactResponseCount.Response.Fields, Has.Length.EqualTo(2));
        });
        AssertLimit(excessRequestBytes, "http-header-bytes-too-large");
        AssertLimit(excessResponseBytes, "http-header-bytes-too-large");
        AssertLimit(excessRequestCount, "http-header-count-too-large");
        AssertLimit(excessResponseCount, "http-header-count-too-large");
    }

    [Test]
    public void WritesRejectCrLfInjectionOnEveryMetadataSurfaceWithoutOutput()
    {
        AssertRequestInjectionRejected(request => request.Method = "GET\r\nInjected");
        AssertRequestInjectionRejected(request => request.RequestTarget = "/chat\r\nInjected: yes");
        AssertRequestInjectionRejected(request => request.Fields[0].Name = "Host\r\nInjected");
        AssertRequestInjectionRejected(request => request.Fields[0].Value = "example.test\r\nInjected: yes");
        AssertResponseInjectionRejected(response => response.ReasonPhrase = "OK\r\nInjected: yes");
        AssertResponseInjectionRejected(response => response.Fields[0].Name = "Upgrade\r\nInjected");
        AssertResponseInjectionRejected(response => response.Fields[0].Value = "websocket\r\nInjected: yes");
    }

    static void AssertRequestInjectionRejected(Action<WebSocketHttpRequest> mutate)
    {
        WebSocketHttpRequest request = Request();
        mutate(request);
        byte[] destination = Enumerable.Repeat((byte)0xAA, 128).ToArray();
        WebSocketHttp1Codec codec = CreateCodec();

        Assert.Throws<WebSocketHandshakeException>(() => codec.MeasureClientRequest(request));
        Assert.Throws<WebSocketHandshakeException>(() => codec.WriteClientRequest(request, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, destination.Length)));
    }

    static void AssertResponseInjectionRejected(Action<WebSocketHttpResponse> mutate)
    {
        WebSocketHttpResponse response = Response();
        mutate(response);
        byte[] destination = Enumerable.Repeat((byte)0xBB, 128).ToArray();
        WebSocketHttp1Codec codec = CreateCodec();

        Assert.Throws<WebSocketHandshakeException>(() => codec.MeasureServerResponse(response));
        Assert.Throws<WebSocketHandshakeException>(() => codec.WriteServerResponse(response, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xBB, destination.Length)));
    }

    static void AssertFailure(WebSocketHttp1ReadResult result, string identifier, int statusCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(statusCode));
        });
    }

    static void AssertLimit(WebSocketHttp1ReadResult result, string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(431));
        });
    }

    static WebSocketHttpRequest Request()
    {
        return new WebSocketHttpRequest
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            Method = "GET",
            HasRequestTarget = true,
            RequestTarget = "/chat",
            HasHttpVersion = true,
            HttpVersion = "HTTP/1.1",
            Fields = [Field("Host", "example.test")]
        };
    }

    static WebSocketHttpResponse Response()
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 101,
            HasReasonPhrase = true,
            ReasonPhrase = "Switching Protocols",
            Fields = [Field("Upgrade", "websocket")]
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

    static WebSocketHttp1Codec CreateCodec()
    {
        return new WebSocketHttp1Codec(1024, 16, 256);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }
}
