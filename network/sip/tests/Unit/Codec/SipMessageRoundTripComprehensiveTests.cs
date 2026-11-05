namespace sip.Tests;

public class SipMessageRoundTripComprehensiveTests
{
    [Test]
    public void CanonicalRoundTripPreservesDuplicateAndUnknownHeadersInOrder()
    {
        byte[] source = Bytes("FROB sip:service@EXAMPLE.com SIP/2.0\r\n"
            + "X-Trace: first\r\nX-Future: alpha;beta=1\r\nX-Trace: second\r\n"
            + "Content-Length: 3\r\n\r\nabc");
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage parsed = codec.ParseStreamMessage(source, ParseOptions()).Message!;

        SipWriteResult written = codec.SerializeMessage(parsed, new SipWriteOptions());
        SipParseResult reparsed = codec.ParseStreamMessage(written.Bytes, ParseOptions());
        string text = Text(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(text, Does.StartWith("FROB sip:service@example.com SIP/2.0\r\n"));
            Assert.That(text.IndexOf("X-Trace: first", System.StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("X-Future: alpha;beta=1", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("X-Future: alpha;beta=1", System.StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("X-Trace: second", System.StringComparison.Ordinal)));
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.Message!.Method, Is.EqualTo(SipMethod.Unknown));
            Assert.That(reparsed.Message.MethodToken, Is.EqualTo("FROB"));
            Assert.That(reparsed.Message.Headers.Where(value => value.Name == "X-Trace")
                .Select(value => value.Value), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(reparsed.Message.Headers.Single(value => value.Name == "X-Future").Value,
                Is.EqualTo("alpha;beta=1"));
            Assert.That(reparsed.Message.Body, Is.EqualTo(Bytes("abc")));
        });
    }

    [Test]
    public void PreservationRoundTripKeepsDuplicateLengthsUnknownHeadersAndBinaryBodyExactly()
    {
        byte[] head = Bytes("FROB sip:service@EXAMPLE.com SIP/2.0\r\n"
            + "X-Trace:  first value \t\r\nX-Trace:\tsecond\r\n"
            + "Content-Length: 5\r\nl: 005\r\n\r\n");
        byte[] body = [0, 255, 13, 10, 65];
        byte[] source = head.Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage parsed = codec.ParseStreamMessage(source, ParseOptions()).Message!;
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;
        preserve.ReconcileContentLength = false;

        SipWriteResult result = codec.SerializeMessage(parsed, preserve);
        result.Bytes[0] = (byte)'X';

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Bytes.Skip(1), Is.EqualTo(source.Skip(1)));
            Assert.That(parsed.OriginalBytes, Is.EqualTo(source));
            Assert.That(parsed.Headers.Count(value => value.Name == "X-Trace"), Is.EqualTo(2));
            Assert.That(parsed.Body, Is.EqualTo(body));
        });
    }

    [Test]
    public void ReconciliationCollapsesPreservedDuplicateContentLengthsOnly()
    {
        byte[] source = Bytes("MESSAGE sip:bob@example.com SIP/2.0\r\n"
            + "X-Trace: one\r\nX-Trace: two\r\nContent-Length: 3\r\nl: 003\r\n\r\nabc");
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage parsed = codec.ParseStreamMessage(source, ParseOptions()).Message!;
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;

        SipWriteResult result = codec.SerializeMessage(parsed, options);
        string text = Text(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(Count(text, "Content-Length:"), Is.EqualTo(1));
            Assert.That(text, Does.Not.Contain("\r\nl:"));
            Assert.That(Count(text, "X-Trace:"), Is.EqualTo(2));
            Assert.That(text, Does.Contain("X-Trace: one\r\nX-Trace: two\r\n"));
        });
    }

    [Test]
    public void UnknownStatusAndUtf8ReasonRoundTripCanonicallyAndExactly()
    {
        byte[] source = Bytes("sip/9.7 799 Café rendezvous\r\n"
            + "X-Future:  retained\r\nContent-Length: 0\r\n\r\n");
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage parsed = codec.ParseStreamMessage(source, ParseOptions()).Message!;
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult exact = codec.SerializeMessage(parsed, preserve);
        SipWriteResult canonical = codec.SerializeMessage(parsed, new SipWriteOptions());
        SipParseResult reparsed = codec.ParseStreamMessage(canonical.Bytes, ParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Bytes, Is.EqualTo(source));
            Assert.That(Text(canonical.Bytes), Does.StartWith("SIP/9.7 799 Café rendezvous\r\n"));
            Assert.That(reparsed.Message!.StatusCode, Is.EqualTo(799));
            Assert.That(reparsed.Message.StatusClass, Is.EqualTo(SipStatusClass.Unknown));
            Assert.That(reparsed.Message.ReasonPhrase, Is.EqualTo("Café rendezvous"));
        });
    }

    [Test]
    public void EveryByteValueSurvivesSerializationAndStreamParsing()
    {
        byte[] body = System.Linq.Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Response;
        message.StatusCode = 200;
        message.ReasonPhrase = "OK";
        message.Body = body;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult written = codec.SerializeMessage(message, new SipWriteOptions());
        SipParseResult reparsed = codec.ParseStreamMessage(written.Bytes, ParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, written.ErrorMessage);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.Message!.Body, Is.EqualTo(body));
            Assert.That(reparsed.Consumed, Is.EqualTo(written.Bytes.Length));
            Assert.That(codec.MeasureMessage(message, new SipWriteOptions()), Is.EqualTo(written.Bytes.Length));
        });
    }

    [Test]
    public void EveryRegisteredCompactHeaderExpandsAndCompactsInMessageRoundTrips()
    {
        SipMessage message = new SipMessage();
        message.Kind = SipMessageKind.Response;
        message.StatusCode = 200;
        message.ReasonPhrase = "OK";
        message.Headers =
        [
            Header("Accept-Contact", "*;+audio"),
            Header("Allow-Events", "presence"),
            Header("Call-ID", "call@example.com"),
            Header("Contact", "<sip:a@example.com>"),
            Header("Content-Encoding", "gzip"),
            Header("Content-Length", "99"),
            Header("Content-Type", "application/sdp"),
            Header("Event", "presence"),
            Header("From", "<sip:a@example.com>;tag=one"),
            Header("Identity", "identity-token"),
            Header("Refer-To", "<sip:b@example.com>"),
            Header("Referred-By", "<sip:a@example.com>"),
            Header("Reject-Contact", "*;+video"),
            Header("Request-Disposition", "proxy"),
            Header("Session-Expires", "90"),
            Header("Subject", "hello"),
            Header("Supported", "timer"),
            Header("To", "<sip:b@example.com>"),
            Header("Via", "SIP/2.0/UDP host.example.com;branch=z9hG4bK-one")
        ];
        SipWriteOptions compactOptions = new SipWriteOptions();
        compactOptions.CompactHeaderNames = true;
        SipMessageCodec codec = new SipMessageCodec();

        SipWriteResult compact = codec.SerializeMessage(message, compactOptions);
        SipMessage reparsed = codec.ParseStreamMessage(compact.Bytes, ParseOptions()).Message!;
        SipWriteResult expanded = codec.SerializeMessage(reparsed, new SipWriteOptions());
        string compactText = Text(compact.Bytes);
        string expandedText = Text(expanded.Bytes);
        string[] aliases =
        [
            "a", "u", "i", "m", "e", "l", "c", "o", "f", "y", "r", "b", "j", "d", "x", "s", "k", "t", "v"
        ];
        string[] names =
        [
            "Accept-Contact", "Allow-Events", "Call-ID", "Contact", "Content-Encoding", "Content-Length",
            "Content-Type", "Event", "From", "Identity", "Refer-To", "Referred-By", "Reject-Contact",
            "Request-Disposition", "Session-Expires", "Subject", "Supported", "To", "Via"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(compact.Success, Is.True, compact.ErrorMessage);
            Assert.That(expanded.Success, Is.True, expanded.ErrorMessage);
            foreach (string alias in aliases)
                Assert.That(compactText, Does.Contain("\r\n" + alias + ":"), alias);
            foreach (string name in names)
                Assert.That(expandedText, Does.Contain("\r\n" + name + ":"), name);
        });
    }

    [Test]
    public void ForwardingRebuildsChangedHeadersButRetainsUntouchedWireForms()
    {
        byte[] body = [0, 255, 65];
        byte[] source = Bytes("FROB sip:service@EXAMPLE.com SIP/2.0\r\n"
            + "Via:  SIP/2.0/UDP old.example.com;branch=z9hG4bK-old\r\n"
            + "Max-Forwards: 070\r\nX-Trace:  retain  this\r\nX-Trace:\tsecond\r\n"
            + "Content-Length: 3\r\n\r\n").Concat(body).ToArray();
        SipMessageCodec codec = new SipMessageCodec();
        SipMessage forwarded = codec.ParseStreamMessage(source, ParseOptions()).Message!;
        SipHeader maxForwards = forwarded.Headers.Single(value => value.Name == "Max-Forwards");
        maxForwards.Value = "69";
        forwarded.Headers = new[]
        {
            Header("Via", "SIP/2.0/TCP new.example.com;branch=z9hG4bK-new")
        }.Concat(forwarded.Headers).ToArray();
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult result = codec.SerializeMessage(forwarded, preserve);
        SipParseResult reparsed = codec.ParseStreamMessage(result.Bytes, ParseOptions());
        string text = Text(result.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(text, Does.StartWith("FROB sip:service@EXAMPLE.com SIP/2.0\r\n"));
            Assert.That(text, Does.Contain("Via: SIP/2.0/TCP new.example.com;branch=z9hG4bK-new\r\n"));
            Assert.That(text, Does.Contain("Via:  SIP/2.0/UDP old.example.com;branch=z9hG4bK-old\r\n"));
            Assert.That(text, Does.Contain("Max-Forwards: 69\r\n"));
            Assert.That(text, Does.Not.Contain("Max-Forwards: 070"));
            Assert.That(text, Does.Contain("X-Trace:  retain  this\r\nX-Trace:\tsecond\r\n"));
            Assert.That(reparsed.Message!.ViaValues, Has.Length.EqualTo(2));
            Assert.That(reparsed.Message.Body, Is.EqualTo(body));
        });
    }

    [Test]
    public void ModifiedStandaloneHeaderDoesNotReuseStaleOriginalBytes()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeader header = codec.ParseHeader(Bytes("Subject:  old value\r\n"), new SipParseOptions()).Header!;
        header.Value = "new value";
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;

        SipWriteResult result = codec.FormatHeader(header, options);

        Assert.That(Text(result.Bytes), Is.EqualTo("Subject: new value\r\n"));
    }

    static SipHeader Header(string name, string value)
    {
        SipHeader header = new SipHeader();
        header.Name = name;
        header.Value = value;
        return header;
    }

    static int Count(string text, string value)
    {
        int count = 0;
        int position = 0;
        while (position < text.Length)
        {
            int found = text.IndexOf(value, position, System.StringComparison.Ordinal);
            if (found < 0) return count;
            count = count + 1;
            position = found + value.Length;
        }
        return count;
    }

    static SipParseOptions ParseOptions()
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = false;
        return options;
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
