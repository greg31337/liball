namespace sdp.Tests.Interop.Pjsip;

public class PjsipFixtureTests
{
    [TestCase(SdpParseMode.Strict)]
    [TestCase(SdpParseMode.Interoperable)]
    [TestCase(SdpParseMode.Lossless)]
    public void SessionAndMediaConnectionFixture_IsAcceptedInEveryMode(SdpParseMode mode)
    {
        string input =
            "v=0\r\n" +
            "o=pjmedia 123456 654321 IN IP4 192.0.2.30\r\n" +
            "s=pjmedia\r\n" +
            "c=IN IP4 192.0.2.30\r\n" +
            "t=0 0\r\n" +
            "m=audio 4000 RTP/AVP 0 8 96\r\n" +
            "a=rtcp:4001 IN IP4 192.0.2.30\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=rtpmap:8 PCMA/8000\r\n" +
            "a=rtpmap:96 telephone-event/8000\r\n" +
            "a=fmtp:96 0-16\r\n" +
            "a=sendrecv";
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo("192.0.2.30"));
            Assert.That(result.Description?.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "8", "96" }));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes, Has.Count.EqualTo(6));
        });
    }
}
