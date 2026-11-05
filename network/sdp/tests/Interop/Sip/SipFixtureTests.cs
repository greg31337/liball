namespace sdp.Tests.Interop.Sip;

public class SipFixtureTests
{
    [TestCase(SdpParseMode.Strict, false, SdpValidationSeverity.Error, 1)]
    [TestCase(SdpParseMode.Interoperable, true, SdpValidationSeverity.Warning, 1)]
    [TestCase(SdpParseMode.Lossless, true, SdpValidationSeverity.Warning, 2)]
    public void Rfc3264StyleOffer_HandlesLegacyEmptySessionNameByMode(SdpParseMode mode, bool success, SdpValidationSeverity severity, int diagnosticCount)
    {
        string input =
            "v=0\r\n" +
            "o=alice 2890844526 2890844526 IN IP4 host.example.com\r\n" +
            "s=\r\n" +
            "c=IN IP4 host.example.com\r\n" +
            "t=0 0\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "a=rtpmap:0 PCMU/8000\r\n" +
            "m=video 51372 RTP/AVP 31\r\n" +
            "a=rtpmap:31 H261/90000";
        ISdpParser parser = new SdpParser();

        SdpParseResult result = parser.ParseText(input, new SdpParseOptions { Mode = mode });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(diagnosticCount));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(severity));
            Assert.That(result.Description?.SessionName, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions, Has.Count.EqualTo(2));
        });
    }
}
