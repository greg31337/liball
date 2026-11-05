namespace sip.Tests;

public class SipSdpCodecTests
{
    [Test]
    public void ParsesSessionMediaAndRealTimeCommunicationAttributes()
    {
        string text = "v=0\r\n"
            + "o=alice 2890844526 2890842807 IN IP4 192.0.2.10\r\n"
            + "s=SDP Seminar\r\n"
            + "i=A Seminar on SDP\r\n"
            + "u=https://example.com/seminar\r\n"
            + "e=alice@example.com (Alice)\r\n"
            + "p=+1 617 555-6011\r\n"
            + "c=IN IP4 233.252.0.1/127\r\n"
            + "b=CT:512\r\n"
            + "t=3724394400 3730536000\r\n"
            + "r=7d 1h 0 25h\r\n"
            + "z=3730928400 -1h 3749680800 0\r\n"
            + "k=prompt\r\n"
            + "a=group:BUNDLE audio video\r\n"
            + "a=ice-options:trickle\r\n"
            + "x=session-extension\r\n"
            + "m=audio 49170/2 RTP/AVP 0 96\r\n"
            + "i=Primary audio\r\n"
            + "c=IN IP4 233.252.0.10/127\r\n"
            + "b=AS:64\r\n"
            + "k=clear:legacy\r\n"
            + "a=mid:audio\r\n"
            + "a=rtcp:49171 IN IP4 192.0.2.10\r\n"
            + "a=ice-ufrag:F7gI\r\n"
            + "a=ice-pwd:x9cml/YzichV2+XlhiMu8g\r\n"
            + "a=candidate:1 1 UDP 2130706431 192.0.2.10 49170 typ host\r\n"
            + "a=end-of-candidates\r\n"
            + "y=media-extension\r\n"
            + "m=video 51372 RTP/AVP 31\r\n"
            + "a=mid:video\r\n";

        SipParseResult result = Parse(text);
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = new SipSdpCodec().FormatSessionDescription(
            result.SessionDescription!, canonical);
        SipParseResult reparsed = new SipSdpCodec().ParseSessionDescription(
            written.Bytes, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.ValidationIssues, Is.Empty);
        SipSessionDescription description = result.SessionDescription!;
        Assert.Multiple(() =>
        {
            Assert.That(description.Origin, Is.EqualTo("alice 2890844526 2890842807 IN IP4 192.0.2.10"));
            Assert.That(description.SessionName, Is.EqualTo("SDP Seminar"));
            Assert.That(description.Connection, Is.EqualTo("IN IP4 233.252.0.1/127"));
            Assert.That(description.Bandwidths, Is.EqualTo(new[] { "CT:512" }));
            Assert.That(description.Timings, Is.EqualTo(new[] { "3724394400 3730536000" }));
            Assert.That(description.RepeatTimes, Is.EqualTo(new[] { "7d 1h 0 25h" }));
            Assert.That(description.TimeZones, Is.EqualTo("3730928400 -1h 3749680800 0"));
            Assert.That(description.Attributes.Select(value => value.Name),
                Is.EqualTo(new[] { "group", "ice-options" }));
            Assert.That(description.UnknownLines, Is.EqualTo(new[] { "x=session-extension" }));
            Assert.That(description.MediaDescriptions, Has.Length.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Port, Is.EqualTo(49170));
            Assert.That(description.MediaDescriptions[0].PortCount, Is.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Protocol, Is.EqualTo("RTP/AVP"));
            Assert.That(description.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "96" }));
            Assert.That(description.MediaDescriptions[0].Attributes.Select(value => value.Name),
                Is.EqualTo(new[] { "mid", "rtcp", "ice-ufrag", "ice-pwd", "candidate", "end-of-candidates" }));
            Assert.That(description.MediaDescriptions[0].Attributes.Last().HasValue, Is.False);
            Assert.That(description.MediaDescriptions[0].UnknownLines,
                Is.EqualTo(new[] { "y=media-extension" }));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.SessionDescription!.UnknownLines,
                Is.EqualTo(new[] { "x=session-extension" }));
            Assert.That(reparsed.SessionDescription.MediaDescriptions[0].UnknownLines,
                Is.EqualTo(new[] { "y=media-extension" }));
        });
    }

    [Test]
    public void SupportsIpv6AndIpv4MulticastConnectionForms()
    {
        string text = "v=0\r\n"
            + "o=- 3890123412 3890123412 IN IP6 2001:db8::10\r\n"
            + "s=Multicast\r\n"
            + "c=IN IP6 2001:db8::20\r\n"
            + "t=0 0\r\n"
            + "m=audio 5004 RTP/AVP 0\r\n"
            + "c=IN IP6 ff00::db8:0:101/3\r\n"
            + "m=video 5006 RTP/AVP 31\r\n"
            + "c=IN IP4 233.252.0.1/127/3\r\n";

        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.SessionDescription!.Connection, Is.EqualTo("IN IP6 2001:db8::20"));
            Assert.That(result.SessionDescription.MediaDescriptions[0].Connection,
                Is.EqualTo("IN IP6 ff00::db8:0:101/3"));
            Assert.That(result.SessionDescription.MediaDescriptions[1].Connection,
                Is.EqualTo("IN IP4 233.252.0.1/127/3"));
        });
    }

    [Test]
    public void FormatsCanonicalSdpAndRoundTripsTheModeledStructure()
    {
        SipSessionDescription description = MinimalDescription();
        SipSdpAttribute bundle = Attribute("group", "BUNDLE 0", true);
        description.Attributes = new[] { bundle };
        SipMediaDescription audio = new SipMediaDescription();
        audio.Media = "audio";
        audio.Port = 49170;
        audio.Protocol = "RTP/AVP";
        audio.Formats = new[] { "0", "96" };
        audio.Attributes = new[] { Attribute("mid", "0", true), Attribute("rtcp-mux", null, false) };
        description.MediaDescriptions = new[] { audio };
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;

        SipWriteResult written = new SipSdpCodec().FormatSessionDescription(description, options);
        SipParseResult reparsed = new SipSdpCodec().ParseSessionDescription(written.Bytes,
            new SipParseOptions());

        string expected = "v=0\r\n"
            + "o=- 3890123412 3890123412 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "a=group:BUNDLE 0\r\n"
            + "m=audio 49170 RTP/AVP 0 96\r\n"
            + "a=mid:0\r\n"
            + "a=rtcp-mux\r\n";
        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(System.Text.Encoding.UTF8.GetString(written.Bytes), Is.EqualTo(expected));
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.SessionDescription!.Attributes[0].Value, Is.EqualTo("BUNDLE 0"));
            Assert.That(reparsed.SessionDescription.MediaDescriptions[0].Attributes[1].HasValue, Is.False);
        });
    }

    [Test]
    public void PreservesExactCompatibleInputWhenRequested()
    {
        byte[] source = Bytes("v=0\n"
            + "o=-\t3890123412  3890123412 IN IP4 192.0.2.1\n"
            + "s=-\n"
            + "c=IN IP4 192.0.2.1\n"
            + "t=0 0\n");
        SipSdpCodec codec = new SipSdpCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;
        SipParseResult parsed = codec.ParseSessionDescription(source, compatible);
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult written = codec.FormatSessionDescription(parsed.SessionDescription!, preserve);
        SipParseResult strict = codec.ParseSessionDescription(source, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.ValidationIssues, Is.Not.Empty);
            Assert.That(parsed.ValidationIssues.All(issue => issue.Severity == SipValidationSeverity.Warning),
                Is.True);
            Assert.That(parsed.SessionDescription!.Origin,
                Is.EqualTo("- 3890123412 3890123412 IN IP4 192.0.2.1"));
            Assert.That(written.Success, Is.True);
            Assert.That(written.Bytes, Is.EqualTo(source));
            Assert.That(written.Bytes, Is.Not.SameAs(source));
            Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        });
    }

    [TestCase("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=IN IP4 224.0.0.1\r\nt=0 0\r\n")]
    [TestCase("v=0\r\no=- 1 1 IN IP6 2001:db8::1\r\ns=-\r\nc=IN IP6 ff00::1/10/2\r\nt=0 0\r\n")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=IN IP4 192.0.2.1\r\nb=AS:-1\r\nt=0 0\r\n")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=IN IP4 192.0.2.1\r\nt=123 0\r\n")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=IN IP4 192.0.2.1\r\nt=0 0\r\na=mid:\r\n")]
    [TestCase("v=0\r\no=- 1 1 IN IP4 192.0.2.1\r\ns=-\r\nc=IN IP4 192.0.2.1\r\nt=0 0\r\nm=audio 70000 RTP/AVP 0\r\n")]
    public void RejectsMalformedAddressTimingBandwidthAttributeAndMediaFields(string text)
    {
        SipParseResult result = Parse(text);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void RejectsAParsedDescriptionWithoutTheRequiredVersionLine()
    {
        string text = "o=- 3890123412 3890123412 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n";

        SipParseResult result = Parse(text);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ValidationIssues.Select(issue => issue.Code),
            Does.Contain("sdp-version-required"));
    }

    [Test]
    public void EnforcesBodyLineAndCollectionLimits()
    {
        string text = "v=0\r\n"
            + "o=- 3890123412 3890123412 IN IP4 192.0.2.1\r\n"
            + "s=-\r\n"
            + "c=IN IP4 192.0.2.1\r\n"
            + "t=0 0\r\n"
            + "a=sendrecv\r\n"
            + "a=rtcp-mux\r\n";
        SipSdpCodec codec = new SipSdpCodec();
        SipParseOptions bodyOptions = new SipParseOptions();
        bodyOptions.MaxBodyBytes = 5;
        SipParseOptions lineOptions = new SipParseOptions();
        lineOptions.MaxHeaderLineBytes = 8;
        SipParseOptions collectionOptions = new SipParseOptions();
        collectionOptions.MaxListValueCount = 1;
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.MaxHeaderLineBytes = 4;

        SipParseResult body = codec.ParseSessionDescription(Bytes(text), bodyOptions);
        SipParseResult line = codec.ParseSessionDescription(Bytes(text), lineOptions);
        SipParseResult collection = codec.ParseSessionDescription(Bytes(text), collectionOptions);
        SipWriteResult write = codec.FormatSessionDescription(MinimalDescription(), writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(body.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(body.ErrorMessage, Does.Contain("maximum size"));
            Assert.That(line.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(line.ErrorMessage, Does.Contain("line"));
            Assert.That(collection.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(collection.ErrorMessage, Does.Contain("collection limit"));
            Assert.That(write.Success, Is.False);
            Assert.That(write.ErrorMessage, Does.Contain("line"));
        });
    }

    [Test]
    public void ValidationRejectsMissingConnectionsAndInvalidMediaModels()
    {
        SipSessionDescription description = MinimalDescription();
        description.Connection = null;
        SipMediaDescription media = new SipMediaDescription();
        media.Media = "audio";
        media.Port = -1;
        media.Protocol = "RTP//AVP";
        media.Formats = new string[0];
        description.MediaDescriptions = new[] { media };

        SipValidationIssue[] issues = new SipSdpCodec().ValidateSessionDescription(description);
        SipWriteResult written = new SipSdpCodec().FormatSessionDescription(description,
            new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(issues.Select(issue => issue.Code), Does.Contain("sdp-connection-required"));
            Assert.That(issues.Select(issue => issue.Code), Does.Contain("sdp-media-port"));
            Assert.That(issues.Select(issue => issue.Code), Does.Contain("sdp-media-protocol"));
            Assert.That(issues.Select(issue => issue.Code), Does.Contain("sdp-media-formats"));
            Assert.That(written.Success, Is.False);
        });
    }

    static SipSessionDescription MinimalDescription()
    {
        SipSessionDescription description = new SipSessionDescription();
        description.ProtocolVersion = 0;
        description.Origin = "- 3890123412 3890123412 IN IP4 192.0.2.1";
        description.SessionName = "-";
        description.Connection = "IN IP4 192.0.2.1";
        description.Timings = new[] { "0 0" };
        return description;
    }

    static SipSdpAttribute Attribute(string name, string? value, bool hasValue)
    {
        SipSdpAttribute attribute = new SipSdpAttribute();
        attribute.Name = name;
        attribute.Value = value;
        attribute.HasValue = hasValue;
        return attribute;
    }

    static SipParseResult Parse(string text)
    {
        return new SipSdpCodec().ParseSessionDescription(Bytes(text), new SipParseOptions());
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }
}
