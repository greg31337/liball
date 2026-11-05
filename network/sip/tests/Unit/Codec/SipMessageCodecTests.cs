namespace sip.Tests;

public class SipMessageCodecTests
{
    [Test]
    public void ParsesRequestAndAggregatesTypedAndDuplicateHeaders()
    {
        string text = "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP first.example.com;branch=z9hG4bK-one\r\n"
            + "v: SIP/2.0/TCP second.example.com;branch=z9hG4bK-two\r\n"
            + "Route: <sip:proxy1.example.com;lr>, <sip:proxy2.example.com;lr>\r\n"
            + "Record-Route: <sip:edge.example.com;lr>\r\n"
            + "m: Alice <sip:alice@client.example.com>\r\n"
            + "f: Alice <sip:alice@example.com>;tag=from-tag\r\n"
            + "t: Bob <sip:bob@example.com>\r\n"
            + "CSeq: 314 INVITE\r\n"
            + "c: application/sdp\r\n"
            + "Content-Disposition: session;handling=required\r\n"
            + "X-Trace: one\r\n"
            + "X-Trace: two\r\n"
            + "Content-Length: 0\r\n\r\n";
        byte[] source = Bytes(text);
        int originalLength = source.Length;
        SipMessageCodec codec = new SipMessageCodec();

        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        SipParseResult result = codec.ParseStreamMessage(source, options);
        source[0] = (byte)'X';

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(originalLength));
        Assert.That(result.RequiredBytes, Is.EqualTo(originalLength));
        Assert.That(result.Message, Is.Not.Null);
        SipMessage message = result.Message!;
        Assert.Multiple(() =>
        {
            Assert.That(message.Kind, Is.EqualTo(SipMessageKind.Request));
            Assert.That(message.Method, Is.EqualTo(SipMethod.Invite));
            Assert.That(message.MethodToken, Is.EqualTo("INVITE"));
            Assert.That(message.RequestUri!.Host, Is.EqualTo("example.com"));
            Assert.That(message.ProtocolVersion, Is.EqualTo("SIP/2.0"));
            Assert.That(message.Headers, Has.Length.EqualTo(13));
            Assert.That(message.Headers.Count(header => header.Name == "X-Trace"), Is.EqualTo(2));
            Assert.That(message.Headers[10].Value, Is.EqualTo("one"));
            Assert.That(message.Headers[11].Value, Is.EqualTo("two"));
            Assert.That(message.ViaValues.Select(value => value.Host),
                Is.EqualTo(new string[] { "first.example.com", "second.example.com" }));
            Assert.That(message.RouteValues, Has.Length.EqualTo(2));
            Assert.That(message.RecordRouteValues, Has.Length.EqualTo(1));
            Assert.That(message.ContactValues, Has.Length.EqualTo(1));
            Assert.That(message.From!.DisplayName, Is.EqualTo("Alice"));
            Assert.That(message.To!.DisplayName, Is.EqualTo("Bob"));
            Assert.That(message.CSeq!.SequenceNumber, Is.EqualTo(314));
            Assert.That(message.ContentType!.Subtype, Is.EqualTo("sdp"));
            Assert.That(message.ContentDisposition!.DispositionToken, Is.EqualTo("session"));
            Assert.That(message.Body, Is.Empty);
            Assert.That(message.OriginalBytes[0], Is.EqualTo((byte)'I'));
            Assert.That(message.Headers[0].OriginalBytes, Is.Not.Empty);
        });
    }

    [Test]
    public void ParsesResponseAndPreservesUnknownStatusAndUtf8Reason()
    {
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseDatagramMessage(
            Bytes("sip/3.12 799 Café rendezvous\r\nX-Empty:\r\n\r\n"), CompatibilityOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Message, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Message!.Kind, Is.EqualTo(SipMessageKind.Response));
            Assert.That(result.Message.StatusCode, Is.EqualTo(799));
            Assert.That(result.Message.StatusClass, Is.EqualTo(SipStatusClass.Unknown));
            Assert.That(result.Message.ProtocolVersion, Is.EqualTo("sip/3.12"));
            Assert.That(result.Message.ReasonPhrase, Is.EqualTo("Café rendezvous"));
            Assert.That(result.Message.Headers[0].Name, Is.EqualTo("X-Empty"));
            Assert.That(result.Message.Headers[0].Value, Is.Empty);
        });
    }

    [Test]
    public void PreservesExtensionMethodAndUnknownHeader()
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions options = new SipParseOptions();
        options.PreserveOriginal = false;
        options.Strict = false;

        SipParseResult result = codec.ParseStreamMessage(
            Bytes("X-CUSTOM urn:service:example SIP/2.0\r\nX-Future: alpha;beta=1\r\nl: 0\r\n\r\n"), options);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.Multiple(() =>
        {
            Assert.That(result.Message!.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(result.Message.MethodToken, Is.EqualTo("X-CUSTOM"));
            Assert.That(result.Message.RequestUri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
            Assert.That(result.Message.Headers[0].Name, Is.EqualTo("X-Future"));
            Assert.That(result.Message.Headers[0].Value, Is.EqualTo("alpha;beta=1"));
            Assert.That(result.Message.Headers[0].OriginalBytes, Is.Empty);
            Assert.That(result.Message.OriginalBytes, Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase("INVITE sip:bob@example.com SIP/2.0")]
    [TestCase("INVITE sip:bob@example.com SIP/2.0\r\n")]
    [TestCase("INVITE sip:bob@example.com SIP/2.0\r\nX-Test: value")]
    [TestCase("INVITE sip:bob@example.com SIP/2.0\r\nX-Test: value\r")]
    public void ReturnsIncompleteUntilHeaderTerminatorArrives(string text)
    {
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(Bytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Incomplete));
        Assert.That(result.Message, Is.Null);
        Assert.That(result.Consumed, Is.Zero);
        Assert.That(result.RequiredBytes, Is.EqualTo(-1));
        Assert.That(result.ErrorOffset, Is.EqualTo(-1));
    }

    [Test]
    public void CompatibleWhitespaceAcceptsNonCanonicalStartLineSeparators()
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.AllowCompatibleWhitespace = true;
        compatible.Strict = false;
        byte[] request = Bytes("OPTIONS\t \tsip:service@example.com\t SIP/2.0\r\nSubject \t: value\r\nContent-Length: 0\r\n\r\n");
        byte[] response = Bytes("SIP/2.0\t 486\t Busy Here\r\nContent-Length: 0\r\n\r\n");

        SipParseResult strict = codec.ParseStreamMessage(request, new SipParseOptions());
        SipParseResult acceptedRequest = codec.ParseStreamMessage(request, compatible);
        SipParseResult acceptedResponse = codec.ParseStreamMessage(response, compatible);

        Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(strict.ErrorOffset, Is.EqualTo(7));
        Assert.That(acceptedRequest.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(acceptedRequest.Message!.Headers[0].Name, Is.EqualTo("Subject"));
        Assert.That(acceptedResponse.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(acceptedResponse.Message!.StatusClass, Is.EqualTo(SipStatusClass.ClientError));
        Assert.That(acceptedResponse.Message.ReasonPhrase, Is.EqualTo("Busy Here"));
    }

    [Test]
    public void HeaderFoldingRequiresItsCompatibilityOption()
    {
        string text = "MESSAGE sip:bob@example.com SIP/2.0\r\nSubject: first\r\n\tsecond part\r\nContent-Length: 0\r\n\r\n";
        int foldOffset = text.IndexOf("\tsecond", StringComparison.Ordinal);
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.AllowHeaderFolding = true;
        compatible.Strict = false;

        SipParseResult strict = codec.ParseStreamMessage(Bytes(text), new SipParseOptions());
        SipParseResult accepted = codec.ParseStreamMessage(Bytes(text), compatible);

        Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(strict.ErrorOffset, Is.EqualTo(foldOffset));
        Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(accepted.Message!.Headers[0].Value, Is.EqualTo("first second part"));
        Assert.That(System.Text.Encoding.ASCII.GetString(accepted.Message.Headers[0].OriginalBytes),
            Is.EqualTo("Subject: first\r\n\tsecond part\r\n"));
    }

    [TestCase("INVITE sip:bob@example.com SIP/2.0\n\n", 34)]
    [TestCase("INVITE sip:bob@example.com SIP/2.0\rX", 34)]
    [TestCase("IN(VITE sip:bob@example.com SIP/2.0\r\n\r\n", 2)]
    [TestCase("INVITE sip:bob@example.com SIP/2.x\r\n\r\n", 33)]
    [TestCase("SIP/2.0 48X Busy\r\n\r\n", 10)]
    public void MalformedStartLinesReportExactOffsets(string text, int expectedOffset)
    {
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(Bytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(expectedOffset));
        Assert.That(result.Consumed, Is.EqualTo(expectedOffset));
        Assert.That(result.ValidationIssues[0].ByteOffset, Is.EqualTo(expectedOffset));
        Assert.That(result.Message, Is.Null);
    }

    [Test]
    public void KnownHeaderErrorsAreTranslatedToMessageByteOffsets()
    {
        string prefix = "OPTIONS sip:service@example.com SIP/2.0\r\n";
        string text = prefix + "Content-Length: invalid\r\n\r\n";
        SipMessageCodec codec = new SipMessageCodec();

        SipParseResult result = codec.ParseStreamMessage(Bytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset,
            Is.EqualTo(System.Text.Encoding.ASCII.GetByteCount(prefix + "Content-Length: ")));
        Assert.That(result.ErrorMessage, Does.Contain("Content-Length"));
    }

    [Test]
    public void InvalidUtf8ReportsTheLeadByteInStartLineAndHeader()
    {
        SipMessageCodec codec = new SipMessageCodec();
        byte[] responsePrefix = Bytes("SIP/2.0 200 ");
        byte[] invalidStart = responsePrefix.Concat(new byte[] { 0xC0, 13, 10, 13, 10 }).ToArray();
        byte[] headerPrefix = Bytes("OPTIONS sip:service@example.com SIP/2.0\r\nX-Text: ");
        byte[] invalidHeader = headerPrefix.Concat(new byte[] { 0xE2, 0x28, 0xA1, 13, 10, 13, 10 }).ToArray();

        SipParseResult startResult = codec.ParseStreamMessage(invalidStart, new SipParseOptions());
        SipParseResult headerResult = codec.ParseStreamMessage(invalidHeader, new SipParseOptions());

        Assert.That(startResult.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(startResult.ErrorOffset, Is.EqualTo(responsePrefix.Length));
        Assert.That(headerResult.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(headerResult.ErrorOffset, Is.EqualTo(headerPrefix.Length));
    }

    [Test]
    public void ConfiguredLimitsFailAtTheirExactBoundaries()
    {
        string start = "OPTIONS sip:service@example.com SIP/2.0\r\n";
        string firstHeader = "X-One: 1234\r\n";
        string secondHeader = "X-Two: 5678\r\n";
        string text = start + firstHeader + secondHeader + "\r\n";
        SipMessageCodec codec = new SipMessageCodec();

        SipParseOptions startLimit = new SipParseOptions();
        startLimit.MaxStartLineBytes = Bytes(start).Length - 1;
        SipParseOptions lineLimit = new SipParseOptions();
        lineLimit.MaxHeaderLineBytes = Bytes(firstHeader).Length - 1;
        SipParseOptions countLimit = new SipParseOptions();
        countLimit.MaxHeaderCount = 1;
        SipParseOptions headerLimit = new SipParseOptions();
        headerLimit.MaxHeaderBytes = Bytes(firstHeader + secondHeader + "\r\n").Length - 1;
        SipParseOptions messageLimit = new SipParseOptions();
        messageLimit.MaxMessageBytes = Bytes(text).Length - 1;

        SipParseResult limitedStart = codec.ParseStreamMessage(Bytes(text), startLimit);
        SipParseResult limitedLine = codec.ParseStreamMessage(Bytes(text), lineLimit);
        SipParseResult limitedCount = codec.ParseStreamMessage(Bytes(text), countLimit);
        SipParseResult limitedHeaders = codec.ParseStreamMessage(Bytes(text), headerLimit);
        SipParseResult limitedMessage = codec.ParseStreamMessage(Bytes(text), messageLimit);

        Assert.That(limitedStart.ErrorOffset, Is.EqualTo(startLimit.MaxStartLineBytes));
        Assert.That(limitedLine.ErrorOffset, Is.EqualTo(Bytes(start).Length + lineLimit.MaxHeaderLineBytes));
        Assert.That(limitedCount.ErrorOffset, Is.EqualTo(Bytes(start + firstHeader).Length));
        Assert.That(limitedHeaders.ErrorOffset, Is.EqualTo(Bytes(start).Length + headerLimit.MaxHeaderBytes));
        Assert.That(limitedMessage.ErrorOffset, Is.EqualTo(messageLimit.MaxMessageBytes));
        Assert.That(new[] { limitedStart, limitedLine, limitedCount, limitedHeaders, limitedMessage }
            .All(result => result.Status == SipParseStatus.Error), Is.True);
    }

    [Test]
    public void RejectsNegativeLimitsAndMissingInputs()
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipParseOptions invalid = new SipParseOptions();
        invalid.MaxHeaderCount = -1;

        SipParseResult sourceMissing = codec.ParseStreamMessage(null!, new SipParseOptions());
        SipParseResult optionsMissing = codec.ParseStreamMessage(new byte[0], null!);
        SipParseResult invalidLimit = codec.ParseStreamMessage(new byte[0], invalid);

        Assert.That(sourceMissing.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(optionsMissing.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(invalidLimit.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(invalidLimit.ErrorMessage, Does.Contain("negative"));
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static SipParseOptions CompatibilityOptions()
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        return options;
    }
}
