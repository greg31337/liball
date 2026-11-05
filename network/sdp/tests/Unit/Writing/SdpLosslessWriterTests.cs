namespace sdp.Tests.Unit.Writing;

public class SdpLosslessWriterTests
{
    [Test]
    public void LosslessWriter_EmitsEveryRetainedLineAndEndingExactlyWhenUnchanged()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\n" +
            "s=Lossless  session\r" +
            "x=opaque future field\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=TOOL:  opaque  value \n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=RTPMAP:0 PCMU/8000\r\n" +
            "broken-line";
        SdpParseResult parsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription description = parsed.Description ?? new SdpSessionDescription();

        SdpWriteResult result = new SdpWriter().WriteText(description, new SdpWriteOptions { Mode = SdpWriteMode.Lossless });

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Description, Is.Not.Null);
            Assert.That(description.RawLines, Is.Not.Empty);
            Assert.That(description.MediaDescriptions[0].RawLines, Is.Not.Empty);
            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.Text, Is.EqualTo(input));
            Assert.That(result.CharactersWritten, Is.EqualTo(input.Length));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void LosslessWriter_ReplacesOnlyEditedLinesAndCanonicallyAppendsNewData()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Old  name\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=TOOL:old\r\n" +
            "m=audio 49170 RTP/AVP 0 96\r\n" +
            "c=IN IP4 192.0.2.2\r\n" +
            "a=RTPMAP:96 opus/48000/2\r\n" +
            "a=X-Vendor:untouched  opaque\r\n";
        SdpParseResult parsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription description = parsed.Description ?? new SdpSessionDescription();
        description.SessionName = "New name";
        description.Attributes[0].Value = "new";
        description.Attributes.Add(ValueAttribute("X-Added", "extension"));
        description.MediaDescriptions[0].Port = 50000;
        description.MediaDescriptions[0].Connections[0].Address = "192.0.2.20";
        description.MediaDescriptions[0].Attributes[0].Value = "96 OPUS/48000/2";
        SdpMediaDescription addedMedia = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Video,
            MediaKind = SdpMediaKind.Video,
            Port = 9,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        addedMedia.Formats.Add("31");
        description.MediaDescriptions.Add(addedMedia);

        SdpWriteResult result = new SdpWriter().WriteText(description, new SdpWriteOptions { Mode = SdpWriteMode.Lossless });

        string expected =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=New name\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "a=tool:new\r\n" +
            "a=X-Added:extension\r\n" +
            "m=audio 50000 RTP/AVP 0 96\r\n" +
            "c=IN IP4 192.0.2.20\r\n" +
            "a=rtpmap:96 OPUS/48000/2\r\n" +
            "a=X-Vendor:untouched  opaque\r\n" +
            "m=video 9 RTP/AVP 31\r\n";

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, ParseDiagnostics(parsed));
            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void LosslessWriter_HonorsUnknownAndObsoleteOmissionWithoutChangingOtherRawLines()
    {
        string input =
            "v=0\n" +
            "o=- 1 1 IN IP4 192.0.2.1\n" +
            "s=Preserved spacing\n" +
            "x=unknown-field\n" +
            "c=IN IP4 192.0.2.1\n" +
            "t=0 0\n" +
            "k=prompt\n" +
            "a=cat:legacy\n" +
            "a=X-Unknown:value\n" +
            "a=SENDRECV";
        SdpParseResult parsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription description = parsed.Description ?? new SdpSessionDescription();
        SdpWriteOptions options = new SdpWriteOptions
        {
            Mode = SdpWriteMode.Lossless,
            IncludeUnknownFields = false,
            IncludeObsoleteFields = false
        };

        SdpWriteResult result = new SdpWriter().WriteText(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Diagnostics(result));
            Assert.That(result.Text, Is.EqualTo(
                "v=0\n" +
                "o=- 1 1 IN IP4 192.0.2.1\n" +
                "s=Preserved spacing\n" +
                "c=IN IP4 192.0.2.1\n" +
                "t=0 0\n" +
                "a=SENDRECV"));
        });
    }

    static SdpParseOptions LosslessParseOptions()
    {
        return new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true
        };
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

    static string Diagnostics(SdpWriteResult result)
    {
        string text = string.Empty;
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            text = text + result.Diagnostics[index].Code + ": " + result.Diagnostics[index].Detail + " | ";
            index = index + 1;
        }
        return text;
    }

    static string ParseDiagnostics(SdpParseResult result)
    {
        string text = string.Empty;
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            text = text + result.Diagnostics[index].Code + ": " + result.Diagnostics[index].Detail + " | ";
            index = index + 1;
        }
        return text;
    }
}
