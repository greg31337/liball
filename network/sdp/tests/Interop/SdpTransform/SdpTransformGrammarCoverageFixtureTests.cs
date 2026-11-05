namespace sdp.Tests.Interop.SdpTransform;

public class SdpTransformGrammarCoverageFixtureTests
{
    [Test]
    public void GrammarFixture_ProjectsEverySupportedFieldAndAttributeShape()
    {
        string input = string.Join("\r\n", new[]
        {
            "v=0",
            "o=- 20518 7 IN IP4 203.0.113.1",
            "s=sdp-transform grammar fixture",
            "i=session description",
            "u=https://example.test/session",
            "e=alice@example.test",
            "p=+1 555 0100",
            "c=IN IP4 203.0.113.1",
            "b=CT:1000",
            "b=AS:512",
            "b=TIAS:64000",
            "b=RR:2500",
            "b=RS:2500",
            "t=2873397496 2873404696",
            "r=7d 1h 0 25h",
            "z=2882844526 -1h 2898848070 0",
            "k=clear:session-key",
            "a=group:BUNDLE audio video data",
            "a=msid-semantic: WMS stream",
            "a=ice-lite",
            "a=ice-ufrag:F7gI",
            "a=ice-pwd:x9cml/YzichV2+XlhiMu8g",
            "a=ice-options:trickle renomination",
            "a=fingerprint:SHA-1 00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33",
            "a=source-filter: incl IN IP4 203.0.113.1 192.0.2.10",
            "a=ts-refclk:ptp=IEEE1588-2008:00-50-C2-FF-FE-90-04-37:0",
            "a=x-session-extension:value with spaces",
            "m=audio 54400 RTP/SAVPF 0 111",
            "i=audio description",
            "c=IN IP4 203.0.113.2",
            "b=AS:128",
            "k=prompt",
            "a=rtpmap:0 PCMU/8000",
            "a=rtpmap:111 opus/48000/2",
            "a=fmtp:111 minptime=10;useinbandfec=1",
            "a=control:streamid=0",
            "a=rtcp:65179",
            "a=rtcp:65180 IN IP4 193.84.77.194",
            "a=rtcp-fb:* trr-int 100",
            "a=rtcp-fb:* nack",
            "a=rtcp-fb:111 nack rpsi",
            "a=extmap:1 urn:ietf:params:rtp-hdrext:toffset",
            "a=extmap:2/recvonly URI-gps-string",
            "a=extmap:3 urn:ietf:params:rtp-hdrext:encrypt urn:ietf:params:rtp-hdrext:smpte-tc 25@600/24",
            "a=extmap-allow-mixed",
            "a=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:PS1uQCVeeCFCanVmcjkpPywjNWhcYD0mXXtxaVBR|2^20|1:32",
            "a=crypto:2 AES_CM_128_HMAC_SHA1_32 inline:dGVzdA== KDR=1",
            "a=setup:actpass",
            "a=connection:new",
            "a=mid:audio",
            "a=msid:stream",
            "a=msid:stream audio-track",
            "a=ptime:20",
            "a=maxptime:60",
            "a=sendrecv",
            "a=candidate:0 1 UDP 2113667327 203.0.113.1 54400 typ host",
            "a=candidate:1 2 UDP 1845501695 193.84.77.194 60017 typ srflx raddr 192.168.34.75 rport 60017 generation 0 network-id 3 network-cost 10",
            "a=candidate:2 1 TCP 1518280447 192.168.150.19 9 typ host tcptype active generation 0 network-id 4 network-cost 20",
            "a=end-of-candidates",
            "a=remote-candidates:1 203.0.113.1 54400 2 203.0.113.1 54401",
            "a=rtcp-mux",
            "a=rtcp-rsize",
            "a=x-google-flag:conference",
            "a=x-media-property",
            "a=x-media-value:opaque value",
            "m=video 55400 RTP/SAVPF 97 98",
            "a=mid:video",
            "a=rtpmap:97 H264/90000",
            "a=fmtp:97 profile-level-id=4d0028;packetization-mode=1",
            "a=rtpmap:98 VP8/90000",
            "a=ssrc:2566107569 cname:t9YU8M1UxTF8Y1A1",
            "a=ssrc:2566107569 mslabel",
            "a=ssrc-group:FEC-FR 2566107569 2566107570",
            "a=rid:1 send",
            "a=rid:2 recv max-width=1280;max-height=720;max-fps=30;depend=1",
            "a=imageattr:97 send [x=800,y=640,sar=1.1,q=0.6] [x=480,y=320] recv [x=330,y=250]",
            "a=imageattr:* send [x=800,y=640] recv *",
            "a=imageattr:98 recv [x=320,y=240]",
            "a=simulcast:send 1,2,3;~4,~5 recv 6;~7,~8",
            "a=simulcast: send rid=5;6;7 paused=6,7",
            "a=framerate:29.97",
            "a=label:1",
            "a=content:main",
            "a=keywds:conference video",
            "m=application 9 UDP/DTLS/SCTP webrtc-datachannel",
            "a=mid:data",
            "a=bundle-only",
            "a=sctpmap:5000 webrtc-datachannel",
            "a=sctpmap:5001 webrtc-datachannel 1024",
            "a=sctp-port:5000",
            "a=max-message-size:100000",
            "a=mediaclk:id=clk direct=963214424 rate=1000/1001",
            "a=floorctrl:c-s",
            "a=confid:1",
            "a=userid:2",
            "a=floorid:3 mstrm:1",
            ""
        });

        SdpParseResult result = new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description!;
        List<SdpAttribute> attributes = AllAttributes(description);
        List<string> expectedAttributeText = input.Split("\r\n").Where(line => line.StartsWith("a=", StringComparison.Ordinal)).Select(line => line.Substring(2)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.ProtocolVersion, Is.Zero);
            Assert.That(description.Origin!.SessionId, Is.EqualTo("20518"));
            Assert.That(description.Origin.SessionVersion, Is.EqualTo("7"));
            Assert.That(description.SessionName, Is.EqualTo("sdp-transform grammar fixture"));
            Assert.That(description.Information, Is.EqualTo("session description"));
            Assert.That(description.Uri, Is.EqualTo("https://example.test/session"));
            Assert.That(description.Emails, Is.EqualTo(new[] { "alice@example.test" }));
            Assert.That(description.Phones, Is.EqualTo(new[] { "+1 555 0100" }));
            Assert.That(description.Connection!.Address, Is.EqualTo("203.0.113.1"));
            Assert.That(description.Bandwidths.Select(bandwidth => bandwidth.Type), Is.EqualTo(new[] { "CT", "AS", "TIAS", "RR", "RS" }));
            Assert.That(description.Timings.Single().RepeatTimes.Single().RawText, Is.EqualTo("7d 1h 0 25h"));
            Assert.That(description.TimeZoneAdjustments, Has.Count.EqualTo(2));
            Assert.That(description.EncryptionKey!.Method, Is.EqualTo("clear"));
            Assert.That(description.MediaDescriptions.Select(media => media.MediaType), Is.EqualTo(new[] { "audio", "video", "application" }));
            Assert.That(description.MediaDescriptions[0].Information, Is.EqualTo("audio description"));
            Assert.That(description.MediaDescriptions[0].Connections.Single().Address, Is.EqualTo("203.0.113.2"));
            Assert.That(description.MediaDescriptions[0].EncryptionKey!.Method, Is.EqualTo("prompt"));
            Assert.That(attributes.Select(attribute => attribute.RawText), Is.EqualTo(expectedAttributeText));
        });

        AssertTypedAndOpaqueAttributeBehavior(attributes);
    }

    static void AssertTypedAndOpaqueAttributeBehavior(List<SdpAttribute> attributes)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        List<SdpAttributeDecodeResult> decoded = attributes.Select(codec.DecodeAttribute).ToList();
        List<string> opaqueNames = new List<string> { "x-session-extension", "x-google-flag", "x-media-property", "x-media-value" };

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Where(result => opaqueNames.Contains(result.SourceAttribute!.Name)).All(result => result.Success && !result.Recognized), Is.True);
            Assert.That(decoded.Single(result => result.SourceAttribute!.RawText == "rtpmap:111 opus/48000/2").RtpMapAttribute!.ChannelCount, Is.EqualTo(2));
            Assert.That(decoded.Single(result => result.SourceAttribute!.RawText.StartsWith("candidate:1", StringComparison.Ordinal)).IceCandidate!.RelatedPort, Is.EqualTo(60017));
            Assert.That(decoded.Single(result => result.SourceAttribute!.RawText.StartsWith("candidate:2", StringComparison.Ordinal)).IceCandidate!.TcpType, Is.EqualTo(SdpIceTcpType.Active));
            Assert.That(decoded.Single(result => result.SourceAttribute!.RawText.StartsWith("extmap:3", StringComparison.Ordinal)).ExtMapAttribute!.Encrypted, Is.True);
            Assert.That(decoded.Single(result => result.SourceAttribute!.RawText.StartsWith("simulcast: send", StringComparison.Ordinal)).SimulcastAttribute!.LegacySyntax, Is.True);
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.MediaClock).MediaClockAttribute!.RateDenominator, Is.EqualTo(1001));
            Assert.That(decoded.Single(result => result.SourceAttribute!.Name == SdpAttributeNames.FloorId).SourceAttribute!.Value, Is.EqualTo("3 mstrm:1"));
        });
    }

    static List<SdpAttribute> AllAttributes(SdpSessionDescription description)
    {
        List<SdpAttribute> result = new List<SdpAttribute>();
        result.AddRange(description.Attributes);
        foreach (SdpMediaDescription media in description.MediaDescriptions)
        {
            result.AddRange(media.Attributes);
        }
        return result;
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
