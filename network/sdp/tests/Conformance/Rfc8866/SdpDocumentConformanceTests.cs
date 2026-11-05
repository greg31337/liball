namespace sdp.Tests.Conformance.Rfc8866;

public class SdpDocumentConformanceTests
{
    [Test]
    public void MinimalSessionDescription_IsAccepted()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 0 0 IN IP4 127.0.0.1\r\n" +
            "s=-\r\n" +
            "t=0 0";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.HasProtocolVersion, Is.True);
            Assert.That(result.Description?.Origin?.Username, Is.EqualTo("-"));
            Assert.That(result.Description?.HasSessionName, Is.True);
            Assert.That(result.Description?.SessionName, Is.EqualTo("-"));
            Assert.That(result.Description?.Timings, Has.Count.EqualTo(1));
            Assert.That(result.Description?.Timings[0].StartTime, Is.EqualTo("0"));
            Assert.That(result.Description?.Timings[0].StopTime, Is.EqualTo("0"));
        });
    }

    [Test]
    public void PublishedRfc8866Example_IsAcceptedWithoutDiagnostics()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=jdoe 3724394400 3724394405 IN IP4 198.51.100.1\r\n" +
            "s=Call to John Smith\r\n" +
            "i=SDP Offer #1\r\n" +
            "u=http://www.jdoe.example.com/home.html\r\n" +
            "e=Jane Doe <jane@jdoe.example.com>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "c=IN IP4 198.51.100.1\r\n" +
            "t=0 0\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "m=audio 49180 RTP/AVP 0\r\n" +
            "m=video 51372 RTP/AVP 99\r\n" +
            "c=IN IP6 2001:db8::2\r\n" +
            "a=rtpmap:99 h263-1998/90000";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.SessionName, Is.EqualTo("Call to John Smith"));
            Assert.That(description.Connection?.Address, Is.EqualTo("198.51.100.1"));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(3));
            Assert.That(description.MediaDescriptions[2].Connections[0].AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(description.MediaDescriptions[2].Attributes[0].Name, Is.EqualTo("rtpmap"));
        });
    }

    [Test]
    public void FullCoreGrammarWithEveryOptionalAndRepeatedField_IsAccepted()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=user 18446744073709551615 0002 X-NET X-ADDR opaque-host\r\n" +
            "s=Full core grammar\r\n" +
            "i=International résumé\r\n" +
            "u=../sessions/current?view=full#media\r\n" +
            "e=first@example.test\r\n" +
            "e=Second Contact <second@example.test>\r\n" +
            "p=+1 617 555-6011\r\n" +
            "p=Support <+44 20 7946 0958>\r\n" +
            "c=X-NET X-ADDR opaque:value\r\n" +
            "b=AS:00128\r\n" +
            "b=X-CUSTOM:0\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0 25h\r\n" +
            "r=3600 60m 0\r\n" +
            "z=2882844526 -1h 2898848070 0\r\n" +
            "t=3724394400 3730536000\r\n" +
            "k=base64:YQ==\r\n" +
            "a=recvonly\r\n" +
            "a=x-empty:\r\n" +
            "m=audio 49170/2 RTP/AVP 0 96\r\n" +
            "i=Primary audio\r\n" +
            "c=IN IP4 224.2.17.12/127/3\r\n" +
            "c=IN IP6 ff15::101/2\r\n" +
            "b=TIAS:64000\r\n" +
            "k=prompt\r\n" +
            "a=sendrecv\r\n" +
            "m=application 9 UDP/DTLS/SCTP webrtc-datachannel";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Emails, Has.Count.EqualTo(2));
            Assert.That(description.Phones, Has.Count.EqualTo(2));
            Assert.That(description.Bandwidths, Has.Count.EqualTo(2));
            Assert.That(description.Timings, Has.Count.EqualTo(2));
            Assert.That(description.Timings[0].RepeatTimes, Has.Count.EqualTo(2));
            Assert.That(description.TimeZoneAdjustments, Has.Count.EqualTo(2));
            Assert.That(description.Attributes, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Connections, Has.Count.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Bandwidths, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[1].Protocol, Is.EqualTo("UDP/DTLS/SCTP"));
        });
    }

    [Test]
    public void PermanentMultipleAndRepeatingSessions_PreserveEveryTimingBlock()
    {
        ISdpParser parser = new SdpParser();
        string input =
            "v=0\r\n" +
            "o=- 3724394400 1 IN IP4 198.51.100.1\r\n" +
            "s=Timing forms\r\n" +
            "t=0 0\r\n" +
            "t=3724394400 3730536000\r\n" +
            "r=7d 1h 0 25h\r\n" +
            "z=3730928400 -1h 3749680800 0\r\n" +
            "t=184467440737095516160 184467440737095516161";

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions());
        SdpSessionDescription description = result.Description ?? new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Timings, Has.Count.EqualTo(3));
            Assert.That(description.Timings[0].StartTime, Is.EqualTo("0"));
            Assert.That(description.Timings[1].RepeatTimes, Has.Count.EqualTo(1));
            Assert.That(description.TimeZoneAdjustments, Has.Count.EqualTo(2));
            Assert.That(description.Timings[2].StartTime, Is.EqualTo("184467440737095516160"));
        });
    }
}
