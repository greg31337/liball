namespace sdp.Tests.Interop.SdpTransform;

public class SdpTransformFixtureTests
{
    [TestCase(SdpParseMode.Strict, false, SdpValidationSeverity.Error)]
    [TestCase(SdpParseMode.Interoperable, true, SdpValidationSeverity.Warning)]
    [TestCase(SdpParseMode.Lossless, true, SdpValidationSeverity.Warning)]
    public void ReadmeFixture_HandlesConnectionAfterTimingByMode(SdpParseMode mode, bool success, SdpValidationSeverity severity)
    {
        string input =
            "v=0\r\n" +
            "o=- 20518 0 IN IP4 203.0.113.1\r\n" +
            "s= \r\n" +
            "t=0 0\r\n" +
            "c=IN IP4 203.0.113.1\r\n" +
            "a=ice-ufrag:F7gI\r\n" +
            "a=ice-pwd:x9cml/YzichV2+XlhiMu8g\r\n" +
            "m=audio 54400 RTP/SAVPF 0 96\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "a=rtpmap:96 opus/48000/2";
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('c'));
            Assert.That(result.Description?.Connection?.Address, Is.EqualTo("203.0.113.1"));
            Assert.That(result.Description?.MediaDescriptions[0].Attributes[1].Value, Is.EqualTo("96 opus/48000/2"));
        });
    }
}
