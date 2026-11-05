using System.Text;

namespace sdp.Tests.Interop.GStreamer;

public class GStreamerOperationFixtureTests
{
    [Test]
    public void CoreMessageAndMediaFixture_ProjectsEveryCollectionInSourceOrder()
    {
        SdpParseResult parsed = new SdpParser().ParseText(GStreamerDocument(), new SdpParseOptions());
        SdpFixtureRoundTripAssert.AssertCanonical(GStreamerDocument(), new SdpParseOptions());
        SdpSessionDescription description = parsed.Description!;
        SdpMediaDescription audio = description.MediaDescriptions[0];

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, ParseDiagnosticText(parsed));
            Assert.That(parsed.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new[]
            {
                "SDP_VALIDATION_OBSOLETE_SYNTAX",
                "SDP_VALIDATION_OBSOLETE_SYNTAX"
            }));
            Assert.That(description.ProtocolVersion, Is.Zero);
            Assert.That(description.Origin!.Username, Is.EqualTo("gstreamer"));
            Assert.That(description.Origin.SessionId, Is.EqualTo("3456"));
            Assert.That(description.Origin.SessionVersion, Is.EqualTo("7"));
            Assert.That(description.SessionName, Is.EqualTo("GStreamer pipeline"));
            Assert.That(description.Information, Is.EqualTo("session information"));
            Assert.That(description.Uri, Is.EqualTo("https://example.test/pipeline"));
            Assert.That(description.Emails, Is.EqualTo(new[] { "first@example.test", "second@example.test" }));
            Assert.That(description.Phones, Is.EqualTo(new[] { "+1 555 0100", "+1 555 0101" }));
            Assert.That(description.Connection!.RawAddress, Is.EqualTo("239.1.1.1/32/2"));
            Assert.That(description.Bandwidths.Select(bandwidth => bandwidth.RawText), Is.EqualTo(new[] { "CT:1000", "AS:512" }));
            Assert.That(description.Timings.Select(timing => timing.RawText), Is.EqualTo(new[] { "0 0", "2873397496 2873404696" }));
            Assert.That(description.Timings[0].RepeatTimes.Select(repeat => repeat.RawText), Is.EqualTo(new[] { "7d 1h 0 25h", "1h 10m 0" }));
            Assert.That(description.Timings[1].RepeatTimes.Single().RawText, Is.EqualTo("1d 2h 0 12h"));
            Assert.That(description.TimeZoneAdjustments.Select(zone => zone.AdjustmentTime), Is.EqualTo(new[] { "2882844526", "2898848070" }));
            Assert.That(description.EncryptionKey!.RawText, Is.EqualTo("clear:session-key"));
            Assert.That(description.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "tool:GStreamer", "x-session:one", "x-session:two" }));
            Assert.That(description.MediaDescriptions.Select(media => media.MediaType), Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(audio.HasPortCount, Is.True);
            Assert.That(audio.PortCount, Is.EqualTo(2));
            Assert.That(audio.Formats, Is.EqualTo(new[] { "0", "96" }));
            Assert.That(audio.Information, Is.EqualTo("audio information"));
            Assert.That(audio.Connections.Select(connection => connection.RawAddress), Is.EqualTo(new[] { "239.1.1.2/32", "239.1.1.3/31" }));
            Assert.That(audio.Bandwidths.Select(bandwidth => bandwidth.RawText), Is.EqualTo(new[] { "AS:128", "TIAS:64000" }));
            Assert.That(audio.EncryptionKey!.RawText, Is.EqualTo("prompt"));
            Assert.That(audio.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "rtpmap:0 PCMU/8000", "rtpmap:96 opus/48000/2", "x-media:first", "x-media:second" }));
        });
    }

    [Test]
    public void OrderedMutationFixture_InsertReplaceRemoveAndAddDriveCanonicalOrder()
    {
        SdpSessionDescription description = Parse(GStreamerDocument());
        SdpMediaDescription audio = description.MediaDescriptions[0];
        ISdpEditor editor = new SdpEditor();

        description.Emails.Insert(1, "inserted@example.test");
        description.Emails[0] = "replacement@example.test";
        description.Emails.RemoveAt(2);
        description.Emails.Add("tail@example.test");

        SdpBandwidth movedSessionBandwidth = description.Bandwidths[0];
        description.Bandwidths.RemoveAt(0);
        description.Bandwidths.Add(movedSessionBandwidth);
        SdpTiming movedTiming = description.Timings[0];
        description.Timings.RemoveAt(0);
        description.Timings.Add(movedTiming);
        SdpTimeZoneAdjustment movedZone = description.TimeZoneAdjustments[0];
        description.TimeZoneAdjustments.RemoveAt(0);
        description.TimeZoneAdjustments.Add(movedZone);

        editor.InsertSessionAttribute(description, 1, ValueAttribute("x-session", "inserted"));
        editor.ReplaceSessionAttribute(description, 0, ValueAttribute(SdpAttributeNames.Tool, "replacement-tool"));
        editor.RemoveSessionAttributeAt(description, 2);
        editor.AddSessionAttribute(description, ValueAttribute("x-session", "tail"));

        audio.Formats.Insert(1, "8");
        audio.Formats[0] = "18";
        audio.Formats.RemoveAt(2);
        audio.Formats.Add("101");
        SdpConnectionData movedConnection = audio.Connections[0];
        audio.Connections.RemoveAt(0);
        audio.Connections.Add(movedConnection);
        SdpBandwidth movedMediaBandwidth = audio.Bandwidths[0];
        audio.Bandwidths.RemoveAt(0);
        audio.Bandwidths.Add(movedMediaBandwidth);

        editor.InsertMediaAttribute(description, 0, 1, ValueAttribute("x-media", "inserted"));
        editor.ReplaceMediaAttribute(description, 0, 0, ValueAttribute(SdpAttributeNames.RtpMap, "18 G729/8000"));
        editor.RemoveMediaAttributeAt(description, 0, 2);
        editor.AddMediaAttribute(description, 0, ValueAttribute(SdpAttributeNames.RtpMap, "101 telephone-event/8000"));

        SdpWriteResult written = new SdpWriter().WriteText(description, new SdpWriteOptions());
        List<string> lines = written.Text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True, WriteDiagnosticText(written));
            Assert.That(description.Emails, Is.EqualTo(new[] { "replacement@example.test", "inserted@example.test", "tail@example.test" }));
            Assert.That(description.Bandwidths.Select(bandwidth => bandwidth.Type), Is.EqualTo(new[] { "AS", "CT" }));
            Assert.That(description.Timings.Select(timing => timing.StartTime), Is.EqualTo(new[] { "2873397496", "0" }));
            Assert.That(description.TimeZoneAdjustments.Select(zone => zone.AdjustmentTime), Is.EqualTo(new[] { "2898848070", "2882844526" }));
            Assert.That(description.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "tool:replacement-tool", "x-session:inserted", "x-session:two", "x-session:tail" }));
            Assert.That(audio.Formats, Is.EqualTo(new[] { "18", "8", "101" }));
            Assert.That(audio.Connections.Select(connection => connection.Address), Is.EqualTo(new[] { "239.1.1.3", "239.1.1.2" }));
            Assert.That(audio.Bandwidths.Select(bandwidth => bandwidth.Type), Is.EqualTo(new[] { "TIAS", "AS" }));
            Assert.That(audio.Attributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "rtpmap:18 G729/8000", "x-media:inserted", "x-media:first", "x-media:second", "rtpmap:101 telephone-event/8000" }));
            Assert.That(lines.IndexOf("e=replacement@example.test"), Is.LessThan(lines.IndexOf("e=inserted@example.test")));
            Assert.That(lines.IndexOf("b=AS:512"), Is.LessThan(lines.IndexOf("b=CT:1000")));
            Assert.That(lines.IndexOf("t=2873397496 2873404696"), Is.LessThan(lines.IndexOf("t=0 0")));
            Assert.That(lines.IndexOf("c=IN IP4 239.1.1.3/31"), Is.LessThan(lines.IndexOf("c=IN IP4 239.1.1.2/32")));
            Assert.That(lines.IndexOf("a=x-media:inserted"), Is.LessThan(lines.IndexOf("a=x-media:first")));
        });
    }

    [Test]
    public void TextConversionFixture_RoundTripsStringUtf8AndCallerBuffer()
    {
        string text = GStreamerDocument();
        byte[] inputBytes = Encoding.UTF8.GetBytes(text);
        ISdpParser parser = new SdpParser();
        ISdpWriter writer = new SdpWriter();

        SdpParseResult fromText = parser.ParseText(text, new SdpParseOptions());
        SdpParseResult fromBytes = parser.ParseUtf8Bytes(inputBytes, new SdpParseOptions());
        SdpWriteResult asText = writer.WriteText(fromText.Description!, new SdpWriteOptions());
        SdpWriteResult asBytes = writer.WriteUtf8Bytes(fromBytes.Description!, new SdpWriteOptions());
        byte[] destination = new byte[asBytes.BytesWritten];
        SdpBufferWriteResult bufferResult = writer.WriteUtf8Buffer(fromText.Description!, destination, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Success, Is.True, ParseDiagnosticText(fromText));
            Assert.That(fromBytes.Success, Is.True, ParseDiagnosticText(fromBytes));
            Assert.That(asText.Success, Is.True, WriteDiagnosticText(asText));
            Assert.That(asText.Text, Is.EqualTo(text));
            Assert.That(asBytes.Success, Is.True, WriteDiagnosticText(asBytes));
            Assert.That(asBytes.Utf8Bytes, Is.EqualTo(inputBytes));
            Assert.That(bufferResult.Success, Is.True);
            Assert.That(bufferResult.BytesWritten, Is.EqualTo(inputBytes.Length));
            Assert.That(destination, Is.EqualTo(inputBytes));
        });
    }

    static string GStreamerDocument()
    {
        return "v=0\r\n"
            + "o=gstreamer 3456 7 IN IP4 192.0.2.20\r\n"
            + "s=GStreamer pipeline\r\n"
            + "i=session information\r\n"
            + "u=https://example.test/pipeline\r\n"
            + "e=first@example.test\r\n"
            + "e=second@example.test\r\n"
            + "p=+1 555 0100\r\n"
            + "p=+1 555 0101\r\n"
            + "c=IN IP4 239.1.1.1/32/2\r\n"
            + "b=CT:1000\r\n"
            + "b=AS:512\r\n"
            + "t=0 0\r\n"
            + "r=7d 1h 0 25h\r\n"
            + "r=1h 10m 0\r\n"
            + "t=2873397496 2873404696\r\n"
            + "r=1d 2h 0 12h\r\n"
            + "z=2882844526 -1h 2898848070 0\r\n"
            + "k=clear:session-key\r\n"
            + "a=tool:GStreamer\r\n"
            + "a=x-session:one\r\n"
            + "a=x-session:two\r\n"
            + "m=audio 5004/2 RTP/AVP 0 96\r\n"
            + "i=audio information\r\n"
            + "c=IN IP4 239.1.1.2/32\r\n"
            + "c=IN IP4 239.1.1.3/31\r\n"
            + "b=AS:128\r\n"
            + "b=TIAS:64000\r\n"
            + "k=prompt\r\n"
            + "a=rtpmap:0 PCMU/8000\r\n"
            + "a=rtpmap:96 opus/48000/2\r\n"
            + "a=x-media:first\r\n"
            + "a=x-media:second\r\n"
            + "m=video 5006 RTP/AVP 31\r\n"
            + "a=rtpmap:31 H261/90000\r\n";
    }

    static SdpSessionDescription Parse(string text)
    {
        SdpParseResult result = new SdpParser().ParseText(text, new SdpParseOptions());
        Assert.That(result.Success, Is.True, ParseDiagnosticText(result));
        return result.Description!;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }

    static string ParseDiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }

    static string WriteDiagnosticText(SdpWriteResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
