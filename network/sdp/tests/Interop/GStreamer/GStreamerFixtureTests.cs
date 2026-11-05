namespace sdp.Tests.Interop.GStreamer;

public class GStreamerFixtureTests
{
    [Test]
    public void BareLfFixture_RequiresExplicitAcceptanceAndIsRetainedLosslessly()
    {
        string input =
            "v=0\n" +
            "o=- 1 1 IN IP4 192.0.2.20\n" +
            "s=GStreamer pipeline\n" +
            "c=IN IP4 239.1.1.1/32\n" +
            "t=0 0\n" +
            "m=video 5004 RTP/AVP 96\n" +
            "a=rtpmap:96 H264/90000";
        ISdpParser parser = new SdpParser();

        SdpParseResult strict = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Strict });
        SdpParseResult interoperable = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable, AllowLfLineEndings = true });
        SdpParseResult lossless = parser.ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless, AllowLfLineEndings = true });

        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable, AllowLfLineEndings = true });

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidLineEnding));
            Assert.That(interoperable.Success, Is.True);
            Assert.That(interoperable.Diagnostics, Is.Empty);
            Assert.That(lossless.Success, Is.True);
            Assert.That(lossless.Diagnostics, Is.Empty);
            Assert.That(lossless.Description?.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(lossless.Description?.RawLines[0].LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(lossless.Description?.MediaDescriptions[0].RawLines[1].Text, Is.EqualTo("a=rtpmap:96 H264/90000"));
        });
    }
}
