namespace sdp.Tests.Interop.Rtsp;

public class RtspFixtureTests
{
    [TestCase(SdpParseMode.Strict)]
    [TestCase(SdpParseMode.Interoperable)]
    [TestCase(SdpParseMode.Lossless)]
    public void AggregateControlFixture_IsAcceptedInEveryMode(SdpParseMode mode)
    {
        string input =
            "v=0\r\n" +
            "o=- 1700000000 1 IN IP4 192.0.2.10\r\n" +
            "s=RTSP presentation\r\n" +
            "i=Recorded presentation\r\n" +
            "c=IN IP4 0.0.0.0\r\n" +
            "t=0 0\r\n" +
            "a=control:*\r\n" +
            "a=range:npt=0-120.0\r\n" +
            "m=video 0 RTP/AVP 96\r\n" +
            "a=rtpmap:96 H264/90000\r\n" +
            "a=control:trackID=0\r\n" +
            "m=audio 0 RTP/AVP 0\r\n" +
            "a=control:trackID=1";
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description?.Attributes[0].Name, Is.EqualTo("control"));
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes[1].Value, Is.EqualTo("trackID=0"));
        });
    }
}
