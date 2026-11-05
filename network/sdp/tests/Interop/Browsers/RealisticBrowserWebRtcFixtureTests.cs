namespace sdp.Tests.Interop.Browsers;

public class RealisticBrowserWebRtcFixtureTests
{
    [Test]
    public void UnifiedPlanOffer_ProjectsBundledAudioVideoAndDataSections()
    {
        string input = string.Join("\r\n", new[]
        {
            "v=0",
            "o=- 4962323933170359740 2 IN IP4 127.0.0.1",
            "s=-",
            "t=0 0",
            "a=group:BUNDLE 0 1 2",
            "a=extmap-allow-mixed",
            "a=msid-semantic: WMS browser-stream",
            "a=ice-options:trickle",
            "a=x-google-flag:conference",
            "m=audio 9 UDP/TLS/RTP/SAVPF 111 0 8",
            "c=IN IP4 0.0.0.0",
            "a=rtcp:9 IN IP4 0.0.0.0",
            "a=ice-ufrag:ufragA",
            "a=ice-pwd:passwordForAudioAndVideo1234",
            "a=fingerprint:sha-256 11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00",
            "a=setup:actpass",
            "a=mid:0",
            "a=sendrecv",
            "a=msid:browser-stream audio-track",
            "a=rtcp-mux",
            "a=rtcp-rsize",
            "a=rtpmap:111 opus/48000/2",
            "a=fmtp:111 minptime=10;useinbandfec=1",
            "a=rtcp-fb:111 transport-cc",
            "a=rtpmap:0 PCMU/8000",
            "a=rtpmap:8 PCMA/8000",
            "a=ssrc:1001 cname:browserCname",
            "a=candidate:1 1 UDP 2122260223 192.0.2.10 54400 typ host generation 0 network-id 1",
            "a=candidate:2 1 UDP 2122194687 2001:db8::10 54401 typ host generation 0 network-id 2",
            "m=video 9 UDP/TLS/RTP/SAVPF 96 97",
            "c=IN IP4 0.0.0.0",
            "a=mid:1",
            "a=sendrecv",
            "a=msid:browser-stream video-track",
            "a=rtcp-mux",
            "a=rtpmap:96 VP8/90000",
            "a=rtcp-fb:96 nack pli",
            "a=rtpmap:97 rtx/90000",
            "a=fmtp:97 apt=96",
            "a=rid:high send max-width=1280;max-height=720",
            "a=rid:low send max-width=640;max-height=360",
            "a=simulcast:send high;low",
            "a=ssrc-group:FID 2001 2002",
            "a=ssrc:2001 cname:browserCname",
            "a=ssrc:2002 cname:browserCname",
            "a=x-browser-video-mode:realtime",
            "m=application 9 UDP/DTLS/SCTP webrtc-datachannel",
            "c=IN IP4 0.0.0.0",
            "a=mid:2",
            "a=sctp-port:5000",
            "a=max-message-size:262144",
            ""
        });

        SdpParseResult result = new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description!;
        SdpMediaDescription audio = description.MediaDescriptions[0];
        SdpMediaDescription video = description.MediaDescriptions[1];
        SdpMediaDescription data = description.MediaDescriptions[2];
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        List<SdpIceCandidate> candidates = audio.Attributes
            .Where(attribute => attribute.Name == SdpAttributeNames.Candidate)
            .Select(attribute => codec.DecodeAttribute(attribute).IceCandidate!)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Attributes.Single(attribute => attribute.Name == SdpAttributeNames.Group).Value, Is.EqualTo("BUNDLE 0 1 2"));
            Assert.That(description.Attributes.Single(attribute => attribute.Name == "x-google-flag").RawText, Is.EqualTo("x-google-flag:conference"));
            Assert.That(description.MediaDescriptions.Select(media => media.MediaKind), Is.EqualTo(new[] { SdpMediaKind.Audio, SdpMediaKind.Video, SdpMediaKind.Application }));
            Assert.That(audio.Formats, Is.EqualTo(new[] { "111", "0", "8" }));
            Assert.That(candidates.Select(candidate => candidate.Address), Is.EqualTo(new[] { "192.0.2.10", "2001:db8::10" }));
            Assert.That(video.Attributes.Single(attribute => attribute.Name == SdpAttributeNames.Simulcast).Value, Is.EqualTo("send high;low"));
            Assert.That(video.Attributes.Single(attribute => attribute.Name == "x-browser-video-mode").Value, Is.EqualTo("realtime"));
            Assert.That(data.Protocol, Is.EqualTo("UDP/DTLS/SCTP"));
            Assert.That(data.Attributes.Single(attribute => attribute.Name == SdpAttributeNames.SctpPort).Value, Is.EqualTo("5000"));
        });
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
