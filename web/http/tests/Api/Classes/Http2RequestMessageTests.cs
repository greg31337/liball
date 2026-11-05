using System.Text;

namespace http.Tests;

public class Http2RequestMessageTests
{
    [Test]
    public void NormalRequestMapsPseudoHeadersAndPreservesRegularFields()
    {
        HttpHeaders headers = Headers(
            Field(":method", "GET"),
            Field(":scheme", "https"),
            Field(":authority", "example.test:8443"),
            Field(":path", "/items/one?q=now"),
            Field("accept", "text/plain"),
            Field("x-order", "one"));
        Http2MessageCodec codec = new Http2MessageCodec();

        Http2MessageResult result = codec.DecodeHttp2RequestHeaders(
            1, headers, true, false);
        HttpRequest request = result.Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.StreamId, Is.EqualTo(1));
            Assert.That(result.MessageKind,
                Is.EqualTo(HttpMessageKind.Request));
            Assert.That(result.Headers, Is.SameAs(headers));
            Assert.That(result.EndStream, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.IsMalformed, Is.False);
            Assert.That(request.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http2));
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.MethodMetadata!.IsSafe, Is.True);
            Assert.That(request.Scheme, Is.EqualTo("https"));
            Assert.That(request.Authority,
                Is.EqualTo("example.test:8443"));
            Assert.That(request.Target, Is.EqualTo("/items/one?q=now"));
            Assert.That(request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(request.Path, Is.EqualTo("/items/one"));
            Assert.That(request.HasQuery, Is.True);
            Assert.That(request.Query, Is.EqualTo("q=now"));
            Assert.That(request.Headers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "accept", "x-order" }));
            Assert.That(request.IsComplete, Is.True);
        });

        headers.Fields[4].Value[0] = (byte)'X';
        Assert.That(Encoding.ASCII.GetString(
            request.Headers.Fields[0].Value), Is.EqualTo("text/plain"));
    }

    [Test]
    public void StandardConnectRequiresOnlyMethodAndHostPortAuthority()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult result = codec.DecodeHttp2RequestHeaders(
            3,
            Headers(
                Field(":method", "CONNECT"),
                Field(":authority", "[2001:db8::1]:443")),
            false,
            false);
        HttpRequest request = result.Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.EndStream, Is.False);
            Assert.That(result.IsComplete, Is.False);
            Assert.That(request.Method, Is.EqualTo(HttpMethods.Connect));
            Assert.That(request.Scheme, Is.Null);
            Assert.That(request.Authority,
                Is.EqualTo("[2001:db8::1]:443"));
            Assert.That(request.Target,
                Is.EqualTo("[2001:db8::1]:443"));
            Assert.That(request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(request.Path, Is.Null);
        });
    }

    [Test]
    public void OptionsAsteriskUsesTheDistinctTargetForm()
    {
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                5,
                Headers(
                    Field(":method", "OPTIONS"),
                    Field(":scheme", "https"),
                    Field(":authority", "example.test"),
                    Field(":path", "*")),
                true,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Request!.Target, Is.EqualTo("*"));
            Assert.That(result.Request.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Asterisk));
            Assert.That(result.Request.Path, Is.Null);
            Assert.That(result.Request.Authority,
                Is.EqualTo("example.test"));
        });
    }

    [Test]
    public void PseudoHeadersMustPrecedeRegularFieldsAndBeUniqueAndKnown()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult ordered = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "GET"),
                Field("accept", "*/*"),
                Field(":scheme", "https"),
                Field(":path", "/")),
            true,
            false);
        Http2MessageResult duplicate = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "GET"),
                Field(":method", "POST"),
                Field(":scheme", "https"),
                Field(":path", "/")),
            true,
            false);
        Http2MessageResult responsePseudo = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", "GET"),
                Field(":status", "200"),
                Field(":scheme", "https"),
                Field(":path", "/")),
            true,
            false);

        Assert.Multiple(() =>
        {
            AssertMalformed(
                ordered, "http2-request-pseudo-header-order");
            AssertMalformed(
                duplicate, "duplicate-http2-request-pseudo-header");
            AssertMalformed(
                responsePseudo, "invalid-http2-request-pseudo-header");
        });
    }

    [Test]
    public void RequiredPseudoHeaderCombinationsAreEnforced()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult missingMethod = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(Field(":scheme", "https"), Field(":path", "/")),
            true,
            false);
        Http2MessageResult missingScheme = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(Field(":method", "GET"), Field(":path", "/")),
            true,
            false);
        Http2MessageResult missingPath = codec.DecodeHttp2RequestHeaders(
            1,
            Headers(Field(":method", "GET"), Field(":scheme", "https")),
            true,
            false);
        Http2MessageResult connectMissingAuthority =
            codec.DecodeHttp2RequestHeaders(
                1, Headers(Field(":method", "CONNECT")), true, false);
        Http2MessageResult connectWithScheme =
            codec.DecodeHttp2RequestHeaders(
                1,
                Headers(
                    Field(":method", "CONNECT"),
                    Field(":scheme", "https"),
                    Field(":authority", "example.test:443")),
                true,
                false);
        Http2MessageResult authorityOmitted =
            codec.DecodeHttp2RequestHeaders(
                1,
                Headers(
                    Field(":method", "GET"),
                    Field(":scheme", "custom"),
                    Field(":path", "/resource")),
                true,
                false);

        Assert.Multiple(() =>
        {
            AssertMalformed(missingMethod, "invalid-http2-request-method");
            AssertMalformed(missingScheme, "invalid-http2-request-scheme");
            AssertMalformed(missingPath, "invalid-http2-request-path");
            AssertMalformed(
                connectMissingAuthority, "invalid-http2-connect-authority");
            AssertMalformed(
                connectWithScheme, "invalid-http2-connect-pseudo-headers");
            Assert.That(authorityOmitted.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(authorityOmitted.Request!.Authority, Is.Null);
        });
    }

    [Test]
    public void MethodSchemePathAndAuthoritySyntaxIsValidated()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        Http2MessageResult invalidMethod = Decode(
            codec, "G ET", "https", "example.test", "/");
        Http2MessageResult invalidScheme = Decode(
            codec, "GET", "1https", "example.test", "/");
        Http2MessageResult relativePath = Decode(
            codec, "GET", "https", "example.test", "relative");
        Http2MessageResult fragment = Decode(
            codec, "GET", "https", "example.test", "/path#fragment");
        Http2MessageResult malformedEscape = Decode(
            codec, "GET", "https", "example.test", "/bad%XX");
        Http2MessageResult wrongAsterisk = Decode(
            codec, "GET", "https", "example.test", "*");
        Http2MessageResult userInfo = Decode(
            codec, "GET", "https", "user@example.test", "/");
        Http2MessageResult emptyHttpHost = Decode(
            codec, "GET", "https", "", "/");
        Http2MessageResult invalidPort = Decode(
            codec, "GET", "https", "example.test:70000", "/");

        Assert.Multiple(() =>
        {
            AssertMalformed(invalidMethod, "invalid-http2-request-method");
            AssertMalformed(invalidScheme, "invalid-http2-request-scheme");
            AssertMalformed(relativePath, "invalid-http2-request-path");
            AssertMalformed(fragment, "invalid-http2-request-path");
            AssertMalformed(malformedEscape, "invalid-http2-request-path");
            AssertMalformed(wrongAsterisk, "invalid-http2-request-path");
            AssertMalformed(userInfo, "invalid-http2-request-authority");
            AssertMalformed(emptyHttpHost, "invalid-http2-request-authority");
            AssertMalformed(invalidPort, "invalid-http2-request-authority");
        });
    }

    [Test]
    public void ConnectRejectsMissingEmptyOrUnsafePortAuthorities()
    {
        Http2MessageCodec codec = new Http2MessageCodec();
        string[] invalidAuthorities =
        {
            "example.test",
            "example.test:",
            "example.test:70000",
            "user@example.test:443",
            ":443"
        };

        Assert.Multiple(() =>
        {
            foreach (string authority in invalidAuthorities)
            {
                Http2MessageResult result = codec.DecodeHttp2RequestHeaders(
                    1,
                    Headers(
                        Field(":method", "CONNECT"),
                        Field(":authority", authority)),
                    false,
                    false);
                AssertMalformed(result, "invalid-http2-connect-authority");
            }
        });
    }

    [Test]
    public void NonAsciiPseudoHeaderValueIsMalformed()
    {
        HttpHeaderField method = new HttpHeaderField();
        method.Name = ":method";
        method.Value = new byte[] { (byte)'G', 0x80, (byte)'T' };
        Http2MessageResult result = new Http2MessageCodec()
            .DecodeHttp2RequestHeaders(
                1,
                Headers(
                    method,
                    Field(":scheme", "https"),
                    Field(":path", "/")),
                true,
                false);

        AssertMalformed(
            result, "invalid-http2-request-pseudo-header-value");
    }

    static Http2MessageResult Decode(
        Http2MessageCodec codec,
        string method,
        string scheme,
        string authority,
        string path)
    {
        return codec.DecodeHttp2RequestHeaders(
            1,
            Headers(
                Field(":method", method),
                Field(":scheme", scheme),
                Field(":authority", authority),
                Field(":path", path)),
            true,
            false);
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
        Assert.That(result.ErrorCodeValue,
            Is.EqualTo((uint)Http2ErrorCode.ProtocolError));
        Assert.That(result.IsKnownErrorCode, Is.True);
        Assert.That(result.ErrorCodeName, Is.EqualTo(errorCodeName));
        Assert.That(result.Request, Is.Null);
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
