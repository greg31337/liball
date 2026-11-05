using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1CodecTests
{
    [TestCase(0, 1, 1)]
    [TestCase(-1, 1, 1)]
    [TestCase(1, 0, 1)]
    [TestCase(1, -1, 1)]
    [TestCase(1, 1, 0)]
    [TestCase(1, 1, -1)]
    [TestCase(8, 1, 9)]
    public void ConstructorRejectsInvalidLimits(int maximumBytes, int maximumCount, int maximumLine)
    {
        Assert.Throws<WebSocketLimitException>(() =>
            new WebSocketHttp1Codec(maximumBytes, maximumCount, maximumLine));
    }

    [Test]
    public void RequestAndResponseReadsAdvanceIndependently()
    {
        var codec = CreateCodec();
        var requestFirst = codec.ReadClientRequest(Bytes("GET /chat HTTP/1.1\r\nHost:"));

        var response = codec.ReadServerResponse(Bytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n\r\n"));
        var requestFinal = codec.ReadClientRequest(Bytes(" example.test\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(requestFirst.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(response.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(response.Response.StatusCode, Is.EqualTo(101));
            Assert.That(requestFinal.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(requestFinal.Request.RequestTarget, Is.EqualTo("/chat"));
            Assert.That(requestFinal.Request.Fields.Single().Value, Is.EqualTo("example.test"));
        });
    }

    [Test]
    public void ResponseReadSurfacesInformationalThenFinalAndPreservesFrameBytes()
    {
        var codec = CreateCodec();
        var interimBytes = Bytes("HTTP/1.1 103 Early Hints\r\nLink: one\r\n\r\n");
        var finalBytes = Bytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\n\r\n");
        var source = interimBytes.Concat(finalBytes).Concat(new byte[] { 0x81, 0x00 }).ToArray();

        var interim = codec.ReadServerResponse(source);
        var final = codec.ReadServerResponse(source.AsSpan(interim.ConsumedCount));

        Assert.Multiple(() =>
        {
            Assert.That(interim.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(interim.ConsumedCount, Is.EqualTo(interimBytes.Length));
            Assert.That(interim.Response.StatusCode, Is.EqualTo(103));
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.ConsumedCount, Is.EqualTo(finalBytes.Length));
            Assert.That(final.Response.StatusCode, Is.EqualTo(101));
            Assert.That(source.Length - interim.ConsumedCount - final.ConsumedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void ResetClearsBothPartialAndFailedReadStates()
    {
        var codec = CreateCodec();
        codec.ReadClientRequest(Bytes("GET /old HTTP/1.1\r"));
        var failed = codec.ReadServerResponse(Bytes("HTTP/1.1 999 Invalid\r\n\r\n"));

        codec.ResetHttpRead();
        var request = codec.ReadClientRequest(Bytes("GET /new HTTP/1.1\r\n\r\n"));
        var response = codec.ReadServerResponse(Bytes("HTTP/1.1 204 \r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(failed.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(failed.HasError, Is.True);
            Assert.That(failed.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(failed.Error.Identifier,
                Is.EqualTo("http-response-status-line-invalid"));
            Assert.That(failed.Error.HasHttpStatusCode, Is.True);
            Assert.That(failed.Error.HttpStatusCode, Is.EqualTo(502));
            Assert.That(failed.Error.HasCloseCode, Is.False);
            Assert.That(request.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(request.Request.RequestTarget, Is.EqualTo("/new"));
            Assert.That(response.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(response.Response.StatusCode, Is.EqualTo(204));
        });
    }

    [Test]
    public void MalformedReadsReturnStableSideSpecificHttpErrors()
    {
        WebSocketHttp1Codec codec = CreateCodec();

        WebSocketHttp1ReadResult request =
            codec.ReadClientRequest(Bytes("INVALID\r\n\r\n"));
        WebSocketHttp1ReadResult response =
            codec.ReadServerResponse(Bytes("HTTP/1.1 999 Invalid\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(request.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(request.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(request.Error.Identifier,
                Is.EqualTo("http-request-line-invalid"));
            Assert.That(request.Error.HasHttpStatusCode, Is.True);
            Assert.That(request.Error.HttpStatusCode, Is.EqualTo(400));
            Assert.That(request.Error.HasCloseCode, Is.False);
            Assert.That(response.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(response.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Handshake));
            Assert.That(response.Error.Identifier,
                Is.EqualTo("http-response-status-line-invalid"));
            Assert.That(response.Error.HasHttpStatusCode, Is.True);
            Assert.That(response.Error.HttpStatusCode, Is.EqualTo(502));
            Assert.That(response.Error.HasCloseCode, Is.False);
        });
    }

    [Test]
    public void PublicReadResultsDoNotExposeParserOwnedMetadata()
    {
        var codec = CreateCodec();
        var first = codec.ReadClientRequest(Bytes("GET / HTTP/1.1\r\nHost: original\r\n\r\n"));
        first.Request.Method = "MUTATED";
        first.Request.Fields[0].Value = "mutated";

        var repeated = codec.ReadClientRequest(ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.Request.Method, Is.EqualTo("GET"));
            Assert.That(repeated.Request.Fields[0].Value, Is.EqualTo("original"));
        });
    }

    [Test]
    public void MeasuresCanonicalRequestAndResponseLengths()
    {
        var codec = CreateCodec();
        var request = Request();
        var response = Response();
        const string requestText =
            "GET /chat HTTP/1.1\r\n" +
            "Host: example.test\r\n\r\n";
        const string responseText =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n\r\n";

        Assert.Multiple(() =>
        {
            Assert.That(codec.MeasureClientRequest(request), Is.EqualTo(requestText.Length));
            Assert.That(codec.MeasureServerResponse(response), Is.EqualTo(responseText.Length));
        });
    }

    [Test]
    public void ClientRequestDrainsAcrossZeroAndTinyDestinations()
    {
        var codec = CreateCodec();
        var request = Request();
        const string expected =
            "GET /chat HTTP/1.1\r\n" +
            "Host: example.test\r\n\r\n";
        var output = new List<byte>();

        Assert.That(codec.WriteClientRequest(request, Span<byte>.Empty), Is.Zero);
        var destinationSizes = new[] { 1, 2, 3, 5, 8 };
        var sizeIndex = 0;
        while (output.Count < expected.Length)
        {
            var destination = Enumerable.Repeat((byte)0xAA, destinationSizes[sizeIndex % destinationSizes.Length]).ToArray();
            var produced = codec.WriteClientRequest(request, destination);
            output.AddRange(destination.AsSpan(0, produced).ToArray());
            if (produced < destination.Length)
            {
                Assert.That(destination.AsSpan(produced).ToArray(),
                    Is.EqualTo(Enumerable.Repeat((byte)0xAA, destination.Length - produced)));
            }

            sizeIndex++;
        }

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void ServerResponseIncludingBodyDrainsAcrossTinyDestinations()
    {
        var codec = CreateCodec();
        var response = RejectionResponse();
        const string expected =
            "HTTP/1.1 403 Forbidden\r\n" +
            "Content-Length: 4\r\n\r\n" +
            "nope";
        var output = new List<byte>();

        while (output.Count < expected.Length)
        {
            var destination = new byte[4];
            var produced = codec.WriteServerResponse(response, destination);
            output.AddRange(destination.AsSpan(0, produced).ToArray());
        }

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void PartialRequestWriteUsesFrozenCanonicalSnapshot()
    {
        var codec = CreateCodec();
        var request = Request();
        const string expected =
            "GET /chat HTTP/1.1\r\n" +
            "Host: example.test\r\n\r\n";
        var output = new List<byte>();
        var firstDestination = new byte[1];
        var firstProduced = codec.WriteClientRequest(request, firstDestination);
        output.AddRange(firstDestination.AsSpan(0, firstProduced).ToArray());
        request.Method = "POST";
        request.RequestTarget = "/mutated";
        request.Fields[0].Value = "mutated.test";

        while (output.Count < expected.Length)
        {
            var destination = new byte[7];
            var produced = codec.WriteClientRequest(request, destination);
            output.AddRange(destination.AsSpan(0, produced).ToArray());
        }

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void PartialResponseWriteUsesFrozenBodyAndFieldSnapshot()
    {
        var codec = CreateCodec();
        var response = RejectionResponse();
        const string expected =
            "HTTP/1.1 403 Forbidden\r\n" +
            "Content-Length: 4\r\n\r\n" +
            "nope";
        var output = new List<byte>();
        var firstDestination = new byte[2];
        var firstProduced = codec.WriteServerResponse(response, firstDestination);
        output.AddRange(firstDestination.AsSpan(0, firstProduced).ToArray());
        response.ReasonPhrase = "Mutated";
        response.Fields[0].Value = "7";
        response.Body[0] = (byte)'X';

        while (output.Count < expected.Length)
        {
            var destination = new byte[6];
            var produced = codec.WriteServerResponse(response, destination);
            output.AddRange(destination.AsSpan(0, produced).ToArray());
        }

        Assert.That(Encoding.ASCII.GetString(output.ToArray()), Is.EqualTo(expected));
    }

    [Test]
    public void CannotSwitchObjectsDuringAPartialWrite()
    {
        var codec = CreateCodec();
        var firstRequest = Request();
        var secondRequest = Request();
        var firstResponse = Response();
        var secondResponse = Response();
        codec.WriteClientRequest(firstRequest, new byte[1]);
        codec.WriteServerResponse(firstResponse, new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                codec.WriteClientRequest(secondRequest, new byte[1]));
            Assert.Throws<WebSocketStateException>(() =>
                codec.WriteServerResponse(secondResponse, new byte[1]));
            Assert.That(codec.WriteClientRequest(firstRequest, new byte[1]), Is.EqualTo(1));
            Assert.That(codec.WriteServerResponse(firstResponse, new byte[1]), Is.EqualTo(1));
        });
    }

    [Test]
    public void RequestAndResponseWritesHaveIndependentPartialState()
    {
        var codec = CreateCodec();
        var request = Request();
        var response = Response();
        var requestDestination = new byte[3];
        var responseDestination = new byte[5];

        var requestProduced = codec.WriteClientRequest(request, requestDestination);
        var responseProduced = codec.WriteServerResponse(response, responseDestination);
        var nextRequestProduced = codec.WriteClientRequest(request, requestDestination);
        var nextResponseProduced = codec.WriteServerResponse(response, responseDestination);

        Assert.Multiple(() =>
        {
            Assert.That(requestProduced, Is.EqualTo(3));
            Assert.That(responseProduced, Is.EqualTo(5));
            Assert.That(nextRequestProduced, Is.EqualTo(3));
            Assert.That(nextResponseProduced, Is.EqualTo(5));
        });
    }

    [Test]
    public void CompletedWriteCanStartAgainWithTheSameObject()
    {
        var codec = CreateCodec();
        var request = Request();
        var length = codec.MeasureClientRequest(request);
        var first = new byte[length];
        var second = new byte[length];

        var firstProduced = codec.WriteClientRequest(request, first);
        var secondProduced = codec.WriteClientRequest(request, second);

        Assert.Multiple(() =>
        {
            Assert.That(firstProduced, Is.EqualTo(length));
            Assert.That(secondProduced, Is.EqualTo(length));
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [Test]
    public void InvalidWriteMetadataMapsToHandshakeExceptionWithoutOutput()
    {
        var codec = CreateCodec();
        var invalidRequest = Request();
        invalidRequest.Fields[0].Value = "safe\r\nInjected: yes";
        var invalidResponse = RejectionResponse();
        invalidResponse.Fields[0].Value = "3";
        var requestDestination = Enumerable.Repeat((byte)0xAA, 128).ToArray();
        var responseDestination = Enumerable.Repeat((byte)0xBB, 128).ToArray();

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.MeasureClientRequest(invalidRequest));
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.MeasureServerResponse(invalidResponse));
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.WriteClientRequest(invalidRequest, requestDestination));
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.WriteServerResponse(invalidResponse, responseDestination));
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.MeasureClientRequest(null!));
            Assert.Throws<WebSocketHandshakeException>(() =>
                codec.WriteServerResponse(null!, responseDestination));
            Assert.That(requestDestination, Is.EqualTo(Enumerable.Repeat((byte)0xAA, 128)));
            Assert.That(responseDestination, Is.EqualTo(Enumerable.Repeat((byte)0xBB, 128)));
        });
    }

    [Test]
    public void ConfiguredReadLimitsRemainExceptionlessParseResults()
    {
        var codec = new WebSocketHttp1Codec(64, 1, 16);

        var request = codec.ReadClientRequest(Bytes("GET / HTTP/1.1\r\nLong: 12345678901\r\n\r\n"));
        var response = codec.ReadServerResponse(Bytes("HTTP/1.1 200 ReasonTooLong\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(request.HasError, Is.True);
            Assert.That(request.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(request.Error.Identifier,
                Is.EqualTo("http-header-line-too-long"));
            Assert.That(request.Error.HasHttpStatusCode, Is.True);
            Assert.That(request.Error.HttpStatusCode, Is.EqualTo(431));
            Assert.That(request.Error.HasCloseCode, Is.False);
            Assert.That(response.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(response.HasError, Is.True);
            Assert.That(response.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(response.Error.Identifier,
                Is.EqualTo("http-header-line-too-long"));
            Assert.That(response.Error.HasHttpStatusCode, Is.True);
            Assert.That(response.Error.HttpStatusCode, Is.EqualTo(431));
            Assert.That(response.Error.HasCloseCode, Is.False);
        });
    }

    static WebSocketHttp1Codec CreateCodec()
    {
        return new WebSocketHttp1Codec(1024, 16, 256);
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
            Fields = new[] { Field("Host", "example.test") }
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
            Fields = new[] { Field("Upgrade", "websocket") }
        };
    }

    static WebSocketHttpResponse RejectionResponse()
    {
        return new WebSocketHttpResponse
        {
            Transport = WebSocketHandshakeTransport.Http1Upgrade,
            StatusCode = 403,
            HasReasonPhrase = true,
            ReasonPhrase = "Forbidden",
            Fields = new[] { Field("Content-Length", "4") },
            HasBody = true,
            Body = Encoding.ASCII.GetBytes("nope")
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

    static byte[] Bytes(string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }
}
