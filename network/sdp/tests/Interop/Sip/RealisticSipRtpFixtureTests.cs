namespace sdp.Tests.Interop.Sip;

public class RealisticSipRtpFixtureTests
{
    [Test]
    public void Ipv6UnicastOffer_ProjectsAudioVideoAndRealtimeText()
    {
        string input = string.Join("\r\n", new[]
        {
            "v=0",
            "o=alice 2890844526 2890844527 IN IP6 2001:db8:1::10",
            "s=SIP multimedia call",
            "c=IN IP6 2001:db8:1::20",
            "t=0 0",
            "a=sendrecv",
            "a=x-cisco-service:telepresence",
            "m=audio 49170 RTP/AVP 0 8 101",
            "b=AS:80",
            "a=rtpmap:0 PCMU/8000",
            "a=rtpmap:8 PCMA/8000",
            "a=rtpmap:101 telephone-event/8000",
            "a=fmtp:101 0-16",
            "a=ptime:20",
            "m=video 51372 RTP/AVP 99",
            "b=AS:1024",
            "a=rtpmap:99 H264/90000",
            "a=fmtp:99 profile-level-id=42e01f;packetization-mode=1",
            "a=rtcp-fb:99 nack pli",
            "m=text 6000 RTP/AVP 98 99",
            "b=AS:20",
            "a=rtpmap:98 t140/1000",
            "a=rtpmap:99 red/1000",
            "a=fmtp:99 98/98/98",
            "a=x-realtime-text:enabled",
            ""
        });

        SdpParseResult result = new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description!;
        SdpMediaDescription text = description.MediaDescriptions[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Origin!.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(description.Connection!.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(description.Connection.Address, Is.EqualTo("2001:db8:1::20"));
            Assert.That(description.MediaDescriptions.Select(media => media.MediaKind), Is.EqualTo(new[] { SdpMediaKind.Audio, SdpMediaKind.Video, SdpMediaKind.Text }));
            Assert.That(description.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "8", "101" }));
            Assert.That(description.MediaDescriptions[1].Bandwidths.Single().RawText, Is.EqualTo("AS:1024"));
            Assert.That(text.Attributes.Single(attribute => attribute.Name == "x-realtime-text").RawText, Is.EqualTo("x-realtime-text:enabled"));
            Assert.That(description.Attributes.Single(attribute => attribute.Name == "x-cisco-service").Value, Is.EqualTo("telepresence"));
        });
    }

    [Test]
    public void Ipv4MulticastRtpAnnouncement_ProjectsTtlAddressCountAndPortCount()
    {
        string input = string.Join("\r\n", new[]
        {
            "v=0",
            "o=broadcaster 1700000000 1700000001 IN IP4 192.0.2.50",
            "s=Multicast program",
            "i=Two-channel live distribution",
            "c=IN IP4 233.252.0.1/32/2",
            "b=CT:6000",
            "t=0 0",
            "a=recvonly",
            "a=x-network-name:production",
            "m=video 5004/2 RTP/AVP 33",
            "b=AS:5000",
            "a=rtpmap:33 MP2T/90000",
            "m=audio 5006/2 RTP/AVP 14",
            "b=AS:384",
            "a=rtpmap:14 MPA/90000",
            ""
        });

        SdpParseResult result = new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description!;
        SdpConnectionData connection = description.Connection!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(connection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4Multicast));
            Assert.That(connection.Address, Is.EqualTo("233.252.0.1"));
            Assert.That(connection.HasTimeToLive, Is.True);
            Assert.That(connection.TimeToLive, Is.EqualTo(32));
            Assert.That(connection.HasAddressCount, Is.True);
            Assert.That(connection.AddressCount, Is.EqualTo(2));
            Assert.That(description.MediaDescriptions.All(media => media.HasPortCount && media.PortCount == 2), Is.True);
            Assert.That(description.Attributes.Single(attribute => attribute.Name == "x-network-name").Value, Is.EqualTo("production"));
        });
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
