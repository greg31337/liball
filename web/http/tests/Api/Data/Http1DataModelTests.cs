using System.Text;

namespace http.Tests;

public class Http1DataModelTests
{
    [Test]
    public void ParserOptionsUseStrictCompatibilityDefaults()
    {
        Http1ParserOptions options = new Http1ParserOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.MessageKind, Is.EqualTo(HttpMessageKind.Request));
            Assert.That(options.ResponseRequestMethod, Is.Null);
            Assert.That(options.IsProxyRecipient, Is.False);
            Assert.That(options.AllowLeadingEmptyRequestLine, Is.True);
            Assert.That(options.AllowObsoleteLineFolding, Is.False);
            Assert.That(options.AllowBareLineFeed, Is.False);
            Assert.That(options.AllowWhitespaceBeforeColon, Is.False);
            Assert.That(options.AllowTransferEncodingWithContentLength, Is.False);
            Assert.That(options.AllowMismatchedContentLengths, Is.False);
            Assert.That(options.AllowChunkedNotFinal, Is.False);
            Assert.That(options.AllowPostCloseData, Is.False);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void ParseEventRetainsHeadContentTrailerAndHandoffMetadata()
    {
        Http1ParseEvent parseEvent = new Http1ParseEvent();
        HttpRequest request = new HttpRequest();
        HttpResponse response = new HttpResponse();
        HttpHeaders trailers = new HttpHeaders();
        Http1FramingDecision framing = new Http1FramingDecision();
        Http1ConnectionDecision connection = new Http1ConnectionDecision();

        parseEvent.EventKind = Http1ParseEventKind.ProtocolSwitch;
        parseEvent.MessageKind = HttpMessageKind.Response;
        parseEvent.Request = request;
        parseEvent.Response = response;
        parseEvent.Content = [0x00, 0x80, 0xff];
        parseEvent.Trailers = trailers;
        parseEvent.FramingDecision = framing;
        parseEvent.ConnectionDecision = connection;
        parseEvent.ProtocolName = "websocket";

        Assert.Multiple(() =>
        {
            Assert.That(parseEvent.EventKind, Is.EqualTo(Http1ParseEventKind.ProtocolSwitch));
            Assert.That(parseEvent.MessageKind, Is.EqualTo(HttpMessageKind.Response));
            Assert.That(parseEvent.Request, Is.SameAs(request));
            Assert.That(parseEvent.Response, Is.SameAs(response));
            Assert.That(parseEvent.Content, Is.EqualTo(new byte[] { 0x00, 0x80, 0xff }));
            Assert.That(parseEvent.Trailers, Is.SameAs(trailers));
            Assert.That(parseEvent.FramingDecision, Is.SameAs(framing));
            Assert.That(parseEvent.ConnectionDecision, Is.SameAs(connection));
            Assert.That(parseEvent.ProtocolName, Is.EqualTo("websocket"));
        });
    }

    [Test]
    public void ParseResultRetainsConsumptionStateEventsAndDiagnostics()
    {
        Http1ParseResult result = new Http1ParseResult();
        Http1ParserOptions options = new Http1ParserOptions();
        Http1ParseEvent parseEvent = new Http1ParseEvent();

        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = 17;
        result.Events.Add(parseEvent);
        result.Options = options;
        result.NeedsEndOfInput = true;
        result.CanAcceptInput = false;
        result.IsTerminal = true;
        result.HasErrorPosition = true;
        result.ErrorPosition = 16;
        result.ErrorCode = "invalid-field";
        result.ErrorMessage = "field contains a prohibited octet";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(17));
            Assert.That(result.Events.Single(), Is.SameAs(parseEvent));
            Assert.That(result.Options, Is.SameAs(options));
            Assert.That(result.NeedsEndOfInput, Is.True);
            Assert.That(result.CanAcceptInput, Is.False);
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.HasErrorPosition, Is.True);
            Assert.That(result.ErrorPosition, Is.EqualTo(16));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-field"));
        });
    }

    [Test]
    public void FramingAndConnectionDecisionsRetainValidatedProtocolState()
    {
        Http1FramingDecision framing = new Http1FramingDecision();
        Http1ConnectionDecision connection = new Http1ConnectionDecision();

        framing.BodyFraming = Http1BodyFraming.Chunked;
        framing.IsValid = true;
        framing.IsSelfDelimited = true;
        framing.HasDeclaredLength = false;
        framing.TransferCodings.Add("gzip");
        framing.TransferCodings.Add("chunked");
        framing.Reason = "final coding is chunked";
        connection.ConnectionMode = Http1ConnectionMode.Upgraded;
        connection.IsPersistent = false;
        connection.MustClose = false;
        connection.IsUpgrade = true;
        connection.IsTunnel = false;
        connection.IsReusable = false;
        connection.ProtocolName = "websocket";
        connection.Reason = "101 response accepted";

        Assert.Multiple(() =>
        {
            Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(framing.IsValid, Is.True);
            Assert.That(framing.IsSelfDelimited, Is.True);
            Assert.That(framing.TransferCodings, Is.EqualTo(new[] { "gzip", "chunked" }));
            Assert.That(connection.ConnectionMode, Is.EqualTo(Http1ConnectionMode.Upgraded));
            Assert.That(connection.IsUpgrade, Is.True);
            Assert.That(connection.IsReusable, Is.False);
            Assert.That(connection.ProtocolName, Is.EqualTo("websocket"));
        });
    }

    [Test]
    public void SerializationOptionsExposeSafeCanonicalDefaults()
    {
        Http1SerializationOptions options = new Http1SerializationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.HasBodyFraming, Is.False);
            Assert.That(options.PreserveFieldNameCasing, Is.True);
            Assert.That(options.ChunkSize, Is.EqualTo(8192));
            Assert.That(options.ChunkExtension, Is.Null);
            Assert.That(options.DeclareTrailers, Is.True);
            Assert.That(options.AddContentLength, Is.True);
            Assert.That(options.VerifyContentLength, Is.True);
            Assert.That(options.UseAbsoluteRequestTarget, Is.False);
            Assert.That(options.RequestTarget, Is.Null);
            Assert.That(options.ResponseRequestMethod, Is.Null);
            Assert.That(options.CloseConnection, Is.False);
            Assert.That(options.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void SerializationResultRetainsOutputCountsAndDecisions()
    {
        Http1SerializationResult result = new Http1SerializationResult();
        Http1SerializationOptions options = new Http1SerializationOptions();
        Http1FramingDecision framing = new Http1FramingDecision();
        Http1ConnectionDecision connection = new Http1ConnectionDecision();
        byte[] bytes = Encoding.ASCII.GetBytes("HTTP/1.1 204 No Content\r\n\r\n");

        result.Status = HttpOperationStatus.Done;
        result.Bytes = bytes;
        result.BytesWritten = bytes.Length;
        result.RequiredSize = bytes.Length;
        result.Options = options;
        result.FramingDecision = framing;
        result.ConnectionDecision = connection;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Bytes, Is.SameAs(bytes));
            Assert.That(result.BytesWritten, Is.EqualTo(bytes.Length));
            Assert.That(result.RequiredSize, Is.EqualTo(bytes.Length));
            Assert.That(result.Options, Is.SameAs(options));
            Assert.That(result.FramingDecision, Is.SameAs(framing));
            Assert.That(result.ConnectionDecision, Is.SameAs(connection));
            Assert.That(result.ErrorCode, Is.Null);
            Assert.That(result.ErrorMessage, Is.Null);
        });
    }
}
