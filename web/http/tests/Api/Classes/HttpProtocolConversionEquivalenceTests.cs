using System.Text;

namespace http.Tests;

public class HttpProtocolConversionEquivalenceTests
{
    [Test]
    public void NormalAndHeadRequestsRemainEquivalentAfterHttp1RoundTrips()
    {
        HttpRequest post = Http1Request(HttpMethods.Post, "/items?q=blue", "example.test");
        post.Headers.Fields.Add(Field("Content-Type", "text/plain"));
        post.Headers.Fields.Add(Field("Content-Length", "5"));
        post.Content = Encoding.ASCII.GetBytes("hello");

        HttpRequest head = Http1Request(HttpMethods.Head, "/manual", "docs.test");
        head.Headers.Fields.Add(Field("Accept", "text/html"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult postHttp2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, post, null);
        Http2MessageResult postHttp1 = codec.ConvertHttp2ToHttp1(postHttp2);
        Http2MessageResult postAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, postHttp1.Request, null);
        Http2MessageResult headHttp2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, head, null);
        Http2MessageResult headHttp1 = codec.ConvertHttp2ToHttp1(headHttp2);
        Http2MessageResult headAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, headHttp1.Request, null);

        Assert.Multiple(() =>
        {
            AssertSuccessful(postHttp2, postHttp1, postAgain);
            Assert.That(postAgain.Request!.Method, Is.EqualTo(HttpMethods.Post));
            Assert.That(postAgain.Request.Target, Is.EqualTo("/items?q=blue"));
            Assert.That(postAgain.Request.Authority, Is.EqualTo("example.test"));
            Assert.That(postAgain.Content, Is.EqualTo(Encoding.ASCII.GetBytes("hello")));
            Assert.That(FieldValue(postAgain.Headers, "content-type"), Is.EqualTo("text/plain"));
            Assert.That(FieldValue(postAgain.Headers, "content-length"), Is.EqualTo("5"));
            Assert.That(SerializeRequest(postHttp1.Request!),
                Does.StartWith("POST /items?q=blue HTTP/1.1\r\nHost: example.test\r\n"));

            AssertSuccessful(headHttp2, headHttp1, headAgain);
            Assert.That(headAgain.Request!.Method, Is.EqualTo(HttpMethods.Head));
            Assert.That(headAgain.Request.Target, Is.EqualTo("/manual"));
            Assert.That(headAgain.Request.Authority, Is.EqualTo("docs.test"));
            Assert.That(FieldValue(headAgain.Headers, "accept"), Is.EqualTo("text/html"));
            Assert.That(SerializeRequest(headHttp1.Request!),
                Does.StartWith("HEAD /manual HTTP/1.1\r\nHost: docs.test\r\n"));
        });
    }

    [Test]
    public void Http2RequestRetainsAuthorityAndSemanticsAcrossHttp1()
    {
        HttpRequest source = Http2Request(HttpMethods.Get, "/search?tag=h2", "api.example:8443");
        source.Headers.Fields.Add(Field("accept", "application/json"));
        Http2MessageResult original = Message(HttpMessageKind.Request);
        original.StreamId = 7;
        original.Request = source;
        original.IsComplete = true;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult http1 = codec.ConvertHttp2ToHttp1(original);
        Http2MessageResult restored = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, http1.Request, null);

        Assert.Multiple(() =>
        {
            Assert.That(http1.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(restored.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(http1.StreamId, Is.EqualTo(7));
            Assert.That(http1.Request!.TargetForm, Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(FieldValue(http1.Headers, "Host"), Is.EqualTo("api.example:8443"));
            Assert.That(restored.Request!.Scheme, Is.EqualTo("https"));
            Assert.That(restored.Request.Authority, Is.EqualTo("api.example:8443"));
            Assert.That(restored.Request.Target, Is.EqualTo("/search?tag=h2"));
            Assert.That(restored.Request.Path, Is.EqualTo("/search"));
            Assert.That(restored.Request.HasQuery, Is.True);
            Assert.That(restored.Request.Query, Is.EqualTo("tag=h2"));
            Assert.That(FieldValue(restored.Headers, "accept"), Is.EqualTo("application/json"));
        });
    }

    [Test]
    public void ChunkedTrailersSurviveBothConversionDirections()
    {
        HttpRequest upload = Http1Request(HttpMethods.Post, "/upload", "files.test");
        upload.Headers.Fields.Add(Field("Transfer-Encoding", "chunked"));
        upload.Headers.Fields.Add(Field("Trailer", "Digest, X-Trace"));
        upload.Content = Encoding.ASCII.GetBytes("abcdef");
        upload.Trailers = Headers(
            Field("Digest", "sha-256=:YWJjZA==:"),
            Field("X-Trace", "finished"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult http2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, upload, null);
        Http2MessageResult http1 = codec.ConvertHttp2ToHttp1(http2);
        Http2MessageResult restored = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, http1.Request, null);

        Assert.Multiple(() =>
        {
            AssertSuccessful(http2, http1, restored);
            Assert.That(http1.Request!.Content, Is.EqualTo(upload.Content));
            Assert.That(FieldValue(http1.Headers, "Transfer-Encoding"), Is.EqualTo("chunked"));
            Assert.That(FieldValue(http1.Headers, "Trailer"), Is.EqualTo("digest, x-trace"));
            Assert.That(HeaderNames(restored.Trailers!), Is.EqualTo(new[] { "digest", "x-trace" }));
            Assert.That(HeaderValues(restored.Trailers!),
                Is.EqualTo(new[] { "sha-256=:YWJjZA==:", "finished" }));
            Assert.That(SerializeRequest(http1.Request!),
                Does.EndWith(
                    "0\r\ndigest: sha-256=:YWJjZA==:\r\n" +
                    "x-trace: finished\r\n\r\n"));
        });
    }

    [Test]
    public void Http2TrailersSelectHttp1ChunkingAndReturnUnchanged()
    {
        HttpResponse source = Http2Response(HttpStatusCodes.Ok);
        source.Headers.Fields.Add(Field("content-type", "text/plain"));
        source.Content = Encoding.ASCII.GetBytes("streamed");
        source.Trailers = Headers(Field("digest", "complete"));
        Http2MessageResult original = Message(HttpMessageKind.Response);
        original.StreamId = 9;
        original.Response = source;
        original.IsComplete = true;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult http1 = codec.ConvertHttp2ToHttp1(original);
        Http2MessageResult restored = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, http1.Response);

        Assert.Multiple(() =>
        {
            Assert.That(http1.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(restored.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(FieldValue(http1.Headers, "Transfer-Encoding"), Is.EqualTo("chunked"));
            Assert.That(restored.Response!.StatusCode, Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(restored.Content, Is.EqualTo(Encoding.ASCII.GetBytes("streamed")));
            Assert.That(HeaderNames(restored.Trailers!), Is.EqualTo(new[] { "digest" }));
            Assert.That(HeaderValues(restored.Trailers!), Is.EqualTo(new[] { "complete" }));
            Assert.That(SerializeResponse(http1.Response!, HttpMethods.Get),
                Does.EndWith("0\r\ndigest: complete\r\n\r\n"));
        });
    }

    [Test]
    public void ConnectAndAbsoluteAuthorityRemainSemanticallyEquivalent()
    {
        HttpRequest connect = Http1Request(
            HttpMethods.Connect, "origin.example:443", "origin.example:443");
        connect.Scheme = null;

        HttpRequest absolute = Http1Request(
            HttpMethods.Get,
            "http://EXAMPLE.test:80/catalog?q=1",
            "example.test");
        absolute.Scheme = null;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult connectHttp2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, connect, null);
        Http2MessageResult connectHttp1 = codec.ConvertHttp2ToHttp1(connectHttp2);
        Http2MessageResult connectAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, connectHttp1.Request, null);
        Http2MessageResult absoluteHttp2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, absolute, null);
        Http2MessageResult absoluteHttp1 = codec.ConvertHttp2ToHttp1(absoluteHttp2);
        Http2MessageResult absoluteAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, absoluteHttp1.Request, null);

        Assert.Multiple(() =>
        {
            AssertSuccessful(connectHttp2, connectHttp1, connectAgain);
            Assert.That(connectAgain.Request!.Method, Is.EqualTo(HttpMethods.Connect));
            Assert.That(connectAgain.Request.Scheme, Is.Null);
            Assert.That(connectAgain.Request.Authority, Is.EqualTo("origin.example:443"));
            Assert.That(connectAgain.Request.TargetForm, Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(connectAgain.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { ":method", ":authority" }));
            Assert.That(SerializeRequest(connectHttp1.Request!),
                Does.StartWith("CONNECT origin.example:443 HTTP/1.1\r\n"));

            AssertSuccessful(absoluteHttp2, absoluteHttp1, absoluteAgain);
            Assert.That(absoluteHttp2.Request!.Scheme, Is.EqualTo("http"));
            Assert.That(absoluteHttp2.Request.Authority, Is.EqualTo("EXAMPLE.test:80"));
            Assert.That(absoluteHttp1.Request!.Target, Is.EqualTo("/catalog?q=1"));
            Assert.That(absoluteAgain.Request!.Scheme, Is.EqualTo("http"));
            Assert.That(absoluteAgain.Request.Authority, Is.EqualTo("EXAMPLE.test:80"));
            Assert.That(absoluteAgain.Request.Target, Is.EqualTo("/catalog?q=1"));
        });
    }

    [Test]
    public void InformationalAndNoContentResponsesRemainBodyless()
    {
        HttpResponse earlyHints = Http1Response(HttpStatusCodes.EarlyHints);
        earlyHints.IsInformational = true;
        earlyHints.HasReasonPhrase = true;
        earlyHints.ReasonPhrase = Encoding.ASCII.GetBytes("Early Hints");
        earlyHints.Headers.Fields.Add(Field("Link", "</style.css>; rel=preload"));

        HttpResponse noContent = Http2Response(HttpStatusCodes.NoContent);
        noContent.Headers.Fields.Add(Field("etag", "\"v1\""));
        Http2MessageResult noContentMessage = Message(HttpMessageKind.Response);
        noContentMessage.Response = noContent;
        noContentMessage.IsComplete = true;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult hintsHttp2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.InformationalResponse, null, earlyHints);
        Http2MessageResult hintsHttp1 = codec.ConvertHttp2ToHttp1(hintsHttp2);
        Http2MessageResult hintsAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.InformationalResponse, null, hintsHttp1.Response);
        Http2MessageResult noContentHttp1 = codec.ConvertHttp2ToHttp1(noContentMessage);
        Http2MessageResult noContentAgain = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Response, null, noContentHttp1.Response);

        Assert.Multiple(() =>
        {
            AssertSuccessful(hintsHttp2, hintsHttp1, hintsAgain);
            Assert.That(hintsAgain.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(hintsAgain.Response!.IsInformational, Is.True);
            Assert.That(hintsAgain.EndStream, Is.False);
            Assert.That(hintsAgain.Content, Is.Empty);
            Assert.That(FieldValue(hintsAgain.Headers, "link"),
                Is.EqualTo("</style.css>; rel=preload"));
            Assert.That(SerializeResponse(hintsHttp1.Response!, HttpMethods.Get),
                Does.StartWith("HTTP/1.1 103 Early Hints\r\n"));

            Assert.That(noContentHttp1.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(noContentAgain.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(noContentAgain.Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NoContent));
            Assert.That(noContentAgain.Content, Is.Empty);
            Assert.That(noContentHttp1.Response!.Headers.Fields.Any(
                item => item.Name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)),
                Is.False);
            Assert.That(FieldValue(noContentAgain.Headers, "etag"), Is.EqualTo("\"v1\""));
        });
    }

    [Test]
    public void HopByHopFieldsAreRemovedAndDoNotReappear()
    {
        HttpRequest source = Http1Request(HttpMethods.Get, "/", "example.test");
        source.Headers.Fields.Add(Field("Connection", "keep-alive, X-Private"));
        source.Headers.Fields.Add(Field("X-Private", "secret"));
        source.Headers.Fields.Add(Field("Keep-Alive", "timeout=5"));
        source.Headers.Fields.Add(Field("Proxy-Connection", "keep-alive"));
        source.Headers.Fields.Add(Field("Upgrade", "websocket"));
        source.Headers.Fields.Add(Field("TE", "gzip, trailers"));
        source.Headers.Fields.Add(Field("X-End-To-End", "preserved"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult http2 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, source, null);
        Http2MessageResult http1 = codec.ConvertHttp2ToHttp1(http2);
        Http2MessageResult restored = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, http1.Request, null);

        Assert.Multiple(() =>
        {
            AssertSuccessful(http2, http1, restored);
            Assert.That(HeaderNames(http2.Headers),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path",
                    "te", "x-end-to-end"
                }));
            Assert.That(FieldValue(http2.Headers, "te"), Is.EqualTo("trailers"));
            Assert.That(FieldValue(http1.Headers, "Connection"), Is.EqualTo("TE"));
            Assert.That(FieldValue(restored.Headers, "x-end-to-end"), Is.EqualTo("preserved"));
            Assert.That(HeaderNames(restored.Headers),
                Does.Not.Contain("x-private"));
            Assert.That(HeaderNames(restored.Headers), Does.Not.Contain("keep-alive"));
            Assert.That(HeaderNames(restored.Headers), Does.Not.Contain("upgrade"));
        });
    }

    [Test]
    public void MalformedInputsAreRejectedWithoutProducingOppositeModels()
    {
        HttpRequest badHttp1 = Http1Request(HttpMethods.Get, "/", "example.test");
        badHttp1.Headers.Fields.Add(Field("Connection", "bad option"));

        HttpRequest badHttp2 = Http2Request(HttpMethods.Get, "/", "example.test");
        badHttp2.Headers.Fields.Add(Field("connection", "close"));
        Http2MessageResult badHttp2Message = Message(HttpMessageKind.Request);
        badHttp2Message.Request = badHttp2;

        HttpHeaders malformedSection = Headers(
            Field(":method", "GET"),
            Field("x-before-path", "value"),
            Field(":scheme", "https"),
            Field(":authority", "example.test"),
            Field(":path", "/"));

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult rejectedHttp1 = codec.ConvertHttp1ToHttp2(
            HttpMessageKind.Request, badHttp1, null);
        Http2MessageResult rejectedHttp2 = codec.ConvertHttp2ToHttp1(badHttp2Message);
        Http2MessageResult malformed = codec.DecodeHttp2RequestHeaders(
            1, malformedSection, true, false);
        Http2MessageResult rejectedMalformed = codec.ConvertHttp2ToHttp1(malformed);

        Assert.Multiple(() =>
        {
            AssertRejected(rejectedHttp1, "invalid-http1-connection-field");
            AssertRejected(rejectedHttp2, "http2-connection-specific-field");
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.IsMalformed, Is.True);
            AssertRejected(rejectedMalformed, "invalid-http2-conversion-input");
        });
    }

    static void AssertSuccessful(params Http2MessageResult[] results)
    {
        Assert.That(results.Select(item => item.Status),
            Is.All.EqualTo(HttpOperationStatus.Done));
    }

    static void AssertRejected(Http2MessageResult result, string errorCode)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCode));
        Assert.That(result.Request, Is.Null);
        Assert.That(result.Response, Is.Null);
    }

    static HttpRequest Http1Request(string method, string target, string host)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http11;
        request.Method = method;
        request.Target = target;
        request.Scheme = "https";
        request.Headers.Fields.Add(Field("Host", host));
        request.IsComplete = true;
        return request;
    }

    static HttpRequest Http2Request(string method, string target, string authority)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = method;
        request.Target = target;
        request.TargetForm = target == "*" ?
            HttpRequestTargetForm.Asterisk : HttpRequestTargetForm.Origin;
        request.Scheme = "https";
        request.Authority = authority;
        request.IsComplete = true;
        return request;
    }

    static HttpResponse Http1Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http11;
        response.StatusCode = statusCode;
        response.IsComplete = true;
        return response;
    }

    static HttpResponse Http2Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http2;
        response.StatusCode = statusCode;
        response.IsComplete = true;
        return response;
    }

    static Http2MessageResult Message(HttpMessageKind kind)
    {
        Http2MessageResult message = new Http2MessageResult();
        message.Status = HttpOperationStatus.Done;
        message.MessageKind = kind;
        return message;
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

    static string FieldValue(HttpHeaders headers, string name)
    {
        return Encoding.ASCII.GetString(headers.Fields.Single(
            item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value);
    }

    static string[] HeaderNames(HttpHeaders headers)
    {
        return headers.Fields.Select(item => item.Name).ToArray();
    }

    static string[] HeaderValues(HttpHeaders headers)
    {
        return headers.Fields.Select(
            item => Encoding.ASCII.GetString(item.Value)).ToArray();
    }

    static string SerializeRequest(HttpRequest request)
    {
        Http1SerializationResult result = new Http1Serializer()
            .SerializeHttp1Request(request, new Http1SerializationOptions());
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return Encoding.ASCII.GetString(result.Bytes);
    }

    static string SerializeResponse(HttpResponse response, string requestMethod)
    {
        Http1SerializationOptions options = new Http1SerializationOptions();
        options.ResponseRequestMethod = requestMethod;
        Http1SerializationResult result = new Http1Serializer()
            .SerializeHttp1Response(response, options);
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return Encoding.ASCII.GetString(result.Bytes);
    }
}
