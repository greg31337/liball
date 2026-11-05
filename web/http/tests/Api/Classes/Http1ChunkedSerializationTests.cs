using System.Text;

namespace http.Tests;

public class Http1ChunkedSerializationTests
{
    [Test]
    public void RequestTrailersSelectChunkedFramingAndRoundTripInOrder()
    {
        HttpRequest request = Request();
        request.Target = "/items";
        AddField(request.Headers, "Host", "example.test");
        request.Content = "abcdef"u8.ToArray();
        AddField(request.Trailers, "Digest", "sha-256=:YWJj:");
        AddField(request.Trailers, "X-Trace", "first");
        AddField(request.Trailers, "x-trace", "second");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.ChunkSize = 3;
        options.ChunkExtension = "sig=abc;flag";

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);
        Http1ParseResult parsed = new Http1Parser().ParseHttp1Bytes(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Is.EqualTo(
                "POST /items HTTP/1.1\r\n" +
                "Host: example.test\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Trailer: Digest, X-Trace\r\n\r\n" +
                "3;sig=abc;flag\r\nabc\r\n" +
                "3;sig=abc;flag\r\ndef\r\n" +
                "0;sig=abc;flag\r\n" +
                "Digest: sha-256=:YWJj:\r\n" +
                "X-Trace: first\r\n" +
                "x-trace: second\r\n\r\n"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events[0].Request!.Content,
                Is.EqualTo("abcdef"u8.ToArray()));
            Assert.That(parsed.Events[0].Request!.Trailers.Fields.Select(
                item => item.Name), Is.EqualTo(new[] { "Digest", "X-Trace", "x-trace" }));
            Assert.That(request.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void EmptyResponseCanCarryTrailersAfterTheTerminalChunk()
    {
        HttpResponse response = Response();
        AddField(response.Headers, "Trailer", "Digest");
        AddField(response.Trailers, "Digest", "complete");
        AddField(response.Trailers, "X-Trace", "42");

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(
                response,
                new Http1SerializationOptions());
        Http1ParseResult parsed = ParseResponse(result.Bytes);
        string wire = Encoding.ASCII.GetString(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wire, Is.EqualTo(
                "HTTP/1.1 200 OK\r\n" +
                "Trailer: Digest\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Trailer: X-Trace\r\n\r\n" +
                "0\r\nDigest: complete\r\nX-Trace: 42\r\n\r\n"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            Assert.That(parsed.Events[0].Response!.Trailers.Fields.Select(
                item => item.Name), Is.EqualTo(new[] { "Digest", "X-Trace" }));
            Assert.That(response.Headers.Fields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TrailerDeclarationCanBeDisabledWithoutDroppingTrailers()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Trailers, "Digest", "final");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.DeclareTrailers = false;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Request(request, options);
        string wire = Encoding.ASCII.GetString(result.Bytes);
        string head = wire.Substring(0, wire.IndexOf("\r\n\r\n", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(head, Does.Not.Contain("Trailer:"));
            Assert.That(wire, Does.EndWith("0\r\nDigest: final\r\n\r\n"));
            Assert.That(ParseResponseOrRequest(result.Bytes).Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [TestCase("")]
    [TestCase(";")]
    [TestCase(";=value")]
    [TestCase(";name=")]
    [TestCase(";name=two words")]
    [TestCase(";name=\"unterminated")]
    [TestCase(";name=value\r\nInjected: yes")]
    public void MalformedChunkExtensionsAreRejectedBeforeOutput(string extension)
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Host", "example.test");
        request.Content = "content"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkExtension = extension;
        byte[] destination = Enumerable.Repeat((byte)0xa5, 256).ToArray();
        Http1Serializer serializer = new Http1Serializer();

        Http1SerializationResult allocated =
            serializer.SerializeHttp1Request(request, options);
        Http1SerializationResult bounded =
            serializer.WriteHttp1Request(request, options, destination);

        Assert.Multiple(() =>
        {
            Assert.That(allocated.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(allocated.ErrorCode,
                Is.EqualTo("invalid-http1-chunk-extension"));
            Assert.That(allocated.Bytes, Is.Empty);
            Assert.That(bounded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(bounded.BytesWritten, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(0xa5));
        });
    }

    [Test]
    public void QuotedChunkExtensionPreservesAllowedOctets()
    {
        HttpResponse response = Response();
        response.Content = "x"u8.ToArray();
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.HasBodyFraming = true;
        options.BodyFraming = Http1BodyFraming.Chunked;
        options.ChunkExtension = ";note=\"a,b\\\"c\"";

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.EndWith(
                "1;note=\"a,b\\\"c\"\r\nx\r\n" +
                "0;note=\"a,b\\\"c\"\r\n\r\n"));
            Assert.That(ParseResponse(result.Bytes).Status,
                Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void TrailersConflictWithNonChunkedAndNoBodyFraming()
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Trailers, "Digest", "value");
        Http1SerializationOptions requestOptions = new Http1SerializationOptions();
        requestOptions.HasBodyFraming = true;
        requestOptions.BodyFraming = Http1BodyFraming.ContentLength;
        HttpResponse head = Response();
        AddField(head.Trailers, "Digest", "value");
        Http1SerializationOptions headOptions = new Http1SerializationOptions();
        headOptions.ResponseRequestMethod = HttpMethods.Head;

        Http1SerializationResult requestResult =
            new Http1Serializer().SerializeHttp1Request(request, requestOptions);
        Http1SerializationResult headResult =
            new Http1Serializer().SerializeHttp1Response(head, headOptions);

        Assert.Multiple(() =>
        {
            Assert.That(requestResult.ErrorCode,
                Is.EqualTo("http1-trailers-require-chunked"));
            Assert.That(headResult.ErrorCode,
                Is.EqualTo("http1-trailers-require-chunked"));
            Assert.That(requestResult.Bytes, Is.Empty);
            Assert.That(headResult.Bytes, Is.Empty);
        });
    }

    [TestCase("Content-Length", "value", "forbidden-http1-trailer-field")]
    [TestCase("Bad Name", "value", "invalid-http1-trailer-field")]
    [TestCase("Digest", "bad\nvalue", "invalid-http1-trailer-field")]
    public void InvalidTrailerFieldsAreRejected(
        string name,
        string value,
        string errorCode)
    {
        HttpResponse response = Response();
        AddRawField(response.Trailers, name, Encoding.ASCII.GetBytes(value));

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(
                response,
                new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    [Test]
    public void ExistingTrailerDeclarationsAreValidated()
    {
        HttpRequest malformed = Request();
        AddField(malformed.Headers, "Host", "example.test");
        AddField(malformed.Headers, "Transfer-Encoding", "chunked");
        AddField(malformed.Headers, "Trailer", "bad:name");
        HttpRequest forbidden = Request();
        AddField(forbidden.Headers, "Host", "example.test");
        AddField(forbidden.Headers, "Transfer-Encoding", "chunked");
        AddField(forbidden.Headers, "Trailer", "Content-Length");

        Http1SerializationResult malformedResult = SerializeRequest(malformed);
        Http1SerializationResult forbiddenResult = SerializeRequest(forbidden);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.ErrorCode,
                Is.EqualTo("invalid-http1-trailer-declaration"));
            Assert.That(forbiddenResult.ErrorCode,
                Is.EqualTo("forbidden-http1-trailer-declaration"));
        });
    }

    [Test]
    public void TrailerResourceLimitsApplyBeforeSerialization()
    {
        HttpRequest count = RequestWithHostAndTrailers("A", "1", "B", "2");
        Http1SerializationOptions countOptions = new Http1SerializationOptions();
        countOptions.Limits.MaximumTrailerCount = 1;
        HttpRequest section = RequestWithHostAndTrailers("Long-Name", "12345", "B", "2");
        Http1SerializationOptions sectionOptions = new Http1SerializationOptions();
        sectionOptions.Limits.MaximumTrailerSectionSize = 15;
        HttpRequest declarations = RequestWithHostAndTrailers("One", "1", "Two", "2");
        Http1SerializationOptions declarationOptions = new Http1SerializationOptions();
        declarationOptions.Limits.MaximumListMemberCount = 1;

        Http1SerializationResult countResult =
            new Http1Serializer().SerializeHttp1Request(count, countOptions);
        Http1SerializationResult sectionResult =
            new Http1Serializer().SerializeHttp1Request(section, sectionOptions);
        Http1SerializationResult declarationResult =
            new Http1Serializer().SerializeHttp1Request(
                declarations,
                declarationOptions);

        Assert.Multiple(() =>
        {
            Assert.That(countResult.ErrorCode,
                Is.EqualTo("http1-trailer-count-limit-exceeded"));
            Assert.That(sectionResult.ErrorCode,
                Is.EqualTo("http1-trailer-section-limit-exceeded"));
            Assert.That(declarationResult.ErrorCode,
                Is.EqualTo("http1-trailer-declaration-limit-exceeded"));
        });
    }

    [Test]
    public void BoundedSizeIncludesExtensionsDeclarationsAndTrailers()
    {
        HttpResponse response = Response();
        response.Content = "abcdef"u8.ToArray();
        AddField(response.Trailers, "Digest", "done");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.ChunkSize = 2;
        options.ChunkExtension = ";part=yes";
        Http1Serializer serializer = new Http1Serializer();
        Http1SerializationResult allocated =
            serializer.SerializeHttp1Response(response, options);
        byte[] insufficient = Enumerable.Repeat(
            (byte)0xcc,
            allocated.RequiredSize - 1).ToArray();
        byte[] exact = new byte[allocated.RequiredSize];

        Http1SerializationResult needOutput =
            serializer.WriteHttp1Response(response, options, insufficient);
        Http1SerializationResult written =
            serializer.WriteHttp1Response(response, options, exact);

        Assert.Multiple(() =>
        {
            Assert.That(needOutput.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.RequiredSize, Is.EqualTo(allocated.Bytes.Length));
            Assert.That(needOutput.BytesWritten, Is.Zero);
            Assert.That(insufficient, Is.All.EqualTo(0xcc));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.BytesWritten, Is.EqualTo(allocated.Bytes.Length));
            Assert.That(exact, Is.EqualTo(allocated.Bytes));
        });
    }

    [Test]
    public void CanonicalCasingAppliesToSerializedTrailerFieldNames()
    {
        HttpResponse response = Response();
        AddField(response.Trailers, "x-miXED-name", "value");
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.PreserveFieldNameCasing = false;

        Http1SerializationResult result =
            new Http1Serializer().SerializeHttp1Response(response, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.Contain(
                "Trailer: X-Mixed-Name\r\n"));
            Assert.That(Encoding.ASCII.GetString(result.Bytes), Does.EndWith(
                "0\r\nX-Mixed-Name: value\r\n\r\n"));
            Assert.That(response.Trailers.Fields[0].Name, Is.EqualTo("x-miXED-name"));
        });
    }

    static Http1SerializationResult SerializeRequest(HttpRequest request)
    {
        return new Http1Serializer().SerializeHttp1Request(
            request,
            new Http1SerializationOptions());
    }

    static HttpRequest Request()
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = HttpMethods.Post;
        request.Target = "/";
        return request;
    }

    static HttpRequest RequestWithHostAndTrailers(
        string firstName,
        string firstValue,
        string secondName,
        string secondValue)
    {
        HttpRequest request = Request();
        AddField(request.Headers, "Host", "example.test");
        AddField(request.Trailers, firstName, firstValue);
        AddField(request.Trailers, secondName, secondValue);
        return request;
    }

    static HttpResponse Response()
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = HttpStatusCodes.Ok;
        return response;
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

    static Http1ParseResult ParseResponseOrRequest(byte[] message)
    {
        return new Http1Parser().ParseHttp1Bytes(message);
    }

    static void AddField(HttpHeaders headers, string name, string value)
    {
        AddRawField(headers, name, Encoding.ASCII.GetBytes(value));
    }

    static void AddRawField(HttpHeaders headers, string name, byte[] value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = value;
        headers.Fields.Add(field);
    }
}
