namespace sip.Tests;

public class SipSdpComprehensiveTests
{
    [Test]
    public void ParsesEveryModeledSessionAndMediaFieldAndRoundTripsThem()
    {
        string source = "v=0\r\n"
            + "o=jdoe 3724394400 3724394405 IN IP4 198.51.100.1\r\n"
            + "s=Call to John Smith\r\n"
            + "i=SDP Offer #1\r\n"
            + "u=http://www.jdoe.example.com/home.html\r\n"
            + "e=Jane Doe <jane@jdoe.example.com>\r\n"
            + "e=john@example.com (John)\r\n"
            + "p=+1 617 555-6011\r\n"
            + "c=IN IP4 198.51.100.1\r\n"
            + "b=CT:1024\r\n"
            + "b=X-YZ:128\r\n"
            + "t=3724394400 3730536000\r\n"
            + "r=7d 1h 0 25h\r\n"
            + "z=3730928400 -1h 3749680800 0\r\n"
            + "k=prompt\r\n"
            + "a=tool:sip-test\r\n"
            + "a=recvonly\r\n"
            + "x=session-extension\r\n"
            + "m=audio 49170/2 RTP/AVP 0 96\r\n"
            + "i=Primary audio\r\n"
            + "c=IN IP4 233.252.0.1/127/2\r\n"
            + "b=AS:64\r\n"
            + "k=base64:YWJjZA==\r\n"
            + "a=rtpmap:96 opus/48000/2\r\n"
            + "a=rtcp-mux\r\n"
            + "y=media-extension\r\n";
        SipSdpCodec codec = new SipSdpCodec();

        SipParseResult parsed = codec.ParseSessionDescription(Bytes(source), new SipParseOptions());
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatSessionDescription(parsed.SessionDescription!, canonical);
        SipParseResult reparsed = codec.ParseSessionDescription(written.Bytes, new SipParseOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        SipSessionDescription description = parsed.SessionDescription!;
        Assert.Multiple(() =>
        {
            Assert.That(description.ProtocolVersion, Is.Zero);
            Assert.That(description.Origin,
                Is.EqualTo("jdoe 3724394400 3724394405 IN IP4 198.51.100.1"));
            Assert.That(description.SessionName, Is.EqualTo("Call to John Smith"));
            Assert.That(description.SessionInformation, Is.EqualTo("SDP Offer #1"));
            Assert.That(description.Uri, Is.EqualTo("http://www.jdoe.example.com/home.html"));
            Assert.That(description.Emails, Is.EqualTo(new[]
            {
                "Jane Doe <jane@jdoe.example.com>", "john@example.com (John)"
            }));
            Assert.That(description.Phones, Is.EqualTo(new[] { "+1 617 555-6011" }));
            Assert.That(description.Connection, Is.EqualTo("IN IP4 198.51.100.1"));
            Assert.That(description.Bandwidths, Is.EqualTo(new[] { "CT:1024", "X-YZ:128" }));
            Assert.That(description.Timings, Is.EqualTo(new[] { "3724394400 3730536000" }));
            Assert.That(description.RepeatTimes, Is.EqualTo(new[] { "7d 1h 0 25h" }));
            Assert.That(description.TimeZones, Is.EqualTo("3730928400 -1h 3749680800 0"));
            Assert.That(description.EncryptionKey, Is.EqualTo("prompt"));
            Assert.That(description.Attributes.Select(attribute => attribute.Name),
                Is.EqualTo(new[] { "tool", "recvonly" }));
            Assert.That(description.UnknownLines, Is.EqualTo(new[] { "x=session-extension" }));
            Assert.That(description.MediaDescriptions, Has.Length.EqualTo(1));
            AssertMedia(description.MediaDescriptions[0]);
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.SessionDescription!.Bandwidths,
                Is.EqualTo(description.Bandwidths));
            Assert.That(reparsed.SessionDescription.UnknownLines,
                Is.EqualTo(description.UnknownLines));
            AssertMedia(reparsed.SessionDescription.MediaDescriptions[0]);
        });
    }

