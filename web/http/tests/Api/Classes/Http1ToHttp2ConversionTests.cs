using System.Text;

namespace http.Tests;

public class Http1ToHttp2ConversionTests
{
    [Test]
    public void ChunkedRequestConversionRemovesHopFieldsAndPreservesTrailers()
    {
        HttpRequest request = Request(HttpMethods.Post, "/submit?q=1", "example.test");
        HttpHeaderField te = Field("TE", "gzip;q=0.5, trailers");
        te.IsSensitive = true;
        te.NeverIndex = true;
        HttpHeaderField retained = Field("X-End-To-End", "retained");
        retained.IsSensitive = true;
        request.Headers = Headers(
            Field("Host", "example.test"),
            Field("Connection", "keep-alive, X-Hop"),
            Field("X-Hop", "private"),
            Field("Keep-Alive", "timeout=5"),
            Field("Proxy-Connection", "keep-alive"),
            Field("Transfer-Encoding", "chunked"),
            Field("Upgrade", "websocket"),
            te,
            Field("Trailer", "Digest, X-Hop"),
            retained);
        request.Content = Encoding.ASCII.GetBytes("body");
        request.Trailers = Headers(
            Field("Digest", "complete"),
            Field("X-Hop", "private-trailer"));
        request.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp1ToHttp2(HttpMessageKind.Request, request, null);
        HttpRequest converted = result.Request!;
        HttpHeaders convertedTrailers = result.Trailers!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(result.EndStream, Is.False);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(result.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path",
                    "te", "trailer", "x-end-to-end", "content-length"
                }));
            Assert.That(result.Headers.Fields.Select(Text),
                Is.EqualTo(new[]
                {
                    "POST", "https", "example.test", "/submit?q=1",
                    "trailers", "Digest, X-Hop", "retained", "4"
                }));
            Assert.That(result.Headers.Fields[4].IsSensitive, Is.True);
            Assert.That(result.Headers.Fields[4].NeverIndex, Is.True);
            Assert.That(result.Headers.Fields[6].IsSensitive, Is.True);
            Assert.That(converted.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(converted.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(converted.Path, Is.EqualTo("/submit"));
            Assert.That(converted.HasQuery, Is.True);
            Assert.That(converted.Query, Is.EqualTo("q=1"));
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "te", "trailer", "x-end-to-end", "content-length"
                }));
            Assert.That(convertedTrailers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "digest" }));
            Assert.That(Text(convertedTrailers.Fields[0]),
                Is.EqualTo("complete"));
        });

        request.Content[0] = (byte)'X';
        retained.Value[0] = (byte)'Y';
        request.Trailers.Fields[0].Value[0] = (byte)'Z';
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(result.Content),
                Is.EqualTo("body"));
            Assert.That(Text(result.Headers.Fields[6]),
                Is.EqualTo("retained"));
            Assert.That(Text(convertedTrailers.Fields[0]),
                Is.EqualTo("complete"));
        });
    }

    [Test]
    public void RequestTargetsMapToTheirHttp2ControlFields()
    {
        HttpRequest absolute = Request(
            HttpMethods.Get,
            "http://EXAMPLE.test:80/path?name=value",
            "example.test");
        absolute.Scheme = null;

        HttpRequest root = Request(
            HttpMethods.Get,
            "http://example.test",
            "example.test");
        root.Scheme = null;

        HttpRequest connect = Request(
            HttpMethods.Connect,
            "example.test:443",
            "example.test:443");
        connect.Scheme = null;

        HttpRequest asterisk = Request(
            HttpMethods.Options,
            "*",
            "example.test");

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult absoluteResult = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, absolute, null);
        Http2MessageResult rootResult = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, root, null);
        Http2MessageResult connectResult = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, connect, null);
        Http2MessageResult asteriskResult = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, asterisk, null);

        Assert.Multiple(() =>
        {
            Assert.That(absoluteResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(absoluteResult.Headers.Fields.Select(Text),
                Is.EqualTo(new[]
                {
                    "GET", "http", "EXAMPLE.test:80", "/path?name=value"
                }));
            Assert.That(absoluteResult.Request!.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(absoluteResult.Request.Target,
                Is.EqualTo("/path?name=value"));
            Assert.That(rootResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(rootResult.Headers.Fields[3]), Is.EqualTo("/"));
            Assert.That(connectResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(connectResult.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { ":method", ":authority" }));
            Assert.That(connectResult.Request!.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(asteriskResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(asteriskResult.Headers.Fields[3]),
                Is.EqualTo("*"));
            Assert.That(asteriskResult.Request!.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
        });
    }

    [Test]
    public void RequestConversionRejectsMissingOrConflictingControlData()
    {
        HttpRequest missingHost = Request(HttpMethods.Get, "/", "example.test");
        missingHost.Headers = new HttpHeaders();

        HttpRequest conflict = Request(
            HttpMethods.Get, "http://one.test/", "two.test");
        conflict.Scheme = null;

        HttpRequest noScheme = Request(HttpMethods.Get, "/", "example.test");
        noScheme.Scheme = null;

        HttpRequest wrongVersion = Request(HttpMethods.Get, "/", "example.test");
        wrongVersion.ProtocolVersion = HttpProtocolVersion.Http2;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult noHost = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, missingHost, null);
        Http2MessageResult authorityConflict = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, conflict, null);
        Http2MessageResult scheme = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, noScheme, null);
        Http2MessageResult version = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, wrongVersion, null);

        Assert.Multiple(() =>
        {
            AssertRejected(noHost, "missing-http1-host-field");
            AssertRejected(
                authorityConflict, "conflicting-http1-request-authority");
            AssertRejected(scheme, "invalid-http2-request-control-data");
            AssertRejected(
                version, "invalid-http1-request-conversion-input");
        });
    }

    [Test]
    public void RequestConversionValidatesHttp1FramingBeforeRemovingIt()
    {
        HttpRequest unframed = Request(HttpMethods.Post, "/", "example.test");
        unframed.Content = Encoding.ASCII.GetBytes("abc");

        HttpRequest mismatch = Request(HttpMethods.Post, "/", "example.test");
        mismatch.Headers.Fields.Add(Field("Content-Length", "2"));
        mismatch.Content = Encoding.ASCII.GetBytes("abc");

        HttpRequest ambiguous = Request(HttpMethods.Post, "/", "example.test");
        ambiguous.Headers.Fields.Add(Field("Transfer-Encoding", "chunked"));
        ambiguous.Headers.Fields.Add(Field("Content-Length", "3"));
        ambiguous.Content = Encoding.ASCII.GetBytes("abc");

        HttpRequest trailers = Request(HttpMethods.Post, "/", "example.test");
        trailers.Trailers = Headers(Field("Digest", "complete"));

        HttpRequest nominated = Request(HttpMethods.Post, "/", "example.test");
        nominated.Headers.Fields.Add(Field("Connection", "Content-Length"));
        nominated.Headers.Fields.Add(Field("Content-Length", "0"));

        HttpRequest declaration = Request(HttpMethods.Post, "/", "example.test");
        declaration.Headers.Fields.Add(Field("Transfer-Encoding", "chunked"));
        declaration.Headers.Fields.Add(Field("Trailer", "bad:name"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult noFrame = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, unframed, null);
        Http2MessageResult wrongLength = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, mismatch, null);
        Http2MessageResult conflict = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, ambiguous, null);
        Http2MessageResult wrongTrailers = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, trailers, null);
        Http2MessageResult unsafeOption = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, nominated, null);
        Http2MessageResult badDeclaration = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, declaration, null);

        Assert.Multiple(() =>
        {
            AssertRejected(noFrame, "http1-content-length-mismatch");
            AssertRejected(wrongLength, "http1-content-length-mismatch");
            AssertRejected(
                conflict,
                "http1-transfer-encoding-content-length-conflict");
            AssertRejected(
                wrongTrailers, "http1-trailers-require-chunked-framing");
            AssertRejected(
                unsafeOption, "http1-connection-framing-option");
            AssertRejected(
                badDeclaration, "invalid-http1-trailer-declaration");
        });
    }

    [Test]
    public void ChunkedResponseConversionDropsReasonAndConnectionFraming()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = Encoding.ASCII.GetBytes("Everything Fine");
        HttpHeaderField server = Field("Server", "example");
        server.NeverIndex = true;
        response.Headers = Headers(
            Field("Connection", "X-Hop"),
            Field("X-Hop", "private"),
            Field("Transfer-Encoding", "chunked"),
            Field("TE", "trailers"),
            server);
        response.Content = Encoding.ASCII.GetBytes("body");
        response.Trailers = Headers(Field("X-Checksum", "abc123"));
        response.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp1ToHttp2(HttpMessageKind.Response, null, response);
        HttpResponse converted = result.Response!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":status", "server", "content-length"
                }));
            Assert.That(result.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "200", "example", "4" }));
            Assert.That(result.Headers.Fields[1].NeverIndex, Is.True);
            Assert.That(converted.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(converted.HasReasonPhrase, Is.False);
            Assert.That(converted.ReasonPhrase, Is.Empty);
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "server", "content-length" }));
            Assert.That(result.Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(result.Trailers!.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "x-checksum" }));
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void CloseDelimitedAndMetadataResponsesMapWithoutHttp1Framing()
    {
        HttpResponse closeDelimited = Response(HttpStatusCodes.Ok);
        closeDelimited.Content = Encoding.ASCII.GetBytes("abc");
        closeDelimited.IsComplete = true;

        HttpResponse hints = Response(HttpStatusCodes.EarlyHints);
        hints.IsInformational = true;
        hints.IsComplete = true;

        HttpResponse notModified = Response(HttpStatusCodes.NotModified);
        notModified.Headers = Headers(Field("Content-Length", "123"));
        notModified.IsComplete = true;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult closed = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, closeDelimited);
        Http2MessageResult informational = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.InformationalResponse, null, hints);
        Http2MessageResult metadata = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, notModified);

        Assert.Multiple(() =>
        {
            Assert.That(closed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(closed.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "200", "3" }));
            Assert.That(informational.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(informational.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(informational.EndStream, Is.False);
            Assert.That(informational.IsComplete, Is.True);
            Assert.That(metadata.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(metadata.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "304", "123" }));
        });
    }

    [Test]
    public void ResponseConversionRejectsIllegalOrUntranslatableFraming()
    {
        HttpResponse noContent = Response(HttpStatusCodes.NoContent);
        noContent.Headers = Headers(Field("Content-Length", "0"));

        HttpResponse unsupported = Response(HttpStatusCodes.Ok);
        unsupported.Headers = Headers(
            Field("Transfer-Encoding", "gzip, chunked"));
        unsupported.Content = Encoding.ASCII.GetBytes("coded");

        HttpResponse trailers = Response(HttpStatusCodes.Ok);
        trailers.Headers = Headers(Field("Content-Length", "0"));
        trailers.Trailers = Headers(Field("Digest", "complete"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult forbidden = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, noContent);
        Http2MessageResult coding = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, unsupported);
        Http2MessageResult wrongTrailers = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, trailers);

        Assert.Multiple(() =>
        {
            AssertRejected(
                forbidden, "http1-response-framing-field-not-allowed");
            AssertRejected(
                coding, "unsupported-http1-transfer-coding-conversion");
            AssertRejected(
                wrongTrailers, "http1-trailers-require-chunked-framing");
        });
    }

    [Test]
    public void ConversionRequiresOneModelMatchingTheMessageKind()
    {
        HttpRequest request = Request(HttpMethods.Get, "/", "example.test");
        HttpResponse response = Response(HttpStatusCodes.Ok);
        Http2MessageCodec codec = new Http2MessageCodec();

        Http2MessageResult missing = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, null, null);
        Http2MessageResult ambiguous = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, request, response);
        Http2MessageResult wrongInfo = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.InformationalResponse, null, response);
        Http2MessageResult trailers = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Trailers, null, null);

        Assert.Multiple(() =>
        {
            AssertRejected(
                missing, "invalid-http1-request-conversion-input");
            AssertRejected(
                ambiguous, "invalid-http1-request-conversion-input");
            AssertRejected(
                wrongInfo, "inconsistent-http1-response-conversion-kind");
            AssertRejected(trailers, "invalid-http1-conversion-kind");
        });
    }

    static void AssertRejected(
        Http2MessageResult result,
        string errorCodeName)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
        Assert.That(result.Request, Is.Null);
        Assert.That(result.Response, Is.Null);
    }

    static HttpRequest Request(string method, string target, string host)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        request.Scheme = "https";
        request.Headers = Headers(Field("Host", host));
        request.IsComplete = true;
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        response.IsComplete = true;
        return response;
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

    static string Text(HttpHeaderField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }
}
