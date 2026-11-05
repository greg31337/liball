using System.Text;

namespace http.Tests;

public class Http1SerializerExhaustiveTests
{
    [TestCase("none")]
    [TestCase("content-length")]
    [TestCase("chunked")]
    [TestCase("chunked-trailers")]
    public void EveryRequestFramingMatchesAllocatingAndEverySpanSize(
        string selected)
    {
        HttpRequest request = Request(
            selected == "none" ? HttpMethods.Get : HttpMethods.Post,
            "/matrix");
        Http1SerializationOptions options = new Http1SerializationOptions();
        string expected;
        if (selected == "none")
        {
            options.HasBodyFraming = true;
            options.BodyFraming = Http1BodyFraming.None;
            expected =
                "GET /matrix HTTP/1.1\r\n" +
                "Host: example.test\r\n\r\n";
        }
        else if (selected == "content-length")
        {
            request.Content = "data"u8.ToArray();
            options.HasBodyFraming = true;
            options.BodyFraming = Http1BodyFraming.ContentLength;
            expected =
                "POST /matrix HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "Content-Length: 4\r\n\r\ndata";
        }
        else if (selected == "chunked")
        {
            request.Content = "data"u8.ToArray();
            options.HasBodyFraming = true;
            options.BodyFraming = Http1BodyFraming.Chunked;
            options.ChunkSize = 2;
            options.ChunkExtension = "part=one";
            expected =
                "POST /matrix HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "Transfer-Encoding: chunked\r\n\r\n" +
                "2;part=one\r\nda\r\n" +
                "2;part=one\r\nta\r\n" +
                "0;part=one\r\n\r\n";
        }
        else
        {
            request.Content = "data"u8.ToArray();
            AddField(request.Trailers, "Digest", "done");
            options.ChunkSize = 2;
            expected =
                "POST /matrix HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Trailer: Digest\r\n\r\n" +
                "2\r\nda\r\n2\r\nta\r\n0\r\n" +
                "Digest: done\r\n\r\n";
        }

        byte[] wire = VerifyRequestOutputs(request, options, expected);
        Http1ParseResult parsed = new Http1Parser().ParseHttp1Bytes(wire);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Request!.Content,
                Is.EqualTo(request.Content));
            Assert.That(parsed.Events[0].Request!.Trailers.Fields.Count,
                Is.EqualTo(request.Trailers.Fields.Count));
        });
    }

    [TestCase("content-length")]
    [TestCase("chunked")]
    [TestCase("chunked-trailers")]
    [TestCase("close-delimited")]
    public void EveryResponseBodyFramingMatchesAllocatingAndEverySpanSize(
        string selected)
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.Content = "data"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        string expected;
        if (selected == "content-length")
        {
            options.BodyFraming = Http1BodyFraming.ContentLength;
            expected =
                "HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\ndata";
        }
        else if (selected == "chunked")
        {
            options.BodyFraming = Http1BodyFraming.Chunked;
            options.ChunkSize = 3;
            options.ChunkExtension = ";part=two";
            expected =
                "HTTP/1.1 200 OK\r\n" +
                "Transfer-Encoding: chunked\r\n\r\n" +
                "3;part=two\r\ndat\r\n" +
                "1;part=two\r\na\r\n" +
                "0;part=two\r\n\r\n";
        }
        else if (selected == "chunked-trailers")
        {
            options.BodyFraming = Http1BodyFraming.Chunked;
            options.ChunkSize = 4;
            AddField(response.Trailers, "Digest", "done");
            expected =
                "HTTP/1.1 200 OK\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Trailer: Digest\r\n\r\n" +
                "4\r\ndata\r\n0\r\nDigest: done\r\n\r\n";
        }
        else
        {
            options.BodyFraming = Http1BodyFraming.CloseDelimited;
            expected =
                "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\ndata";
        }

        byte[] wire = VerifyResponseOutputs(response, options, expected);
        Http1Parser parser = ResponseParser(HttpMethods.Get);
        Http1ParseResult parsed = parser.ParseHttp1Bytes(wire);
        if (selected == "close-delimited")
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            parsed = parser.CompleteHttp1Input();
        }

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events.Last().Response!.Content,
                Is.EqualTo(response.Content));
            Assert.That(parsed.Events.Last().Response!.Trailers.Fields.Count,
                Is.EqualTo(response.Trailers.Fields.Count));
        });
    }

    [TestCase("informational")]
    [TestCase("head")]
    [TestCase("no-content")]
    [TestCase("reset-content")]
    [TestCase("not-modified")]
    [TestCase("connect")]
    [TestCase("upgrade")]
    public void EveryNoBodyAndProtocolTransitionCaseHasExactOutput(
        string selected)
    {
        HttpResponse response;
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1BodyFraming framing = Http1BodyFraming.None;
        Http1ConnectionMode connection = Http1ConnectionMode.Persistent;
        string requestMethod = HttpMethods.Get;
        string expected;
        if (selected == "informational")
        {
            response = Response(HttpStatusCodes.EarlyHints);
            expected = "HTTP/1.1 103 Early Hints\r\n\r\n";
        }
        else if (selected == "head")
        {
            response = Response(HttpStatusCodes.Ok);
            AddField(response.Headers, "Content-Length", "7");
            options.ResponseRequestMethod = HttpMethods.Head;
            requestMethod = HttpMethods.Head;
            expected =
                "HTTP/1.1 200 OK\r\nContent-Length: 7\r\n\r\n";
        }
        else if (selected == "no-content")
        {
            response = Response(HttpStatusCodes.NoContent);
            expected = "HTTP/1.1 204 No Content\r\n\r\n";
        }
        else if (selected == "reset-content")
        {
            response = Response(HttpStatusCodes.ResetContent);
            framing = Http1BodyFraming.ContentLength;
            expected =
                "HTTP/1.1 205 Reset Content\r\nContent-Length: 0\r\n\r\n";
        }
        else if (selected == "not-modified")
        {
            response = Response(HttpStatusCodes.NotModified);
            AddField(response.Headers, "Content-Length", "9");
            expected =
                "HTTP/1.1 304 Not Modified\r\nContent-Length: 9\r\n\r\n";
        }
        else if (selected == "connect")
        {
            response = Response(HttpStatusCodes.Ok);
            options.ResponseRequestMethod = HttpMethods.Connect;
            requestMethod = HttpMethods.Connect;
            framing = Http1BodyFraming.Tunnel;
            connection = Http1ConnectionMode.Tunneled;
            expected = "HTTP/1.1 200 OK\r\n\r\n";
        }
        else
        {
            response = Response(HttpStatusCodes.SwitchingProtocols);
            AddField(response.Headers, "Connection", "Upgrade");
            AddField(response.Headers, "Upgrade", "websocket");
            connection = Http1ConnectionMode.Upgraded;
            expected =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n\r\n";
        }

        byte[] wire = VerifyResponseOutputs(response, options, expected);
        Http1SerializationResult allocated = new Http1Serializer()
            .SerializeHttp1Response(response, options);
        Http1ParseResult parsed = ResponseParser(requestMethod)
            .ParseHttp1Bytes(wire);

        Assert.Multiple(() =>
        {
            Assert.That(allocated.FramingDecision!.BodyFraming,
                Is.EqualTo(framing));
            Assert.That(allocated.ConnectionDecision!.ConnectionMode,
                Is.EqualTo(connection));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Response!.Content, Is.Empty);
            Assert.That(parsed.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(framing));
            Assert.That(parsed.Events[0].ConnectionDecision!.ConnectionMode,
                Is.EqualTo(connection));
        });
    }

    [TestCase(Http1BodyFraming.CloseDelimited)]
    [TestCase(Http1BodyFraming.Tunnel)]
    [TestCase(Http1BodyFraming.Invalid)]
    public void UnsupportedRequestFramingRejectsBothOutputApis(
        Http1BodyFraming framing)
    {
        HttpRequest request = Request(HttpMethods.Post, "/");
        Http1SerializationOptions options = ExplicitFraming(framing);
        byte[] destination = Enumerable.Repeat((byte)0xa5, 128).ToArray();
        Http1Serializer serializer = new Http1Serializer();

        Http1SerializationResult allocated =
            serializer.SerializeHttp1Request(request, options);
        Http1SerializationResult bounded =
            serializer.WriteHttp1Request(request, options, destination);

        AssertRejectedPair(
            allocated,
            bounded,
            destination,
            "unsupported-http1-request-framing");
    }

    [TestCase(Http1BodyFraming.None)]
    [TestCase(Http1BodyFraming.Tunnel)]
    [TestCase(Http1BodyFraming.Invalid)]
    public void UnsupportedOrdinaryResponseFramingRejectsBothOutputApis(
        Http1BodyFraming framing)
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        Http1SerializationOptions options = ExplicitFraming(framing);
        byte[] destination = Enumerable.Repeat((byte)0xa5, 128).ToArray();
        Http1Serializer serializer = new Http1Serializer();

        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        Http1SerializationResult bounded =
            serializer.WriteHttp1Response(response, options, destination);

        AssertRejectedPair(
            allocated,
            bounded,
            destination,
            "unsupported-http1-response-framing");
    }

    static byte[] VerifyRequestOutputs(
        HttpRequest request,
        Http1SerializationOptions options,
        string expected)
    {
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Request(request, options);
        byte[] expectedBytes = Encoding.ASCII.GetBytes(expected);

        Assert.Multiple(() =>
        {
            Assert.That(allocated.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allocated.Bytes, Is.EqualTo(expectedBytes));
            Assert.That(allocated.BytesWritten, Is.EqualTo(expectedBytes.Length));
            Assert.That(allocated.RequiredSize, Is.EqualTo(expectedBytes.Length));
            Assert.That(allocated.Options, Is.SameAs(options));
        });

        VerifyEveryRequestSpan(serializer, request, options, allocated.Bytes);
        return allocated.Bytes;
    }

    static byte[] VerifyResponseOutputs(
        HttpResponse response,
        Http1SerializationOptions options,
        string expected)
    {
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] expectedBytes = Encoding.ASCII.GetBytes(expected);

        Assert.Multiple(() =>
        {
            Assert.That(allocated.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allocated.Bytes, Is.EqualTo(expectedBytes));
            Assert.That(allocated.BytesWritten, Is.EqualTo(expectedBytes.Length));
            Assert.That(allocated.RequiredSize, Is.EqualTo(expectedBytes.Length));
            Assert.That(allocated.Options, Is.SameAs(options));
        });

        VerifyEveryResponseSpan(serializer, response, options, allocated.Bytes);
        return allocated.Bytes;
    }

    static void VerifyEveryRequestSpan(
        Http1Serializer serializer,
        HttpRequest request,
        Http1SerializationOptions options,
        byte[] expected)
    {
        int size = 0;
        while (size < expected.Length)
        {
            byte[] destination = Enumerable.Repeat(
                (byte)0xa5,
                expected.Length + 2).ToArray();
            Http1SerializationResult result = serializer.WriteHttp1Request(
                request,
                options,
                destination.AsSpan(1, size));
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(result.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(destination, Is.All.EqualTo(0xa5));
            size = size + 1;
        }

        byte[] exact = Enumerable.Repeat((byte)0xa5, expected.Length + 4).ToArray();
        Http1SerializationResult written = serializer.WriteHttp1Request(
            request,
            options,
            exact.AsSpan(2, expected.Length));
        AssertSuccessfulBoundedWrite(written, exact, expected);
    }

    static void VerifyEveryResponseSpan(
        Http1Serializer serializer,
        HttpResponse response,
        Http1SerializationOptions options,
        byte[] expected)
    {
        int size = 0;
        while (size < expected.Length)
        {
            byte[] destination = Enumerable.Repeat(
                (byte)0xa5,
                expected.Length + 2).ToArray();
            Http1SerializationResult result = serializer.WriteHttp1Response(
                response,
                options,
                destination.AsSpan(1, size));
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(result.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(destination, Is.All.EqualTo(0xa5));
            size = size + 1;
        }

        byte[] exact = Enumerable.Repeat((byte)0xa5, expected.Length + 4).ToArray();
        Http1SerializationResult written = serializer.WriteHttp1Response(
            response,
            options,
            exact.AsSpan(2, expected.Length));
        AssertSuccessfulBoundedWrite(written, exact, expected);
    }

    static void AssertSuccessfulBoundedWrite(
        Http1SerializationResult result,
        byte[] destination,
        byte[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.BytesWritten, Is.EqualTo(expected.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(expected.Length));
            Assert.That(result.Bytes, Is.Empty);
            Assert.That(destination.Take(2), Is.All.EqualTo(0xa5));
            Assert.That(destination.Skip(2).Take(expected.Length),
                Is.EqualTo(expected));
            Assert.That(destination.Skip(expected.Length + 2),
                Is.All.EqualTo(0xa5));
        });
    }

    static void AssertRejectedPair(
        Http1SerializationResult allocated,
        Http1SerializationResult bounded,
        byte[] destination,
        string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(allocated.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(bounded.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(allocated.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(bounded.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(allocated.Bytes, Is.Empty);
            Assert.That(bounded.BytesWritten, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(0xa5));
        });
    }

    static Http1SerializationOptions ExplicitFraming(Http1BodyFraming framing)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = framing;
        return options;
    }

    static HttpRequest Request(string method, string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        AddField(request.Headers, "Host", "example.test");
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        response.IsInformational = statusCode >= 100 && statusCode < 200;
        return response;
    }

    static Http1Parser ResponseParser(string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }
}
