namespace sip.Tests;

public class SipMessageSerializationTests
{
    [Test]
    public void CanonicalSerializationReconcilesLengthAndMeasuresExactBytes()
    {
        SipMessage message = CreateRequest("SIP:Alice@EXAMPLE.COM;Transport=udp", Bytes("abc"));
        message.MethodToken = "IGNORED";
        message.ProtocolVersion = "sip/2.0";
        message.Headers =
        [
            Header("X-Trace", "café"),
            Header("Content-Length", "99"),
            Header("l", "98")
        ];
        SipMessageCodec codec = new SipMessageCodec();
        SipWriteOptions options = new SipWriteOptions();

        SipWriteResult result = codec.SerializeMessage(message, options);
        int measured = codec.MeasureMessage(message, options);
        string head = System.Text.Encoding.UTF8.GetString(result.Bytes.Take(result.Bytes.Length - 3).ToArray());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Written, Is.EqualTo(result.Bytes.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(result.Bytes.Length));
        Assert.That(measured, Is.EqualTo(result.Bytes.Length));
        Assert.That(head, Is.EqualTo("INVITE sip:Alice@example.com;transport=udp SIP/2.0\r\n"
            + "X-Trace: café\r\nContent-Length: 3\r\n\r\n"));
        Assert.That(result.Bytes.TakeLast(3), Is.EqualTo(Bytes("abc")));
        Assert.That(CountText(head, "Content-Length:"), Is.EqualTo(1));

        SipParseResult reparsed = codec.ParseStreamMessage(result.Bytes, CompatibilityOptions());
        Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(reparsed.Message!.Body, Is.EqualTo(Bytes("abc")));
    }

    [Test]
    public void SerializesResponseStartLineAndArbitraryBinaryBody()
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Response;
        message.ProtocolVersion = "sip/2.0";
        message.StatusCode = 486;
        message.ReasonPhrase = "Busy Here";
        message.Body = [0, 255, 13, 10];
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult result = codec.SerializeMessage(message, new SipWriteOptions());

