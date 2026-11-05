using System.Text;

namespace http.Tests;

public class Http2ToHttp1ConversionTests
{
    [Test]
    public void RequestConversionBuildsHostLengthAndSerializableOriginForm()
    {
        HttpRequest source = Request(HttpMethods.Post, "/submit?q=1");
        HttpHeaderField agent = Field("user-agent", "example");
        source.Headers = Headers(agent);
        source.Content = Encoding.ASCII.GetBytes("body");
        source.IsComplete = true;
        Http2MessageResult message = Message(
            3, HttpMessageKind.Request);
        message.Request = source;
        message.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(message);
        HttpRequest converted = result.Request!;
        Http1SerializationResult serialized = new Http1Serializer()
            .SerializeHttp1Request(
                converted, new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.StreamId, Is.EqualTo(3));
            Assert.That(converted.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http11));
            Assert.That(converted.Target, Is.EqualTo("/submit?q=1"));
            Assert.That(converted.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(converted.Authority, Is.EqualTo("example.test"));
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "Host", "user-agent", "Content-Length"
                }));
            Assert.That(converted.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "example.test", "example", "4" }));
            Assert.That(result.Headers, Is.SameAs(converted.Headers));
            Assert.That(result.Content,
                Is.EqualTo(Encoding.ASCII.GetBytes("body")));
            Assert.That(result.IsLossyConversion, Is.True);
            Assert.That(result.ConversionNotes,
                Does.Contain("http2-request-scheme-retained-out-of-band"));
            Assert.That(result.ConversionNotes,
                Does.Contain("http1-content-length-framing-selected"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(serialized.Bytes),
                Does.StartWith(
                    "POST /submit?q=1 HTTP/1.1\r\nHost: example.test\r\n"));
        });

        source.Content[0] = (byte)'X';
        agent.Value[0] = (byte)'Y';
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(result.Content),
                Is.EqualTo("body"));
            Assert.That(Text(converted.Headers.Fields[1]),
                Is.EqualTo("example"));
        });
    }

    [Test]
    public void RequestConversionSelectsHostFallbackAsteriskAndAuthorityTargets()
    {
        HttpRequest fallback = Request(HttpMethods.Get, "/");
        fallback.Authority = null;
        fallback.Headers = Headers(Field("host", "fallback.test"));

        HttpRequest asterisk = Request(HttpMethods.Options, "*");

        HttpRequest connect = Request(
            HttpMethods.Connect, "example.test:443");
        connect.Scheme = null;
        connect.Authority = "example.test:443";

        HttpRequest extended = Request(HttpMethods.Connect, "/chat");
        extended.ConnectProtocol = "websocket";

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult fallbackResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(1, fallback));
        Http2MessageResult asteriskResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(3, asterisk));
        Http2MessageResult connectResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(5, connect));
        Http2MessageResult extendedResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(7, extended));

        Assert.Multiple(() =>
        {
            Assert.That(fallbackResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Text(fallbackResult.Request!.Headers.Fields[0]),
                Is.EqualTo("fallback.test"));
            Assert.That(fallbackResult.Request.Authority,
                Is.EqualTo("fallback.test"));
            Assert.That(asteriskResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(asteriskResult.Request!.Target, Is.EqualTo("*"));
            Assert.That(asteriskResult.Request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
            Assert.That(connectResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(connectResult.Request!.Target,
                Is.EqualTo("example.test:443"));
            Assert.That(connectResult.Request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(extendedResult.Status,
                Is.EqualTo(HttpOperationStatus.Unsupported));
            Assert.That(extendedResult.ErrorCodeName,
                Is.EqualTo(
                    "http2-extended-connect-not-representable-in-http1"));
        });
    }

    [Test]
    public void RequestTrailersSelectChunkingDeclarationAndTeConnectionPolicy()
    {
        HttpRequest source = Request(HttpMethods.Post, "/upload");
        HttpHeaderField te = Field("te", "trailers");
        te.IsSensitive = true;
        te.NeverIndex = true;
        source.Headers = Headers(
            te,
            Field("content-length", "3"),
            Field("trailer", "old-name"));
        source.Content = Encoding.ASCII.GetBytes("abc");
        source.Trailers = Headers(
            Field("x-checksum", "first"),
            Field("x-checksum", "second"),
            Field("x-trace", "done"));
        source.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(RequestMessage(9, source));
        HttpRequest converted = result.Request!;
        Http1SerializationResult serialized = new Http1Serializer()
            .SerializeHttp1Request(
                converted, new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[]
                {
                    "Host", "TE", "Connection", "Transfer-Encoding",
                    "Trailer"
                }));
            Assert.That(converted.Headers.Fields.Select(Text),
                Is.EqualTo(new[]
                {
                    "example.test", "trailers", "TE", "chunked",
                    "x-checksum, x-trace"
                }));
            Assert.That(converted.Headers.Fields[1].IsSensitive, Is.True);
            Assert.That(converted.Headers.Fields[1].NeverIndex, Is.True);
            Assert.That(converted.Trailers.Fields, Has.Count.EqualTo(3));
            Assert.That(result.ConversionNotes,
                Does.Contain("http1-chunked-framing-selected"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(serialized.Bytes),
                Does.Contain("Transfer-Encoding: chunked\r\n"));
            Assert.That(Encoding.ASCII.GetString(serialized.Bytes),
                Does.EndWith(
                    "0\r\nx-checksum: first\r\nx-checksum: second\r\n" +
                    "x-trace: done\r\n\r\n"));
        });
    }

    [Test]
    public void ResponseConversionSynthesizesReasonAndExactLength()
    {
        HttpResponse source = Response(HttpStatusCodes.Ok);
        HttpHeaderField server = Field("server", "example");
        source.Headers = Headers(server);
        source.Content = Encoding.ASCII.GetBytes("body");
        source.IsComplete = true;
        Http2MessageResult message = ResponseMessage(11, source);
        message.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(message);
        HttpResponse converted = result.Response!;
        Http1SerializationResult serialized = new Http1Serializer()
            .SerializeHttp1Response(
                converted, new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(converted.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http11));
            Assert.That(converted.HasReasonPhrase, Is.True);
            Assert.That(Encoding.ASCII.GetString(converted.ReasonPhrase),
                Is.EqualTo(HttpReasonPhrases.Ok));
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "server", "Content-Length" }));
            Assert.That(converted.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "example", "4" }));
            Assert.That(result.IsLossyConversion, Is.True);
            Assert.That(result.ConversionNotes,
                Does.Contain("http1-reason-phrase-synthesized"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(serialized.Bytes),
                Does.StartWith("HTTP/1.1 200 OK\r\n"));
        });

        source.Content[0] = (byte)'X';
        server.Value[0] = (byte)'Y';
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(result.Content),
                Is.EqualTo("body"));
            Assert.That(Text(converted.Headers.Fields[0]),
                Is.EqualTo("example"));
        });
    }

    [Test]
    public void ResponseTrailersSelectChunkingAndACompleteDeclaration()
    {
        HttpResponse source = Response(HttpStatusCodes.Ok);
        source.Content = Encoding.ASCII.GetBytes("abc");
        source.Trailers = Headers(
            Field("digest", "one"),
            Field("x-trace", "two"));
        source.IsComplete = true;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(ResponseMessage(13, source));
        HttpResponse converted = result.Response!;
        Http1SerializationResult serialized = new Http1Serializer()
            .SerializeHttp1Response(
                converted, new Http1SerializationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(converted.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "Transfer-Encoding", "Trailer" }));
            Assert.That(converted.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "chunked", "digest, x-trace" }));
            Assert.That(result.Trailers, Is.SameAs(converted.Trailers));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(serialized.Bytes),
                Does.EndWith("0\r\ndigest: one\r\nx-trace: two\r\n\r\n"));
        });
    }

    [Test]
    public void ResponseStatusAndTunnelPoliciesSelectLegalFraming()
    {
        HttpResponse hints = Response(HttpStatusCodes.EarlyHints);
        hints.IsInformational = true;
        HttpResponse noContent = Response(HttpStatusCodes.NoContent);
        HttpResponse reset = Response(HttpStatusCodes.ResetContent);
        HttpResponse notModified = Response(HttpStatusCodes.NotModified);
        notModified.Headers = Headers(Field("content-length", "123"));
        HttpResponse extension = Response(699);
        HttpResponse tunnel = Response(HttpStatusCodes.Ok);

        Http2MessageResult hintResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(InformationalMessage(15, hints));
        Http2MessageResult noContentResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(ResponseMessage(17, noContent));
        Http2MessageResult resetResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(ResponseMessage(19, reset));
        Http2MessageResult metadataResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(ResponseMessage(21, notModified));
        Http2MessageResult extensionResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(ResponseMessage(23, extension));
        Http2MessageResult tunnelMessage = ResponseMessage(25, tunnel);
        tunnelMessage.IsTunnel = true;
        Http2MessageResult tunnelResult = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(tunnelMessage);

        Assert.Multiple(() =>
        {
            Assert.That(hintResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(
                hintResult.Response!.ReasonPhrase),
                Is.EqualTo(HttpReasonPhrases.EarlyHints));
            Assert.That(hintResult.Response.Headers.Fields, Is.Empty);
            Assert.That(noContentResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(noContentResult.Response!.Headers.Fields, Is.Empty);
            Assert.That(resetResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(resetResult.Response!.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "0" }));
            Assert.That(metadataResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(metadataResult.Response!.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "123" }));
            Assert.That(extensionResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(extensionResult.Response!.ReasonPhrase, Is.Empty);
            Assert.That(extensionResult.Response.Headers.Fields.Select(Text),
                Is.EqualTo(new[] { "0" }));
            Assert.That(tunnelResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(tunnelResult.Response!.Headers.Fields, Is.Empty);
            Assert.That(tunnelResult.ConversionNotes,
                Does.Contain("http1-tunnel-framing-selected"));
        });
    }

    [Test]
    public void ConversionRejectsInvalidMappedMessagesAndUnsafeFields()
    {
        Http2MessageResult failed = Message(1, HttpMessageKind.Request);
        failed.Status = HttpOperationStatus.Rejected;

        HttpRequest wrongVersion = Request(HttpMethods.Get, "/");
        wrongVersion.ProtocolVersion = HttpProtocolVersion.Http11;

        HttpRequest connection = Request(HttpMethods.Get, "/");
        connection.Headers = Headers(Field("connection", "close"));

        HttpRequest mismatch = Request(HttpMethods.Post, "/");
        mismatch.Headers = Headers(Field("content-length", "2"));
        mismatch.Content = Encoding.ASCII.GetBytes("abc");

        HttpResponse badTrailer = Response(HttpStatusCodes.Ok);
        badTrailer.Trailers = Headers(Field("content-length", "0"));

        HttpResponse tunnelContent = Response(HttpStatusCodes.Ok);
        tunnelContent.Content = new byte[] { 1 };
        Http2MessageResult tunnel = ResponseMessage(11, tunnelContent);
        tunnel.IsTunnel = true;

        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult failedResult = codec.ConvertHttp2ToHttp1(failed);
        Http2MessageResult versionResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(3, wrongVersion));
        Http2MessageResult connectionResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(5, connection));
        Http2MessageResult mismatchResult = codec.ConvertHttp2ToHttp1(
            RequestMessage(7, mismatch));
        Http2MessageResult trailerResult = codec.ConvertHttp2ToHttp1(
            ResponseMessage(9, badTrailer));
        Http2MessageResult tunnelResult = codec.ConvertHttp2ToHttp1(tunnel);

        Assert.Multiple(() =>
        {
            AssertRejected(failedResult, "invalid-http2-conversion-input");
            AssertRejected(
                versionResult, "invalid-http2-request-conversion-input");
            AssertRejected(
                connectionResult, "http2-connection-specific-field");
            AssertRejected(
                mismatchResult, "http2-content-length-mismatch");
            AssertRejected(trailerResult, "invalid-http2-trailer-field");
            AssertRejected(
                tunnelResult, "invalid-http2-tunnel-response-conversion");
        });
    }

    [Test]
    public void ConversionReportsIndexingAndServerPushInformationLoss()
    {
        HttpResponse response = Response(HttpStatusCodes.Ok);
        HttpHeaderField secret = Field("set-cookie", "session=secret");
        secret.IsSensitive = true;
        secret.NeverIndex = true;
        response.Headers = Headers(secret);
        Http2MessageResult message = ResponseMessage(27, response);
        message.IsPushed = true;
        message.HasAssociatedStreamId = true;
        message.AssociatedStreamId = 1;

        Http2MessageResult result = new Http2MessageCodec()
            .ConvertHttp2ToHttp1(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsLossyConversion, Is.True);
            Assert.That(result.ConversionNotes,
                Does.Contain(
                    "hpack-indexing-metadata-has-no-http1-wire-form"));
            Assert.That(result.ConversionNotes,
                Does.Contain(
                    "http2-server-push-association-has-no-http1-wire-form"));
            Assert.That(result.IsPushed, Is.True);
            Assert.That(result.HasAssociatedStreamId, Is.True);
            Assert.That(result.AssociatedStreamId, Is.EqualTo(1));
            Assert.That(result.Response!.Headers.Fields[0].IsSensitive,
                Is.True);
            Assert.That(result.Response.Headers.Fields[0].NeverIndex,
                Is.True);
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

    static HttpRequest Request(string method, string target)
    {
        HttpRequest request = new HttpRequest();
        request.ProtocolVersion = HttpProtocolVersion.Http2;
        request.Method = method;
        request.Scheme = "https";
        request.Authority = "example.test";
        request.Target = target;
        request.TargetForm = target == "*" ?
            HttpRequestTargetForm.Asterisk : HttpRequestTargetForm.Origin;
        request.IsComplete = true;
        return request;
    }

    static HttpResponse Response(int statusCode)
    {
        HttpResponse response = new HttpResponse();
        response.ProtocolVersion = HttpProtocolVersion.Http2;
        response.StatusCode = statusCode;
        response.IsComplete = true;
        return response;
    }

    static Http2MessageResult RequestMessage(int streamId, HttpRequest request)
    {
        Http2MessageResult result = Message(streamId, HttpMessageKind.Request);
        result.Request = request;
        result.IsComplete = request.IsComplete;
        return result;
    }

    static Http2MessageResult ResponseMessage(int streamId, HttpResponse response)
    {
        Http2MessageResult result = Message(streamId, HttpMessageKind.Response);
        result.Response = response;
        result.IsComplete = response.IsComplete;
        return result;
    }

    static Http2MessageResult InformationalMessage(
        int streamId,
        HttpResponse response)
    {
        Http2MessageResult result = Message(
            streamId, HttpMessageKind.InformationalResponse);
        result.Response = response;
        result.IsComplete = response.IsComplete;
        return result;
    }

    static Http2MessageResult Message(
        int streamId,
        HttpMessageKind messageKind)
    {
        Http2MessageResult result = new Http2MessageResult();
        result.Status = HttpOperationStatus.Done;
        result.StreamId = streamId;
        result.MessageKind = messageKind;
        return result;
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
