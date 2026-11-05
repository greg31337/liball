namespace http.Tests;

public class HttpExceptionTests
{
    [Test]
    public void ProtocolAndParseExceptionsRetainMessagesAndLocations()
    {
        HttpProtocolException protocol = new HttpProtocolException(
            "invalid protocol state", "invalid-state");
        HttpParseException parse = new HttpParseException(
            "invalid start line", "invalid-start-line", 17);

        Assert.Multiple(() =>
        {
            Assert.That(protocol.Message, Is.EqualTo("invalid protocol state"));
            Assert.That(protocol.DiagnosticCode, Is.EqualTo("invalid-state"));
            Assert.That(parse.Message, Is.EqualTo("invalid start line"));
            Assert.That(parse.DiagnosticCode, Is.EqualTo("invalid-start-line"));
            Assert.That(parse.Position, Is.EqualTo(17));
        });
    }

    [Test]
    public void LimitFieldAndCodingExceptionsRetainOperationContext()
    {
        HttpLimitExceededException limit = new HttpLimitExceededException(
            "content is too large", "content-size", 1024, 1025);
        HttpFieldException field = new HttpFieldException(
            "invalid field value", "invalid-value", "Content-Length");
        HttpContentCodingException coding = new HttpContentCodingException(
            "invalid compressed stream", "malformed-coding", "gzip");

        Assert.Multiple(() =>
        {
            Assert.That(limit.Message, Is.EqualTo("content is too large"));
            Assert.That(limit.LimitName, Is.EqualTo("content-size"));
            Assert.That(limit.Limit, Is.EqualTo(1024));
            Assert.That(limit.Actual, Is.EqualTo(1025));
            Assert.That(field.DiagnosticCode, Is.EqualTo("invalid-value"));
            Assert.That(field.Message, Is.EqualTo("invalid field value"));
            Assert.That(field.FieldName, Is.EqualTo("Content-Length"));
            Assert.That(coding.DiagnosticCode, Is.EqualTo("malformed-coding"));
            Assert.That(coding.Message,
                Is.EqualTo("invalid compressed stream"));
            Assert.That(coding.Coding, Is.EqualTo("gzip"));
        });
    }

    [Test]
    public void EveryPublicExceptionHasDirectConstructorCoverage()
    {
        Type[] expected =
        [
            typeof(HpackException),
            typeof(Http1FramingException),
            typeof(Http2ConnectionException),
            typeof(Http2StreamException),
            typeof(HttpContentCodingException),
            typeof(HttpFieldException),
            typeof(HttpLimitExceededException),
            typeof(HttpParseException),
            typeof(HttpProtocolException)
        ];
        Type[] actual = typeof(HttpProtocolException).Assembly.GetTypes()
            .Where(type => type.IsPublic)
            .Where(type => typeof(Exception).IsAssignableFrom(type))
            .ToArray();

        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Test]
    public void FramingAndHpackExceptionsRetainProtocolDiagnostics()
    {
        Http1FramingException framing = new Http1FramingException(
            "ambiguous message framing", "transfer-encoding-content-length",
            Http1BodyFraming.Invalid);
        HpackException hpack = new HpackException(
            "invalid dynamic table index", "invalid-index", 3);

        Assert.Multiple(() =>
        {
            Assert.That(framing.Message, Is.EqualTo("ambiguous message framing"));
            Assert.That(framing.DiagnosticCode, Is.EqualTo("transfer-encoding-content-length"));
            Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.Invalid));
            Assert.That(hpack.Message, Is.EqualTo("invalid dynamic table index"));
            Assert.That(hpack.DiagnosticCode, Is.EqualTo("invalid-index"));
            Assert.That(hpack.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public void Http2ExceptionsRetainKnownAndUnknownRawErrorCodes()
    {
        Http2ConnectionException connection = new Http2ConnectionException(
            "frame size invalid", "invalid-frame-size", Http2ErrorCode.FrameSizeError,
            0x06u, true);
        Http2StreamException stream = new Http2StreamException(
            "extension stream failure", "extension-error", 31, Http2ErrorCode.Unknown,
            0xf0000001u, false);

        Assert.Multiple(() =>
        {
            Assert.That(connection.Message, Is.EqualTo("frame size invalid"));
            Assert.That(connection.DiagnosticCode, Is.EqualTo("invalid-frame-size"));
            Assert.That(connection.ErrorCode, Is.EqualTo(Http2ErrorCode.FrameSizeError));
            Assert.That(connection.ErrorCodeValue, Is.EqualTo(0x06u));
            Assert.That(connection.IsKnownErrorCode, Is.True);
            Assert.That(stream.Message, Is.EqualTo("extension stream failure"));
            Assert.That(stream.DiagnosticCode, Is.EqualTo("extension-error"));
            Assert.That(stream.StreamId, Is.EqualTo(31));
            Assert.That(stream.ErrorCode, Is.EqualTo(Http2ErrorCode.Unknown));
            Assert.That(stream.ErrorCodeValue, Is.EqualTo(0xf0000001u));
            Assert.That(stream.IsKnownErrorCode, Is.False);
        });
    }
}