        Assert.That(result.Success, Is.True);
        byte[] expectedPrefix = Bytes("SIP/2.0 486 Busy Here\r\nContent-Length: 4\r\n\r\n");
        Assert.That(result.Bytes.Take(expectedPrefix.Length), Is.EqualTo(expectedPrefix));
        Assert.That(result.Bytes.Skip(expectedPrefix.Length), Is.EqualTo(message.Body));
    }

    [Test]
    public void CompactOptionUsesRegisteredCompactHeaderNames()
    {
        SipMessage message = CreateRequest("sip:bob@example.com", new byte[0]);
        message.Headers =
        [
            Header("Via", "SIP/2.0/UDP client.example.com;branch=z9hG4bK-one"),
            Header("From", "Alice <sip:alice@example.com>;tag=one"),
            Header("To", "Bob <sip:bob@example.com>"),
            Header("Contact", "<sip:alice@client.example.com>"),
            Header("Content-Type", "application/sdp"),
            Header("Content-Length", "12")
        ];
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult result = codec.SerializeMessage(message, options);
        string text = System.Text.Encoding.ASCII.GetString(result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(text, Does.Contain("\r\nv: SIP/2.0/UDP client.example.com;branch=z9hG4bK-one\r\n"));
        Assert.That(text, Does.Contain("\r\nf: Alice <sip:alice@example.com>;tag=one\r\n"));
        Assert.That(text, Does.Contain("\r\nt: Bob <sip:bob@example.com>\r\n"));
        Assert.That(text, Does.Contain("\r\nm: <sip:alice@client.example.com>\r\n"));
        Assert.That(text, Does.Contain("\r\nc: application/sdp\r\n"));
        Assert.That(text, Does.Contain("\r\nl: 0\r\n\r\n"));
    }

    [Test]
    public void UnchangedParsedMessageCanBePreservedByteForByte()
    {
        byte[] source = Bytes("OPTIONS sip:service@EXAMPLE.com SIP/2.0\r\n"
            + "v:  SIP/2.0/UDP host.example.com;branch=z9hG4bK-one\r\n"
            + "l:  0\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();
        SipParseResult parsed = codec.ParseStreamMessage(source, CompatibilityOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;

        SipWriteResult result = codec.SerializeMessage(parsed.Message!, options);
        result.Bytes[0] = (byte)'X';

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes.Skip(1), Is.EqualTo(source.Skip(1)));
        Assert.That(parsed.Message!.OriginalBytes[0], Is.EqualTo((byte)'O'));
    }

    [Test]
    public void ChangedBodyRebuildsPreservedMessageAndReconcilesLength()
    {
        byte[] source = Bytes("MESSAGE sip:bob@EXAMPLE.com SIP/2.0\r\n"
            + "X-Spacing:  retained form\r\nl:  0\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage message = codec.ParseStreamMessage(source, CompatibilityOptions()).Message!;
        message.Body = Bytes("new-body");
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;

        SipWriteResult result = codec.SerializeMessage(message, options);
        string text = System.Text.Encoding.UTF8.GetString(result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(text, Does.StartWith("MESSAGE sip:bob@EXAMPLE.com SIP/2.0\r\n"));
        Assert.That(text, Does.Contain("X-Spacing:  retained form\r\n"));
        Assert.That(text, Does.Contain("Content-Length: 8\r\n\r\nnew-body"));
        Assert.That(text, Is.Not.EqualTo(System.Text.Encoding.UTF8.GetString(source)));
    }

    [Test]
    public void ReconciliationAndFoldingPolicyOverrideIncompatiblePreservedForms()
    {
        SipMessageCodec codec = new SipMessageCodec();
        byte[] datagram = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\nX-Test: value\r\n\r\nabc");
        SipMessage datagramMessage = codec.ParseDatagramMessage(datagram, CompatibilityOptions()).Message!;
        SipParseOptions foldedParse = new SipParseOptions();
        foldedParse.AllowHeaderFolding = true;
        foldedParse.Strict = false;
        SipMessage foldedMessage = codec.ParseStreamMessage(Bytes("OPTIONS sip:bob@example.com SIP/2.0\r\n"
            + "Subject: first\r\n second\r\nContent-Length: 0\r\n\r\n"), foldedParse).Message!;
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult reconciled = codec.SerializeMessage(datagramMessage, preserve);
        SipWriteResult unfolded = codec.SerializeMessage(foldedMessage, preserve);
        string reconciledText = System.Text.Encoding.UTF8.GetString(reconciled.Bytes);
        string unfoldedText = System.Text.Encoding.UTF8.GetString(unfolded.Bytes);

        Assert.That(reconciled.Success, Is.True);
        Assert.That(reconciledText, Does.Contain("Content-Length: 3\r\n\r\nabc"));
        Assert.That(unfolded.Success, Is.True);
        Assert.That(unfoldedText, Does.Contain("Subject: first second\r\n"));
        Assert.That(unfoldedText, Does.Not.Contain("\r\n second"));
    }

    [Test]
    public void ReconciliationCanBeDisabledForTransparentOutput()
    {
        SipMessage message = CreateRequest("sip:bob@example.com", Bytes("abc"));
        message.Headers =
        [
            Header("Content-Length", "1"),
            Header("l", "2"),
            Header("X-Future", "unchanged")
        ];
        SipWriteOptions options = new SipWriteOptions();
        options.ReconcileContentLength = false;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult result = codec.SerializeMessage(message, options);
        string text = System.Text.Encoding.ASCII.GetString(result.Bytes);

        Assert.That(result.Success, Is.True);
        Assert.That(CountText(text, "Content-Length:"), Is.EqualTo(2));
        Assert.That(text, Does.Contain("Content-Length: 1\r\nContent-Length: 2\r\n"));
        Assert.That(result.Bytes.TakeLast(3), Is.EqualTo(Bytes("abc")));
    }

    [Test]
    public void FoldingOccursOnlyWhenEnabledAndAtExistingWhitespace()
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Response;
        message.StatusCode = 200;
        message.ReasonPhrase = "OK";
        message.Headers = [Header("X-Long", "alpha beta gamma delta epsilon")];
        SipWriteOptions strict = new SipWriteOptions();
        strict.ReconcileContentLength = false;
        strict.MaxHeaderLineBytes = 18;
        SipWriteOptions folded = new SipWriteOptions();
        folded.ReconcileContentLength = false;
        folded.AllowHeaderFolding = true;
        folded.MaxHeaderLineBytes = 18;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult rejected = codec.SerializeMessage(message, strict);
        SipWriteResult accepted = codec.SerializeMessage(message, folded);
        string text = System.Text.Encoding.ASCII.GetString(accepted.Bytes);

        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.RequiredBytes, Is.GreaterThan(18));
        Assert.That(accepted.Success, Is.True);
        Assert.That(text, Does.Contain("\r\n "));
        Assert.That(HeaderLineLengths(text).All(length => length <= 18), Is.True);

        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.AllowHeaderFolding = true;
        parseOptions.Strict = false;
        SipParseResult reparsed = codec.ParseDatagramMessage(accepted.Bytes, parseOptions);
        Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(reparsed.Message!.Headers[0].Value, Is.EqualTo("alpha beta gamma delta epsilon"));
    }

    [Test]
    public void FoldingRejectsAnUnbreakableValue()
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Response;
        message.StatusCode = 200;
        message.ReasonPhrase = "OK";
        message.Headers = [Header("X-Blob", "abcdefghijklmnopqrstuvwxyz")];
        SipWriteOptions options = new SipWriteOptions();
        options.ReconcileContentLength = false;
        options.AllowHeaderFolding = true;
        options.MaxHeaderLineBytes = 12;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult result = codec.SerializeMessage(message, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("cannot be folded"));
    }

    [Test]
    public void InvalidModelsReturnWriteErrorsAndMeasurementThrowsProtocolException()
    {
        SipMessage invalid = new SipMessage();
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult result = codec.SerializeMessage(invalid, new SipWriteOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        Assert.That(result.ErrorMessage, Does.Contain("message kind"));
        Assert.Throws<SipProtocolException>(() => codec.MeasureMessage(invalid, new SipWriteOptions()));
        Assert.That(codec.SerializeMessage(null!, new SipWriteOptions()).Success, Is.False);
        Assert.That(codec.SerializeMessage(invalid, null!).Success, Is.False);
    }

    static SipMessage CreateRequest(string uriText, byte[] body)
    {
        SipUriCodec uriCodec = new SipUriCodec();
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.PreserveOriginal = false;
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Request;
        message.Method = SipMethod.Invite;
        message.MethodToken = "INVITE";
        message.RequestUri = uriCodec.ParseUri(Bytes(uriText), parseOptions).Uri;
        message.Body = body;
        return message;
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static int CountText(string text, string value)
    {
        int count = 0;
        int offset = 0;
        while (offset < text.Length)
        {
            int found = text.IndexOf(value, offset, StringComparison.Ordinal);
            if (found < 0) return count;
            count = count + 1;
            offset = found + value.Length;
        }
        return count;
    }

    static int[] HeaderLineLengths(string text)
    {
        string headerText = text.Substring(text.IndexOf("\r\n", StringComparison.Ordinal) + 2);
        string[] lines = headerText.Split("\r\n", StringSplitOptions.None);
        return lines.TakeWhile(line => line.Length > 0).Select(line => Bytes(line + "\r\n").Length).ToArray();
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
