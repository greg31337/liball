using System.Text;

namespace http.Tests;

public class Http1SerializationNormalizationTests
{
    [Test]
    public void ExplicitChunkedRequestReplacesValidatedContentLength()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Content-Length", "6");
        request.Content = "abcdef"u8.ToArray();
        Http1SerializationOptions options = Framing(Http1BodyFraming.Chunked);
        options.ChunkSize = 3;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);
        string wire = Encoding.ASCII.GetString(result.Bytes);
        Http1ParseResult parsed = new Http1Parser().ParseHttp1Bytes(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Does.Not.Contain("Content-Length:"));
            Assert.That(CountText(wire, "Transfer-Encoding: chunked"), Is.EqualTo(1));
            Assert.That(wire, Does.EndWith(
                "3\r\nabc\r\n3\r\ndef\r\n0\r\n\r\n"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Request!.Content,
                Is.EqualTo("abcdef"u8.ToArray()));
            Assert.That(request.Headers.Fields.Select(item => item.Name),
                Does.Contain(HttpHeaderNames.ContentLength));
        });
    }

    [Test]
    public void ExplicitLengthRequestReplacesValidatedChunkedField()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Transfer-Encoding", "CHUNKED");
        AddField(request.Headers, "Trailer", "Digest");
        request.Content = "abc"u8.ToArray();
        Http1SerializationOptions options = Framing(Http1BodyFraming.ContentLength);

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Does.Contain("Content-Length: 3\r\n"));
            Assert.That(wire, Does.Not.Contain("Transfer-Encoding:"));
            Assert.That(wire, Does.Not.Contain("Trailer:"));
            Assert.That(wire, Does.EndWith("\r\n\r\nabc"));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ExplicitNoBodyRequestRemovesZeroLengthFraming()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Content-Length", "0");
        AddField(request.Headers, "Trailer", "Digest");
        Http1SerializationOptions options = Framing(Http1BodyFraming.None);

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "POST / HTTP/1.1\r\nHost: example.test\r\n\r\n"));
            Assert.That(result.FramingDecision!.HasNoBody, Is.True);
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ExplicitCloseDelimitedResponseRemovesLengthAndTrailerMetadata()
    {
        HttpResponse response = Response();
        AddField(response.Headers, "Content-Length", "3");
        AddField(response.Headers, "Trailer", "Digest");
        response.Content = "abc"u8.ToArray();
        Http1SerializationOptions options = Framing(Http1BodyFraming.CloseDelimited);

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Is.EqualTo(
                "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\nabc"));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(result.ConnectionDecision!.MustClose, Is.True);
            Assert.That(response.Headers.Fields, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ExplicitLengthResponseReplacesChunkedFraming()
    {
        HttpResponse response = Response();
        AddField(response.Headers, "Transfer-Encoding", "chunked");
        response.Content = "payload"u8.ToArray();
        Http1SerializationOptions options = Framing(Http1BodyFraming.ContentLength);

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);
        Http1ParseResult parsed = ParseResponse(result.Bytes);
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Does.Not.Contain("Transfer-Encoding:"));
            Assert.That(wire, Does.Contain("Content-Length: 7\r\n"));
            Assert.That(wire, Does.EndWith("\r\n\r\npayload"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Response!.Content,
                Is.EqualTo("payload"u8.ToArray()));
        });
    }

    [Test]
    public void ExplicitFramingCanonicalizesEquivalentDuplicateLengths()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Content-Length", "003, 3");
        AddField(request.Headers, "content-length", "3");
        request.Content = "abc"u8.ToArray();

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(
                request,
                Framing(Http1BodyFraming.ContentLength));
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(CountText(wire, "Content-Length:"), Is.EqualTo(1));
            Assert.That(wire, Does.Contain("Content-Length: 3\r\n"));
            Assert.That(wire, Does.Not.Contain("003"));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void AutomaticFramingPreservesValidatedDuplicateLengths()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Content-Length", "003, 3");
        AddField(request.Headers, "content-length", "3");
        request.Content = "abc"u8.ToArray();

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(
                request,
                new Http1SerializationOptions());
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Does.Contain("Content-Length: 003, 3\r\n"));
            Assert.That(wire, Does.Contain("content-length: 3\r\n"));
        });
    }

    [Test]
    public void HostileFramingIsRejectedBeforeExplicitNormalization()
    {
        HttpRequest conflict = Request();
        AddField(conflict.Headers, "Transfer-Encoding", "chunked");
        AddField(conflict.Headers, "Content-Length", "3");
        conflict.Content = "abc"u8.ToArray();
        HttpRequest divergent = Request();
        AddField(divergent.Headers, "Content-Length", "3, 4");
        divergent.Content = "abc"u8.ToArray();
        HttpRequest mismatch = Request();
        AddField(mismatch.Headers, "Content-Length", "4");
        mismatch.Content = "abc"u8.ToArray();
        HttpResponse unsupported = Response();
        AddField(unsupported.Headers, "Transfer-Encoding", "gzip, chunked");
        unsupported.Content = "abc"u8.ToArray();

        Http1SerializationResult conflictResult =
            new Http1Serializer().SerializeHttp1Request(
                conflict,
                Framing(Http1BodyFraming.Chunked));
        Http1SerializationResult divergentResult =
            new Http1Serializer().SerializeHttp1Request(
                divergent,
                Framing(Http1BodyFraming.ContentLength));
        Http1SerializationResult mismatchResult =
            new Http1Serializer().SerializeHttp1Request(
                mismatch,
                Framing(Http1BodyFraming.Chunked));
        Http1SerializationResult unsupportedResult =
            new Http1Serializer().SerializeHttp1Response(
                unsupported,
                Framing(Http1BodyFraming.ContentLength));

        Assert.Multiple(() =>
        {
            Assert.That(conflictResult.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-content-length-conflict"));
            Assert.That(divergentResult.ErrorCode,
                Is.EqualTo("mismatched-http1-content-length"));
            Assert.That(mismatchResult.ErrorCode,
                Is.EqualTo("mismatched-http1-content-length"));
            Assert.That(unsupportedResult.ErrorCode,
                Is.EqualTo("unsupported-http1-response-transfer-coding"));
            Assert.That(conflictResult.Bytes, Is.Empty);
            Assert.That(unsupportedResult.Bytes, Is.Empty);
        });
    }

    [TestCase("Content-Length")]
    [TestCase("Transfer-Encoding")]
    public void ConnectionCannotNominateAFramingField(string option)
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Connection", option);
        AddField(request.Headers, "Content-Length", "0");

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(
                request,
                Framing(Http1BodyFraming.ContentLength));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-connection-framing-option"));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void OriginalLimitsAreCheckedBeforeFieldsAreCollapsed()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Content-Length", "0");
        AddField(request.Headers, "content-length", "0");
        Http1SerializationOptions options = Framing(Http1BodyFraming.ContentLength);
        options.Limits.MaximumFieldCount = 2;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-field-count-limit-exceeded"));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void BoundedNormalizationMatchesAllocatingOutputWithoutPartialWrites()
    {
        HttpResponse response = Response();
        AddField(response.Headers, "Transfer-Encoding", "chunked");
        response.Content = "normalized"u8.ToArray();
        Http1SerializationOptions options = Framing(Http1BodyFraming.ContentLength);
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] insufficient = Enumerable.Repeat(
            (byte)0xa5,
            allocated.RequiredSize - 1).ToArray();
        byte[] exact = new byte[allocated.RequiredSize];

        Http1SerializationResult needOutput =
            serializer.WriteHttp1Response(response, options, insufficient);
        Http1SerializationResult written =
            serializer.WriteHttp1Response(response, options, exact);

        Assert.Multiple(() =>
        {
            Assert.That(needOutput.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.RequiredSize, Is.EqualTo(allocated.RequiredSize));
            Assert.That(needOutput.BytesWritten, Is.Zero);
            Assert.That(insufficient, Is.All.EqualTo(0xa5));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact, Is.EqualTo(allocated.Bytes));
        });
    }

    static HttpRequest Request()
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = HttpMethods.Post;
        request.Target = "/";
        AddField(request.Headers, "Host", "example.test");
        return request;
    }

    static HttpResponse Response()
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = HttpStatusCodes.Ok;
        return response;
    }

    static Http1SerializationOptions Framing(Http1BodyFraming framing)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = framing;
        return options;
    }

    static Http1ParseResult ParseResponse(byte[] message)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(message);
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = Encoding.ASCII.GetBytes(value);
        headers.Fields.Add(field);
    }

    static int CountText(string text, string value)
    {
        int count = 0;
        int position = 0;
        while (position <= text.Length - value.Length)
        {
            int found = text.IndexOf(value, position, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }

            count = count + 1;
            position = found + value.Length;
        }

        return count;
    }
}
