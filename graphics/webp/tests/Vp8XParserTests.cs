using webp;

namespace webp.Tests;

[TestFixture]
public class Vp8XParserTests
{
    [Test]
    public void Parse_ValidPayloadExpandsFeatureFlagsAndDimensions()
    {
        byte flags = (byte)(
            WebpConstants.Vp8XFeatureIccProfile
            | WebpConstants.Vp8XFeatureAlpha
            | WebpConstants.Vp8XFeatureXmp);
        byte[] payload = NewPayload(flags, 300, 200);

        WebpVp8XParseResult result = WebpVp8XParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Header.FeatureFlags, Is.EqualTo(flags));
        Assert.That(result.Header.HasIccProfile, Is.True);
        Assert.That(result.Header.HasAlpha, Is.True);
        Assert.That(result.Header.HasExif, Is.False);
        Assert.That(result.Header.HasXmp, Is.True);
        Assert.That(result.Header.HasAnimation, Is.False);
        Assert.That(result.Header.HasReservedFeatureBits, Is.False);
        Assert.That(result.Header.HasReservedBytes, Is.False);
        Assert.That(result.Header.CanvasWidth, Is.EqualTo(300));
        Assert.That(result.Header.CanvasHeight, Is.EqualTo(200));
    }

    [Test]
    public void Parse_AllowsMaximumOneBasedCanvasDimensions()
    {
        byte[] payload = NewPayload(0, WebpConstants.Vp8XMaxCanvasDimension, WebpConstants.Vp8XMaxCanvasDimension);

        WebpVp8XParseResult result = WebpVp8XParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.CanvasWidth, Is.EqualTo(WebpConstants.Vp8XMaxCanvasDimension));
        Assert.That(result.Header.CanvasHeight, Is.EqualTo(WebpConstants.Vp8XMaxCanvasDimension));
    }

    [Test]
    public void Parse_RejectsPayloadsWithWrongSize()
    {
        WebpVp8XParseResult shortResult = WebpVp8XParser.Parse(new byte[9], WebpStrictness.Permissive);
        WebpVp8XParseResult longResult = WebpVp8XParser.Parse(new byte[11], WebpStrictness.Permissive);

        Assert.That(shortResult.IsValid, Is.False);
        Assert.That(longResult.IsValid, Is.False);
        Assert.That(shortResult.Diagnostics[0].Message, Does.Contain("10 bytes"));
        Assert.That(longResult.Diagnostics[0].Message, Does.Contain("10 bytes"));
    }

    [Test]
    public void Parse_ReservedFeatureBitsWarnInPermissiveMode()
    {
        byte[] payload = NewPayload(0x81, 1, 1);

        WebpVp8XParseResult result = WebpVp8XParser.Parse(payload, WebpStrictness.Permissive);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.HasReservedFeatureBits, Is.True);
        Assert.That(result.Header.ReservedFeatureBits, Is.EqualTo(0x81));
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
    }

    [Test]
    public void Parse_ReservedFeatureBitsFailInStrictMode()
    {
        byte[] payload = NewPayload(0x40, 1, 1);

        WebpVp8XParseResult result = WebpVp8XParser.Parse(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Header.HasReservedFeatureBits, Is.True);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ReservedBytesRespectStrictness()
    {
        byte[] permissivePayload = NewPayload(0, 10, 10);
        byte[] strictPayload = NewPayload(0, 10, 10);
        permissivePayload[2] = 0x12;
        strictPayload[3] = 0x34;

        WebpVp8XParseResult permissive = WebpVp8XParser.Parse(permissivePayload, WebpStrictness.Permissive);
        WebpVp8XParseResult strict = WebpVp8XParser.Parse(strictPayload, WebpStrictness.Strict);

        Assert.That(permissive.IsValid, Is.True);
        Assert.That(permissive.Header.HasReservedBytes, Is.True);
        Assert.That(permissive.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Warning));
        Assert.That(strict.IsValid, Is.False);
        Assert.That(strict.Header.HasReservedBytes, Is.True);
        Assert.That(strict.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
        Assert.That(strict.Diagnostics[0].Offset, Is.EqualTo(1));
    }

    static byte[] NewPayload(byte flags, int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        payload[0] = flags;
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(height - 1));
        return payload;
    }
}
