namespace sdp.Tests.Interop.LibSdpTransform;

public class LibSdpTransformFixtureTests
{
    [TestCase(SdpParseMode.Strict, false, 2)]
    [TestCase(SdpParseMode.Interoperable, true, 2)]
    [TestCase(SdpParseMode.Lossless, true, 3)]
    public void ReadmeFixture_CombinesLegacyEmptyNameLfAndFieldOrderPolicies(SdpParseMode mode, bool success, int diagnosticCount)
    {
        string input =
            "v=0\n" +
            "o=- 20518 0 IN IP4 203.0.113.1\n" +
            "s=\n" +
            "t=0 0\n" +
            "c=IN IP4 203.0.113.1\n" +
            "a=ice-ufrag:F7gI\n" +
            "m=audio 54400 RTP/SAVPF 0 96\n" +
            "a=rtpmap:0 PCMU/8000\n" +
            "a=rtpmap:96 opus/48000/2";
        ISdpParser parser = new SdpParser();
        SdpParseOptions options = new SdpParseOptions { Mode = mode, AllowLfLineEndings = true };

        SdpParseResult result = parser.ParseText(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(success));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(diagnosticCount));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidToken));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(mode == SdpParseMode.Strict ? SdpValidationSeverity.Error : SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[diagnosticCount - 1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseFieldOutOfOrder));
            Assert.That(result.Description?.SessionName, Is.Empty);
            Assert.That(result.Description?.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "0", "96" }));
            if (mode == SdpParseMode.Lossless)
            {
                Assert.That(result.Description?.RawLines[2].IsMalformed, Is.True);
                Assert.That(result.Description?.RawLines[2].LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            }
        });
    }
}
