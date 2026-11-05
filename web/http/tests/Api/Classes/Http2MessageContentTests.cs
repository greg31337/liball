using System.Text;

namespace http.Tests;

public class Http2MessageContentTests
{
    [Test]
    public void RequestDataAccumulatesAndCompletesAtTheDeclaredLength()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult head = DecodeRequest(
            codec, 1, HttpMethods.Post, "5", false);
        byte[] firstBytes = Encoding.ASCII.GetBytes("he");
        Http2MessageResult first = codec.DecodeHttp2Content(
            1, HttpMessageKind.Request, firstBytes, false);
        Http2MessageResult last = codec.DecodeHttp2Content(
            1,
            HttpMessageKind.Request,
            Encoding.ASCII.GetBytes("llo"),
            true);

        firstBytes[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.EndStream, Is.False);
            Assert.That(first.IsComplete, Is.False);
            Assert.That(Text(first.Content), Is.EqualTo("he"));
            Assert.That(last.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(last.EndStream, Is.True);
            Assert.That(last.IsComplete, Is.True);
            Assert.That(last.Request, Is.SameAs(head.Request));
            Assert.That(last.Request!.IsComplete, Is.True);
            Assert.That(Text(last.Request.Content), Is.EqualTo("hello"));
        });
    }

    [Test]
    public void ContentLengthSyntaxConflictsAndEndStreamMismatchesAreMalformed()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult malformed = DecodeRequest(
            codec, 3, HttpMethods.Post, "one", false);
        Http2MessageResult conflicting = codec.DecodeHttp2RequestHeaders(
            5,
            RequestHeaders(
                HttpMethods.Post,
                Field("content-length", "3"),
                Field("content-length", "4")),
            false,
            false);
        Http2MessageResult premature = DecodeRequest(
            codec, 7, HttpMethods.Post, "1", true);

        Http2MessageCodec overrunCodec = new Http2MessageCodec();
        DecodeRequest(overrunCodec, 9, HttpMethods.Post, "2", false);
        Http2MessageResult overrun = overrunCodec.DecodeHttp2Content(
            9, HttpMessageKind.Request, Encoding.ASCII.GetBytes("abc"), true);

        Http2MessageCodec underrunCodec = new Http2MessageCodec();
        DecodeRequest(underrunCodec, 11, HttpMethods.Post, "3", false);
        Http2MessageResult underrun = underrunCodec.DecodeHttp2Content(
            11, HttpMessageKind.Request, Encoding.ASCII.GetBytes("ab"), true);

        Assert.Multiple(() =>
        {
            AssertMalformed(malformed, "invalid-http2-content-length");
            AssertMalformed(conflicting, "invalid-http2-content-length");
            AssertMalformed(premature, "http2-content-length-mismatch");
            AssertMalformed(overrun, "http2-content-length-mismatch");
            AssertMalformed(underrun, "http2-content-length-mismatch");
        });
    }

    [Test]
    public void EqualCommaSeparatedContentLengthsAreAccepted()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult head = codec.DecodeHttp2RequestHeaders(
            13,
            RequestHeaders(
                HttpMethods.Post,
                Field("content-length", "003, 3"),
                Field("content-length", "3")),
            false,
            false);
        Http2MessageResult content = codec.DecodeHttp2Content(
            13, HttpMessageKind.Request, Encoding.ASCII.GetBytes("abc"), true);

        Assert.Multiple(() =>
        {
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(content.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(content.IsComplete, Is.True);
        });
    }

    [Test]
    public void TrailersRequireAnActiveMessageEndStreamAndPermittedFields()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        DecodeRequest(codec, 15, HttpMethods.Post, "3", false);
        codec.DecodeHttp2Content(
            15, HttpMessageKind.Request, Encoding.ASCII.GetBytes("abc"), false);
        Http2MessageResult notEnding = codec.DecodeHttp2Trailers(
            15, Headers(Field("x-checksum", "one")), false);
        Http2MessageResult complete = codec.DecodeHttp2Trailers(
            15, Headers(Field("x-checksum", "one")), true);

        Http2MessageCodec pseudoCodec = new Http2MessageCodec();
        DecodeRequest(pseudoCodec, 17, HttpMethods.Post, "0", false);
        Http2MessageResult pseudo = pseudoCodec.DecodeHttp2Trailers(
            17, Headers(Field(":path", "/")), true);

        Http2MessageCodec forbiddenCodec = new Http2MessageCodec();
        DecodeRequest(forbiddenCodec, 19, HttpMethods.Post, "0", false);
        Http2MessageResult forbidden = forbiddenCodec.DecodeHttp2Trailers(
            19, Headers(Field("content-length", "0")), true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                notEnding, "http2-trailers-require-end-stream");
            Assert.That(complete.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(complete.MessageKind,
                Is.EqualTo(HttpMessageKind.Trailers));
            Assert.That(complete.EndStream, Is.True);
            Assert.That(complete.IsComplete, Is.True);
            Assert.That(complete.Request!.IsComplete, Is.True);
            Assert.That(complete.Trailers, Is.SameAs(complete.Request.Trailers));
            Assert.That(Text(complete.Trailers!.Fields[0]), Is.EqualTo("one"));
            AssertMalformed(pseudo, "http2-trailer-pseudo-header");
            AssertMalformed(forbidden, "invalid-http2-trailer-field");
        });
    }

    [Test]
    public void TrailerValuesAreOwnedAndDuplicateCompletionIsRejected()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        DecodeRequest(codec, 21, HttpMethods.Post, "0", false);
        HttpHeaderField source = Field("x-result", "original");
        Http2MessageResult first = codec.DecodeHttp2Trailers(
            21, Headers(source), true);
        source.Value[0] = (byte)'X';
        Http2MessageResult duplicate = codec.DecodeHttp2Trailers(
            21, Headers(Field("x-result", "later")), true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(first.Trailers!.Fields[0]),
                Is.EqualTo("original"));
            AssertMalformed(
                duplicate, "http2-message-already-complete");
        });
    }

    [Test]
    public void InformationalNoContentAndMetadataResponsesApplyContentRules()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult informationalLength =
            codec.DecodeHttp2ResponseHeaders(
                23,
                Headers(
                    Field(":status", "103"),
                    Field("content-length", "0")),
                false);
        Http2MessageResult noContentLength =
            codec.DecodeHttp2ResponseHeaders(
                25,
                Headers(
                    Field(":status", "204"),
                    Field("content-length", "0")),
                true);
        Http2MessageResult notModified =
            codec.DecodeHttp2ResponseHeaders(
                27,
                Headers(
                    Field(":status", "304"),
                    Field("content-length", "123")),
                true);

        Http2MessageCodec noContentCodec = new Http2MessageCodec();
        Http2MessageResult noContentHead =
            noContentCodec.DecodeHttp2ResponseHeaders(
                29, Headers(Field(":status", "204")), false);
        Http2MessageResult prohibited = noContentCodec.DecodeHttp2Content(
            29, HttpMessageKind.Response, new byte[] { 1 }, true);

        Http2MessageCodec emptyCodec = new Http2MessageCodec();
        emptyCodec.DecodeHttp2ResponseHeaders(
            31, Headers(Field(":status", "205")), false);
        Http2MessageResult empty = emptyCodec.DecodeHttp2Content(
            31, HttpMessageKind.Response, Array.Empty<byte>(), true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                informationalLength, "http2-content-length-not-allowed");
            AssertMalformed(
                noContentLength, "http2-content-length-not-allowed");
            Assert.That(notModified.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(notModified.IsComplete, Is.True);
            Assert.That(noContentHead.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(
                prohibited, "http2-response-content-not-allowed");
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.IsComplete, Is.True);
        });
    }

    [Test]
    public void HeadAndSuccessfulConnectResponsesRejectMessageContent()
    {
        Http2MessageCodec headCodec = new Http2MessageCodec();
        DecodeRequest(headCodec, 33, HttpMethods.Head, null, true);
        Http2MessageResult headResponse =
            headCodec.DecodeHttp2ResponseHeaders(
                33,
                Headers(
                    Field(":status", "200"),
                    Field("content-length", "100")),
                false);
        Http2MessageResult headContent = headCodec.DecodeHttp2Content(
            33, HttpMessageKind.Response, new byte[] { 1 }, true);

        Http2MessageCodec connectCodec = new Http2MessageCodec();
        Http2MessageResult connectRequest =
            connectCodec.DecodeHttp2RequestHeaders(
                35,
                Headers(
                    Field(":method", "CONNECT"),
                    Field(":authority", "example.test:443")),
                true,
                false);
        Http2MessageResult connectLength =
            connectCodec.DecodeHttp2ResponseHeaders(
                35,
                Headers(
                    Field(":status", "200"),
                    Field("content-length", "0")),
                false);

        Assert.Multiple(() =>
        {
            Assert.That(headResponse.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(
                headContent, "http2-response-content-not-allowed");
            Assert.That(connectRequest.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(
                connectLength, "http2-content-length-not-allowed");
        });
    }

    [Test]
    public void DataAndTrailersRejectMissingHeadsAndCompletedMessages()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult dataBeforeHead = codec.DecodeHttp2Content(
            37, HttpMessageKind.Request, Array.Empty<byte>(), true);
        Http2MessageResult trailersBeforeHead = codec.DecodeHttp2Trailers(
            39, Headers(Field("x-last", "value")), true);
        Http2MessageResult wrongKind = codec.DecodeHttp2Content(
            41,
            HttpMessageKind.InformationalResponse,
            Array.Empty<byte>(),
            true);
        DecodeRequest(codec, 43, HttpMethods.Get, null, true);
        Http2MessageResult afterComplete = codec.DecodeHttp2Content(
            43, HttpMessageKind.Request, Array.Empty<byte>(), true);
        Http2MessageResult duplicateHead = DecodeRequest(
            codec, 43, HttpMethods.Get, null, true);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                dataBeforeHead, "http2-message-head-not-received");
            AssertMalformed(
                trailersBeforeHead, "http2-message-head-not-received");
            AssertMalformed(
                wrongKind, "invalid-http2-content-message-kind");
            AssertMalformed(
                afterComplete, "http2-message-already-complete");
            AssertMalformed(
                duplicateHead, "http2-request-head-already-received");
        });
    }

    [Test]
    public void RetainedContentLimitIsAppliedBeforeAccumulation()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        codec.limits.MaximumContentSize = 2;
        Http2MessageResult declared = DecodeRequest(
            codec, 45, HttpMethods.Post, "3", false);
        Http2MessageResult head = DecodeRequest(
            codec, 47, HttpMethods.Post, null, false);
        Http2MessageResult content = codec.DecodeHttp2Content(
            47, HttpMessageKind.Request, Encoding.ASCII.GetBytes("abc"), true);

        Assert.Multiple(() =>
        {
            AssertMalformed(declared, "http2-content-limit-exceeded");
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertMalformed(content, "http2-content-limit-exceeded");
        });
    }

    static Http2MessageResult DecodeRequest(
        Http2MessageCodec codec,
        int streamId,
        string method,
        string? contentLength,
        bool endStream)
    {
        if (contentLength == null)
        {
            return codec.DecodeHttp2RequestHeaders(
                streamId, RequestHeaders(method), endStream, false);
        }

        return codec.DecodeHttp2RequestHeaders(
            streamId,
            RequestHeaders(method, Field("content-length", contentLength)),
            endStream,
            false);
    }

    static HttpHeaders RequestHeaders(
        string method,
        params HttpHeaderField[] regularFields)
    {
        HttpHeaders headers = Headers(
            Field(":method", method),
            Field(":scheme", "https"),
            Field(":authority", "example.test"),
            Field(":path", "/"));
        headers.Fields.AddRange(regularFields);
        return headers;
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

    static string Text(HttpHeaderField field)
    {
        return Text(field.Value);
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
