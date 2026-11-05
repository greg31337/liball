namespace sdp.Tests.Interop.Browsers;

public class BrowserFixtureTests
{
    [TestCase(SdpParseMode.Strict)]
    [TestCase(SdpParseMode.Interoperable)]
    [TestCase(SdpParseMode.Lossless)]
    public void WebRtcOfferFixture_PreservesOpaqueAttributesInEveryMode(SdpParseMode mode)
    {
        string input =
            "v=0\r\n" +
            "o=- 4611733054144332974 2 IN IP4 127.0.0.1\r\n" +
            "s=-\r\n" +
            "t=0 0\r\n" +
            "a=group:BUNDLE 0 1\r\n" +
            "a=msid-semantic:WMS stream\r\n" +
            "m=audio 9 UDP/TLS/RTP/SAVPF 111\r\n" +
            "c=IN IP4 0.0.0.0\r\n" +
            "a=mid:0\r\n" +
            "a=ice-ufrag:abcd\r\n" +
            "a=fingerprint:sha-256 00:11:22:33\r\n" +
            "a=rtpmap:111 opus/48000/2\r\n" +
            "m=video 9 UDP/TLS/RTP/SAVPF 96\r\n" +
            "c=IN IP4 0.0.0.0\r\n" +
            "a=mid:1\r\n" +
            "a=rtpmap:96 VP8/90000";
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Attributes[0].Value, Is.EqualTo("BUNDLE 0 1"));
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(result.Description?.MediaDescriptions[0].Protocol, Is.EqualTo("UDP/TLS/RTP/SAVPF"));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes[2].Name, Is.EqualTo("fingerprint"));
        });
    }
}
