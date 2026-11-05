using System.Text;

namespace http.Tests;

public class Http2MessageEncodingTests
{
    [Test]
    public void RequestEncodingCreatesCanonicalOwnedAndHpackReadyFields()
    {
        HttpRequest request = Request();
        HttpHeaderField accept = Field("Accept", "application/json");
        HttpHeaderField cookie = Field("Cookie", "a=1; b=2");
        cookie.IsSensitive = true;
        cookie.NeverIndex = true;
        request.Headers = Headers(accept, cookie);
        request.Content = Encoding.ASCII.GetBytes("abc");

        Http2MessageResult result = new Http2MessageCodec()
            .EncodeHttp2RequestHeaders(3, request, false);
        HpackResult encoded = new HpackEncoder().EncodeHeaderBlock(
            result.Headers, new HpackOptions());
        HpackResult decoded = new HpackDecoder().DecodeHeaderBlock(
            encoded.Bytes, new HpackOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.StreamId, Is.EqualTo(3));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.EndStream, Is.False);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":authority", ":path",
                    "accept", "cookie", "cookie", "content-length"
                }));
            Assert.That(result.Headers.Fields.Select(Text),
                Is.EqualTo(new[]
                {
                    "GET", "https", "example.test", "/items?q=1",
                    "application/json", "a=1", "b=2", "3"
                }));
            Assert.That(result.Headers.Fields[5].IsSensitive, Is.True);
            Assert.That(result.Headers.Fields[5].NeverIndex, Is.True);
            Assert.That(result.Headers.Fields[6].IsSensitive, Is.True);
            Assert.That(result.Headers.Fields[6].NeverIndex, Is.True);
            Assert.That(encoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decoded.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(result.Headers.Fields.Select(item => item.Name)));
            Assert.That(decoded.Headers.Fields.Select(Text),
                Is.EqualTo(result.Headers.Fields.Select(Text)));
            Assert.That(decoded.Headers.Fields[5].NeverIndex, Is.True);
            Assert.That(decoded.Headers.Fields[6].NeverIndex, Is.True);
        });

        accept.Name = "changed";
        accept.Value[0] = (byte)'X';
        cookie.Value[0] = (byte)'Y';
        Assert.Multiple(() =>
        {
            Assert.That(result.Headers.Fields[4].Name, Is.EqualTo("accept"));
            Assert.That(Text(result.Headers.Fields[4]),
                Is.EqualTo("application/json"));
            Assert.That(Text(result.Headers.Fields[5]), Is.EqualTo("a=1"));
        });
    }

    [Test]
    public void RequestEncodingSelectsTheLegalPseudoFieldSetForEachForm()
    {
        HttpRequest hostRequest = Request();
        hostRequest.Authority = null;
        hostRequest.Headers = Headers(Field("Host", "example.test"));

        HttpRequest connect = Request();
        connect.Method = HttpMethods.Connect;
        connect.Scheme = null;
        connect.Authority = "example.test:443";
        connect.Target = "example.test:443";

        HttpRequest extended = Request();
        extended.Method = HttpMethods.Connect;
        extended.ConnectProtocol = "websocket";
        extended.Target = "/chat";

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult host = codec.EncodeHttp2RequestHeaders(
            1, hostRequest, true);
        Http2MessageResult standard = codec.EncodeHttp2RequestHeaders(
            3, connect, true);
        Http2MessageResult protocol = codec.EncodeHttp2RequestHeaders(
            5, extended, true);

        Assert.Multiple(() =>
        {
            Assert.That(host.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(host.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":scheme", ":path", "host"
                }));
            Assert.That(standard.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(standard.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { ":method", ":authority" }));
            Assert.That(protocol.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(protocol.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":method", ":protocol", ":scheme", ":authority",
                    ":path"
                }));
            Assert.That(protocol.IsTunnel, Is.True);
        });
    }

    [Test]
    public void RequestEncodingRejectsInvalidControlFieldsAndFraming()
    {
        HttpRequest missingAuthority = Request();
        missingAuthority.Authority = null;

        HttpRequest conflict = Request();
        conflict.Headers = Headers(Field("Host", "other.test"));

        HttpRequest pseudo = Request();
        pseudo.Headers = Headers(Field(":status", "200"));

        HttpRequest connection = Request();
        connection.Headers = Headers(Field("Connection", "close"));

        HttpRequest length = Request();
        length.Content = Encoding.ASCII.GetBytes("abc");
        length.Headers = Headers(Field("Content-Length", "2"));

        HttpRequest trace = Request();
        trace.Method = HttpMethods.Trace;
        trace.Content = new byte[] { 1 };

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult noAuthority = codec.EncodeHttp2RequestHeaders(
            1, missingAuthority, true);
        Http2MessageResult hostConflict = codec.EncodeHttp2RequestHeaders(
            3, conflict, true);
        Http2MessageResult suppliedPseudo = codec.EncodeHttp2RequestHeaders(
            5, pseudo, true);
        Http2MessageResult prohibited = codec.EncodeHttp2RequestHeaders(
            7, connection, true);
        Http2MessageResult mismatch = codec.EncodeHttp2RequestHeaders(
            9, length, false);
        Http2MessageResult premature = codec.EncodeHttp2RequestHeaders(
            11, length, true);
        Http2MessageResult forbiddenContent = codec.EncodeHttp2RequestHeaders(
            13, trace, false);

        Assert.Multiple(() =>
        {
            AssertRejected(
                noAuthority, "missing-http2-request-authority");
            AssertRejected(
                hostConflict, "conflicting-http2-authority");
            AssertRejected(
                suppliedPseudo, "invalid-http2-field-name");
            AssertRejected(
                prohibited, "http2-connection-specific-field");
            AssertRejected(
                mismatch, "http2-content-length-mismatch");
            AssertRejected(premature, "http2-premature-end-stream");
            AssertRejected(
                forbiddenContent, "http2-request-content-not-allowed");
        });
    }

    [Test]
    public void ResponseEncodingCreatesStatusFirstAndSuppliesContentLength()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        response.HasReasonPhrase = true;
        response.ReasonPhrase = Encoding.ASCII.GetBytes("Ignored");
        HttpHeaderField server = Field("Server", "example");
        server.IsSensitive = true;
        server.NeverIndex = true;
        response.Headers = Headers(server);
        response.Content = Encoding.ASCII.GetBytes("body");

        Http2MessageResult result = new Http2MessageCodec()
            .EncodeHttp2ResponseHeaders(7, response, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Response));
            Assert.That(result.Response, Is.SameAs(response));
            Assert.That(result.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    ":status", "server", "content-length"
                }));
            Assert.That(result.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "200", "example", "4" }));
            Assert.That(result.Headers.Fields[1].IsSensitive, Is.True);
            Assert.That(result.Headers.Fields[1].NeverIndex, Is.True);
        });

        server.Value[0] = (byte)'X';
        Assert.That(Text(result.Headers.Fields[1]), Is.EqualTo("example"));
    }

    [Test]
    public void InformationalResponseEncodingUsesItsDistinctCompletionRules()
    {
        HttpResponse hints = Response(HttpStatusCodes.EarlyHints);
        hints.IsInformational = true;
        hints.Headers = Headers(Field("Link", "</style.css>"));
        HttpResponse inconsistent = Response(HttpStatusCodes.EarlyHints);
        HttpResponse switching = Response(HttpStatusCodes.SwitchingProtocols);
        switching.IsInformational = true;
        Http2MessageCodec codec = new Http2MessageCodec();

        Http2MessageResult valid = codec.EncodeHttp2ResponseHeaders(
            1, hints, false);
        Http2MessageResult ended = codec.EncodeHttp2ResponseHeaders(
            3, hints, true);
        Http2MessageResult mismatch = codec.EncodeHttp2ResponseHeaders(
            5, inconsistent, false);
        Http2MessageResult upgrade = codec.EncodeHttp2ResponseHeaders(
            7, switching, false);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(valid.IsComplete, Is.True);
            Assert.That(valid.EndStream, Is.False);
            Assert.That(valid.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { ":status", "link" }));
            AssertRejected(ended, "http2-informational-end-stream");
            AssertRejected(
                mismatch, "inconsistent-http2-informational-status");
            AssertRejected(
                upgrade, "http2-switching-protocols-not-supported");
        });
    }

    [Test]
    public void ResponseEncodingEnforcesStatusAndContentLengthRules()
    {
        HttpResponse noContent = Response(HttpStatusCodes.NoContent);
        noContent.Headers = Headers(Field("Content-Length", "0"));

        HttpResponse notModified = Response(HttpStatusCodes.NotModified);
        notModified.Headers = Headers(Field("Content-Length", "123"));

        HttpResponse reset = Response(HttpStatusCodes.ResetContent);
        reset.Content = new byte[] { 1 };

        HttpResponse mismatch = Response(HttpStatusCodes.Ok);
        mismatch.Headers = Headers(Field("Content-Length", "2"));
        mismatch.Content = Encoding.ASCII.GetBytes("abc");

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult prohibitedLength =
            codec.EncodeHttp2ResponseHeaders(1, noContent, true);
        Http2MessageResult metadataLength =
            codec.EncodeHttp2ResponseHeaders(3, notModified, true);
        Http2MessageResult prohibitedContent =
            codec.EncodeHttp2ResponseHeaders(5, reset, false);
        Http2MessageResult wrongLength =
            codec.EncodeHttp2ResponseHeaders(7, mismatch, false);
        Http2MessageResult premature =
            codec.EncodeHttp2ResponseHeaders(9, mismatch, true);

        Assert.Multiple(() =>
        {
            AssertRejected(
                prohibitedLength, "http2-content-length-not-allowed");
            Assert.That(metadataLength.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(metadataLength.Headers.Fields[1]),
                Is.EqualTo("123"));
            AssertRejected(
                prohibitedContent, "http2-response-content-not-allowed");
            AssertRejected(wrongLength, "http2-content-length-mismatch");
            AssertRejected(premature, "http2-premature-end-stream");
        });
    }

    [Test]
    public void TrailerEncodingNormalizesCopiesAndValidatesFields()
    {
        HttpHeaderField checksum = Field("X-Checksum", "abc123");
        checksum.IsSensitive = true;
        checksum.NeverIndex = true;
        Http2MessageCodec codec = new Http2MessageCodec();

        Http2MessageResult valid = codec.EncodeHttp2Trailers(
            9, Headers(checksum));
        Http2MessageResult framing = codec.EncodeHttp2Trailers(
            11, Headers(Field("Content-Length", "0")));
        Http2MessageResult pseudo = codec.EncodeHttp2Trailers(
            13, Headers(Field(":status", "200")));
        Http2MessageResult connection = codec.EncodeHttp2Trailers(
            15, Headers(Field("Connection", "close")));

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.MessageKind,
                Is.EqualTo(HttpMessageKind.Trailers));
            Assert.That(valid.EndStream, Is.True);
            Assert.That(valid.IsComplete, Is.True);
            Assert.That(valid.Trailers, Is.SameAs(valid.Headers));
            Assert.That(valid.Headers.Fields[0].Name,
                Is.EqualTo("x-checksum"));
            Assert.That(valid.Headers.Fields[0].IsSensitive, Is.True);
            Assert.That(valid.Headers.Fields[0].NeverIndex, Is.True);
            AssertRejected(framing, "invalid-http2-trailer-field");
            AssertRejected(pseudo, "invalid-http2-field-name");
            AssertRejected(connection, "invalid-http2-trailer-field");
        });

        checksum.Value[0] = (byte)'X';
        Assert.That(Text(valid.Headers.Fields[0]), Is.EqualTo("abc123"));
    }

    [Test]
    public void EncodingAppliesHeaderListLimitsAfterAddingPseudoFields()
    {
        Http2MessageCodec countCodec = new Http2MessageCodec();
        countCodec.limits.MaximumFieldCount = 4;
        HttpRequest request = Request();
        request.Headers = Headers(Field("Accept", "text/plain"));

        Http2MessageResult count = countCodec.EncodeHttp2RequestHeaders(
            1, request, true);

        AssertRejected(count, "http2-header-count-limit-exceeded");
    }

    static void AssertRejected(
        Http2MessageResult result,
        string errorCodeName)
    {
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.Request, Is.Null);
        Assert.That(result.Response, Is.Null);
    }

    static HttpRequest Request()
    {
        HttpRequest request = new HttpRequest();
        request.Method = HttpMethods.Get;
        request.Scheme = "https";
        request.Authority = "example.test";
        request.Target = "/items?q=1";
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.StatusCode = statusCode;
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