    [Test]
    public void PreservesStandardAndUnknownPropertyAndValueAttributes()
    {
        string source = MinimalPrefix()
            + "a=cat:foo.bar\r\n"
            + "a=keywds:SDP session description protocol\r\n"
            + "a=tool:foobar V3.2\r\n"
            + "a=type:meeting\r\n"
            + "a=charset:UTF-8\r\n"
            + "a=sdplang:fr\r\n"
            + "a=lang:de\r\n"
            + "a=x-note:café\r\n"
            + "a=inactive\r\n"
            + "m=video 51372 RTP/AVP 99\r\n"
            + "a=rtpmap:99 h263-1998/90000\r\n"
            + "a=framerate:60\r\n"
            + "a=quality:10\r\n"
            + "a=orient:landscape\r\n"
            + "a=x-media-flag\r\n";
        SipSdpCodec codec = new SipSdpCodec();

        SipParseResult parsed = codec.ParseSessionDescription(Bytes(source), new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult written = codec.FormatSessionDescription(parsed.SessionDescription!, options);
        SipParseResult reparsed = codec.ParseSessionDescription(written.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.SessionDescription!.Attributes.Select(attribute => attribute.Name),
                Is.EqualTo(new[]
                {
                    "cat", "keywds", "tool", "type", "charset", "sdplang", "lang",
                    "x-note", "inactive"
                }));
            Assert.That(parsed.SessionDescription.Attributes.Single(attribute =>
                attribute.Name == "x-note").Value, Is.EqualTo("café"));
            Assert.That(parsed.SessionDescription.Attributes.Last().HasValue, Is.False);
            Assert.That(parsed.SessionDescription.MediaDescriptions[0].Attributes.Select(attribute =>
                attribute.Name), Is.EqualTo(new[]
                {
                    "rtpmap", "framerate", "quality", "orient", "x-media-flag"
                }));
            Assert.That(parsed.SessionDescription.MediaDescriptions[0].Attributes.Last().HasValue,
                Is.False);
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.SessionDescription!.Attributes.Single(attribute =>
                attribute.Name == "x-note").Value, Is.EqualTo("café"));
        });
    }

    [Test]
    public void SupportsRfcIpv4Ipv6AndLayeredMulticastConnectionForms()
    {
        string source = "v=0\r\n"
            + "o=jdoe 3724395000 3724395001 IN IP6 2001:db8::1\r\n"
            + "s=-\r\n"
            + "t=0 0\r\n"
            + "m=audio 49170 RTP/AVP 0\r\n"
            + "c=IN IP4 233.252.0.1/127/3\r\n"
            + "m=video 51372 RTP/AVP 99\r\n"
            + "c=IN IP6 ff00::db8:0:101/3\r\n"
            + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n"
            + "c=IN IP6 2001:db8::2\r\n";

        SipParseResult parsed = Parse(source);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.SessionDescription!.Origin,
                Does.EndWith("IN IP6 2001:db8::1"));
            Assert.That(parsed.SessionDescription.Connection, Is.Null);
            Assert.That(parsed.SessionDescription.MediaDescriptions.Select(media => media.Connection),
                Is.EqualTo(new[]
                {
                    "IN IP4 233.252.0.1/127/3",
                    "IN IP6 ff00::db8:0:101/3",
                    "IN IP6 2001:db8::2"
                }));
        });
    }

    [Test]
    public void RejectsMalformedStructureAndFieldCardinality()
    {
        string prefix = MinimalPrefix();
        byte[][] malformed =
        [
            Bytes("V=0\r\n" + prefix.Substring(5)),
            Bytes("version=0\r\n" + prefix.Substring(5)),
            Bytes("v=0\r\nv=0\r\n" + prefix.Substring(5)),
            Bytes("o=- 1 1 IN IP4 192.0.2.1\r\nv=0\r\ns=-\r\n"
                + "c=IN IP4 192.0.2.1\r\nt=0 0\r\n"),
            Bytes("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nt=0 0\r\n"
                + "c=IN IP4 192.0.2.1\r\n"),
            Bytes("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\n"
                + "c=IN IP4 192.0.2.1\r\nr=7d 1h 0\r\n"),
            Bytes("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\n"
                + "c=IN IP4 192.0.2.1\r\nt=0 0\r\nz=3730928400 -1h\r\n"),
            Bytes(prefix + "m=audio 49170 RTP/AVP 0\r\ns=late\r\n"),
            Bytes(prefix + "m=audio 49170 RTP/AVP 0\r\ni=one\r\ni=two\r\n"),
            Bytes(prefix + "a=empty:\r\n"),
            Bytes(prefix + "m=audio 49170 RTP//AVP 0\r\n"),
            Bytes(prefix + "\r\n")
        ];
        byte[] invalidUtf8 = Bytes(prefix).Concat(new byte[] { (byte)'a', (byte)'=', 0xc3, 0x28,
            (byte)'\r', (byte)'\n' }).ToArray();
        byte[] nul = Bytes(prefix).Concat(new byte[] { (byte)'a', (byte)'=', 0,
            (byte)'\r', (byte)'\n' }).ToArray();

        SipParseResult[] results = malformed.Concat(new[] { invalidUtf8, nul })
            .Select(source => new SipSdpCodec().ParseSessionDescription(
                source, new SipParseOptions())).ToArray();

        Assert.That(results, Has.All.Matches<SipParseResult>(result =>
            result.Status == SipParseStatus.Error));
    }

    [Test]
    public void RejectsInvalidUnicastAndMulticastAddressForms()
    {
        string[] connections =
        [
            "IN IP4 233.252.0.1",
            "IN IP4 192.0.2.1/127",
            "IN IP6 2001:db8::1/3",
            "IN IP4 240.0.0.1"
        ];
        SipParseResult[] sessionResults = connections.Select(connection => Parse(
            "v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=" + connection
            + "\r\nt=0 0\r\n")).ToArray();
        SipParseResult multicastOrigin = Parse("v=0\r\n"
            + "o=- 1 1 IN IP6 ff00::db8:1\r\ns=-\r\n"
            + "c=IN IP6 2001:db8::1\r\nt=0 0\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(sessionResults, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Error));
            Assert.That(multicastOrigin.Status, Is.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void EnforcesExactBodyLineCollectionAndWriteLimits()
    {
        string source = MinimalPrefix()
            + "a=sendrecv\r\n"
            + "a=rtcp-mux\r\n"
            + "m=audio 49170 RTP/AVP 0 96\r\n"
            + "a=mid:audio\r\n";
        byte[] bytes = Bytes(source);
        int longestLine = source.Split("\r\n", StringSplitOptions.None)
            .Max(line => Bytes(line).Length);
        SipSdpCodec codec = new SipSdpCodec();
        SipParseOptions exact = new SipParseOptions();
        exact.MaxMessageBytes = bytes.Length;
        exact.MaxBodyBytes = bytes.Length;
        exact.MaxHeaderLineBytes = longestLine;
        exact.MaxListValueCount = 2;
        SipParseOptions messageTooSmall = CopyLimits(exact);
        messageTooSmall.MaxMessageBytes = bytes.Length - 1;
        SipParseOptions bodyTooSmall = CopyLimits(exact);
        bodyTooSmall.MaxBodyBytes = bytes.Length - 1;
        SipParseOptions lineTooSmall = CopyLimits(exact);
        lineTooSmall.MaxHeaderLineBytes = longestLine - 1;
        SipParseOptions collectionTooSmall = CopyLimits(exact);
        collectionTooSmall.MaxListValueCount = 1;

        SipParseResult accepted = codec.ParseSessionDescription(bytes, exact);
        SipWriteOptions exactWrite = new SipWriteOptions();
        exactWrite.MaxHeaderLineBytes = longestLine;
        SipWriteOptions smallWrite = new SipWriteOptions();
        smallWrite.MaxHeaderLineBytes = longestLine - 1;
        SipWriteResult written = codec.FormatSessionDescription(
            accepted.SessionDescription!, exactWrite);
        SipWriteResult writeRejected = codec.FormatSessionDescription(
            accepted.SessionDescription!, smallWrite);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(codec.ParseSessionDescription(bytes, messageTooSmall).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseSessionDescription(bytes, bodyTooSmall).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseSessionDescription(bytes, lineTooSmall).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseSessionDescription(bytes, collectionTooSmall).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(written.Success, Is.True);
            Assert.That(writeRejected.Success, Is.False);
        });
    }

    [Test]
    public void CompatibilityModeCanonicalizesOrderingWhitespaceAndLineEndings()
    {
        byte[] source = Bytes("v=0\n"
            + "o=-\t1  1 IN IP4 192.0.2.1\n"
            + "s=-\n"
            + "t=0  0\n"
            + "c=IN  IP4 192.0.2.1");
        SipSdpCodec codec = new SipSdpCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;

        SipParseResult parsed = codec.ParseSessionDescription(source, compatible);
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatSessionDescription(parsed.SessionDescription!, canonical);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.ValidationIssues, Is.Not.Empty);
            Assert.That(parsed.ValidationIssues.All(issue =>
                issue.Severity == SipValidationSeverity.Warning), Is.True);
            Assert.That(Text(written.Bytes), Is.EqualTo("v=0\r\n"
                + "o=- 1 1 IN IP4 192.0.2.1\r\n"
                + "s=-\r\n"
                + "c=IN IP4 192.0.2.1\r\n"
                + "t=0 0\r\n"));
        });
    }

    [Test]
    public void CanonicalFormatterEmitsTheRfcFieldOrder()
    {
        SipSessionDescription description = new SipSessionDescription();
        description.Origin = "- 1 1 IN IP4 192.0.2.1";
        description.SessionName = "-";
        description.SessionInformation = "information";
        description.Uri = "https://example.com/session";
        description.Emails = new[] { "alice@example.com" };
        description.Phones = new[] { "+1 555 0100" };
        description.Connection = "IN IP4 192.0.2.1";
        description.Bandwidths = new[] { "X-YZ:128" };
        description.Timings = new[] { "0 0" };
        description.EncryptionKey = "uri:https://example.com/key";
        description.Attributes = new[] { Attribute("sendrecv", null, false) };
        description.UnknownLines = new[] { "x=extension" };
        SipMediaDescription media = new SipMediaDescription();
        media.Media = "audio";
        media.Port = 49170;
        media.PortCount = 2;
        media.Protocol = "RTP/AVP";
        media.Formats = new[] { "0" };
        media.Title = "audio";
        media.Connection = "IN IP6 2001:db8::2";
        media.Bandwidths = new[] { "AS:64" };
        media.EncryptionKey = "clear:secret";
        media.Attributes = new[] { Attribute("mid", "audio", true) };
        media.UnknownLines = new[] { "y=extension" };
        description.MediaDescriptions = new[] { media };

        SipWriteResult written = new SipSdpCodec().FormatSessionDescription(
            description, new SipWriteOptions());

        Assert.That(written.Success, Is.True);
        Assert.That(Text(written.Bytes), Is.EqualTo("v=0\r\n"
            + "o=- 1 1 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "i=information\r\n"
            + "u=https://example.com/session\r\n"
            + "e=alice@example.com\r\n"
            + "p=+1 555 0100\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "b=X-YZ:128\r\n"
            + "t=0 0\r\n"
            + "k=uri:https://example.com/key\r\n"
            + "a=sendrecv\r\n"
            + "x=extension\r\n"
            + "m=audio 49170/2 RTP/AVP 0\r\n"
            + "i=audio\r\n"
            + "c=IN IP6 2001:db8::2\r\n"
            + "b=AS:64\r\n"
            + "k=clear:secret\r\n"
            + "a=mid:audio\r\n"
            + "y=extension\r\n"));
    }

    static void AssertMedia(SipMediaDescription media)
    {
        Assert.Multiple(() =>
        {
            Assert.That(media.Media, Is.EqualTo("audio"));
            Assert.That(media.Port, Is.EqualTo(49170));
            Assert.That(media.PortCount, Is.EqualTo(2));
            Assert.That(media.Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(media.Formats, Is.EqualTo(new[] { "0", "96" }));
            Assert.That(media.Title, Is.EqualTo("Primary audio"));
            Assert.That(media.Connection, Is.EqualTo("IN IP4 233.252.0.1/127/2"));
            Assert.That(media.Bandwidths, Is.EqualTo(new[] { "AS:64" }));
            Assert.That(media.EncryptionKey, Is.EqualTo("base64:YWJjZA=="));
            Assert.That(media.Attributes.Select(attribute => attribute.Name),
                Is.EqualTo(new[] { "rtpmap", "rtcp-mux" }));
            Assert.That(media.Attributes[1].HasValue, Is.False);
            Assert.That(media.UnknownLines, Is.EqualTo(new[] { "y=media-extension" }));
        });
    }

    static SipSdpAttribute Attribute(string name, string? value, bool hasValue)
    {
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        attribute.Value = value;
        attribute.HasValue = hasValue;
        return attribute;
    }

    static SipParseOptions CopyLimits(SipParseOptions source)
    {
        SipParseOptions copy = new SipParseOptions();
        copy.MaxMessageBytes = source.MaxMessageBytes;
        copy.MaxBodyBytes = source.MaxBodyBytes;
        copy.MaxHeaderLineBytes = source.MaxHeaderLineBytes;
        copy.MaxListValueCount = source.MaxListValueCount;
        return copy;
    }

    static string MinimalPrefix()
    {
        return "v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\n"
            + "c=IN IP4 192.0.2.1\r\nt=0 0\r\n";
    }

    static SipParseResult Parse(string text)
    {
        return new SipSdpCodec().ParseSessionDescription(Bytes(text), new SipParseOptions());
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
