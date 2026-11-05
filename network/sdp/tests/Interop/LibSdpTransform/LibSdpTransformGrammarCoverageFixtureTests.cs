namespace sdp.Tests.Interop.LibSdpTransform;

public class LibSdpTransformGrammarCoverageFixtureTests
{
    [Test]
    public void GrammarFixture_AcceptsBareLfAndProjectsEveryCppGrammarShape()
    {
        string input = string.Join("\n", new[]
        {
            "v=0",
            "o=- 20518 0 IN IP4 203.0.113.1",
            "s=libsdptransform grammar fixture",
            "i=session description",
            "u=https://example.test/session",
            "e=alice@example.test",
            "p=+1 555 0100",
            "c=IN IP4 224.2.1.1/127/3",
            "b=CT:1000",
            "b=AS:512",
            "b=TIAS:64000",
            "b=RR:2500",
            "b=RS:2500",
            "t=2873397496 2873404696",
            "r=7d 1h 0 25h",
            "z=2882844526 -1h 2898848070 0",
            "a=group:BUNDLE audio video",
            "a=msid-semantic: WMS stream",
            "a=ice-lite",
            "a=ice-ufrag:F7gI",
            "a=ice-pwd:x9cml/YzichV2+XlhiMu8g",
            "a=ice-options:trickle",
            "a=fingerprint:SHA-1 00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33",
            "a=source-filter: incl IN IP4 224.2.1.1 192.0.2.10",
            "a=ts-refclk:ptp=IEEE1588-2008:00-50-C2-FF-FE-90-04-37:0",
            "m=audio 54400/2 RTP/SAVPF 0 96",
            "i=audio description",
            "c=IN IP4 239.5.2.31/63",
            "b=AS:128",
            "a=rtpmap:0 PCMU/8000",
            "a=rtpmap:96 opus/48000/2",
            "a=fmtp:96 minptime=10; useinbandfec=1",
            "a=control:streamid=0",
            "a=rtcp:65179",
            "a=rtcp:65180 IN IP4 193.84.77.194",
            "a=rtcp-fb:* trr-int 100",
            "a=rtcp-fb:* nack",
            "a=rtcp-fb:96 nack rpsi",
            "a=extmap:1 urn:ietf:params:rtp-hdrext:toffset",
            "a=extmap:2/recvonly URI-gps-string",
            "a=extmap:3 urn:ietf:params:rtp-hdrext:encrypt urn:ietf:params:rtp-hdrext:smpte-tc 25@600/24",
            "a=extmap-allow-mixed",
            "a=crypto:1 AES_CM_128_HMAC_SHA1_80 inline:PS1uQCVeeCFCanVmcjkpPywjNWhcYD0mXXtxaVBR|2^20|1:32",
            "a=setup:actpass",
            "a=mid:audio",
            "a=msid:stream audio-track",
            "a=ptime:20",
            "a=maxptime:60",
            "a=sendrecv",
            "a=candidate:0 1 UDP 2113667327 203.0.113.1 54400 typ host",
            "a=candidate:1 2 UDP 1845501695 193.84.77.194 60017 typ srflx raddr 192.168.34.75 rport 60017 generation 0 network-id 3 network-cost 10",
            "a=candidate:2 1 TCP 1518280447 192.168.150.19 9 typ host tcptype active generation 0 network-id 4 network-cost 20",
            "a=end-of-candidates",
            "a=remote-candidates:1 203.0.113.1 54400 2 203.0.113.1 54401",
            "a=ssrc:2566107569 cname:t9YU8M1UxTF8Y1A1",
            "a=ssrc:2566107569 mslabel",
            "a=ssrc-group:FEC-FR 2566107569 2566107570",
            "a=rtcp-mux",
            "a=rtcp-rsize",
            "a=sctpmap:5000 webrtc-datachannel 1024",
            "a=x-google-flag:conference",
            "a=rid:1 send",
            "a=rid:2 recv max-width=1280;max-height=720;max-fps=30;depend=1",
            "a=imageattr:97 send [x=800,y=640,sar=1.1,q=0.6] [x=480,y=320] recv [x=330,y=250]",
            "a=imageattr:* send [x=800,y=640] recv *",
            "a=imageattr:98 recv [x=320,y=240]",
            "a=simulcast:send 1,2,3;~4,~5 recv 6;~7,~8",
            "a=simulcast: send rid=5;6;7 paused=6,7",
            "a=framerate:29.97",
            "a=mediaclk:direct=0",
            "a=x-cpp-unknown:opaque value",
            "m=video 55400 RTP/SAVPF 97 98",
            "a=mid:video",
            "a=rtpmap:97 H264/90000",
            "a=fmtp:97 profile-level-id=4d0028;packetization-mode=1",
            "a=rtpmap:98 VP8/90000",
            "a=recvonly",
            ""
        });
        ISdpParser parser = new SdpParser();

        SdpParseResult rejected = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Strict });
        SdpParseResult accepted = parser.ParseText(input, new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true
        });
        SdpSessionDescription description = accepted.Description!;
        List<SdpAttribute> attributes = AllAttributes(description);
        List<string> expectedAttributeText = input.Split('\n').Where(line => line.StartsWith("a=", StringComparison.Ordinal)).Select(line => line.Substring(2)).ToList();
        SdpAttributeDecodeResult unknown = new SdpAttributeCodec().DecodeAttribute(attributes.Single(attribute => attribute.Name == "x-cpp-unknown"));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostics.Any(diagnostic => diagnostic.Code == SdpDiagnosticCodes.ParseInvalidLineEnding), Is.True);
            Assert.That(accepted.Success, Is.True, DiagnosticText(accepted));
            Assert.That(accepted.Diagnostics, Is.Empty);
            Assert.That(description.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(description.RawLines.All(line => line.LineEnding == SdpLineEnding.Lf), Is.True);
            Assert.That(description.MediaDescriptions.SelectMany(media => media.RawLines).All(line => line.LineEnding == SdpLineEnding.Lf), Is.True);
            Assert.That(description.Connection!.Address, Is.EqualTo("224.2.1.1"));
            Assert.That(description.Connection.HasTimeToLive, Is.True);
            Assert.That(description.Connection.TimeToLive, Is.EqualTo(127));
            Assert.That(description.Connection.HasAddressCount, Is.True);
            Assert.That(description.Connection.AddressCount, Is.EqualTo(3));
            Assert.That(description.MediaDescriptions[0].HasPortCount, Is.True);
            Assert.That(description.MediaDescriptions[0].PortCount, Is.EqualTo(2));
            Assert.That(description.MediaDescriptions[0].Connections.Single().TimeToLive, Is.EqualTo(63));
            Assert.That(attributes.Select(attribute => attribute.RawText), Is.EqualTo(expectedAttributeText));
            Assert.That(unknown.Success, Is.True);
            Assert.That(unknown.Recognized, Is.False);
            Assert.That(unknown.SourceAttribute!.Value, Is.EqualTo("opaque value"));
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
