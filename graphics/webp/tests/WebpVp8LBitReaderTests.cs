using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LBitReaderTests
{
    [Test]
    public void Reader_ReadsLeastSignificantBitFirst()
    {
        WebpVp8LBitReader reader = new WebpVp8LBitReader(new byte[] { 0xAC, 0x03 });

        Assert.That(reader.ReadBits(4), Is.EqualTo(0xCU));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xAU));
        Assert.That(reader.ReadBits(2), Is.EqualTo(0x3U));
        Assert.That(reader.BitPosition(), Is.EqualTo(10));
        Assert.That(reader.BitsRemaining(), Is.EqualTo(6));
    }

    [Test]
    public void ParseHeader_DecodesFieldsAndLeavesReaderAtImageData()
    {
        byte[] payload = NewVp8LPayload(17, 33, true, 0, new byte[] { 0xAC, 0x03 });

        WebpVp8LHeaderParseResult result = WebpVp8LBitReader.ParseHeader(payload, WebpStrictness.Strict);
        WebpVp8LBitReader reader = result.Reader!;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Header.Signature, Is.EqualTo(WebpConstants.Vp8LSignature));
        Assert.That(result.Header.Width, Is.EqualTo(17));
        Assert.That(result.Header.Height, Is.EqualTo(33));
        Assert.That(result.Header.HasAlpha, Is.True);
        Assert.That(result.Header.Version, Is.EqualTo(0));
        Assert.That(reader.Header, Is.SameAs(result.Header));
        Assert.That(reader.BitPosition(), Is.EqualTo(WebpConstants.Vp8LHeaderSize * 8));
        Assert.That(reader.BytePosition(), Is.EqualTo(WebpConstants.Vp8LHeaderSize));
        Assert.That(reader.BitsRemaining(), Is.EqualTo(16));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xCU));
        Assert.That(reader.ReadBits(4), Is.EqualTo(0xAU));
    }

    [Test]
    public void ParseHeader_CopiesPayloadForPositionedReader()
    {
        byte[] payload = NewVp8LPayload(1, 1, false, 0, new byte[] { 0xAC });

        WebpVp8LHeaderParseResult result = WebpVp8LBitReader.ParseHeader(payload, WebpStrictness.Strict);
        payload[WebpConstants.Vp8LHeaderSize] = 0;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reader!.ReadBits(8), Is.EqualTo(0xACU));
    }

    [Test]
    public void ParseHeader_RejectsShortPayloadBadSignatureAndBadVersion()
    {
        byte[] badSignature = NewVp8LPayload(1, 1, false, 0, Array.Empty<byte>());
        badSignature[0] = 0;
        byte[] badVersion = NewVp8LPayload(1, 1, false, 1, Array.Empty<byte>());

        WebpVp8LHeaderParseResult shortResult = WebpVp8LBitReader.ParseHeader(new byte[4], WebpStrictness.Strict);
        WebpVp8LHeaderParseResult signatureResult = WebpVp8LBitReader.ParseHeader(badSignature, WebpStrictness.Strict);
        WebpVp8LHeaderParseResult versionResult = WebpVp8LBitReader.ParseHeader(badVersion, WebpStrictness.Strict);

        Assert.That(shortResult.IsValid, Is.False);
        Assert.That(shortResult.Diagnostics[0].Message, Does.Contain("too short"));
        Assert.That(signatureResult.IsValid, Is.False);
        Assert.That(signatureResult.Reader, Is.Null);
        Assert.That(signatureResult.Diagnostics[0].Message, Does.Contain("signature"));
        Assert.That(versionResult.IsValid, Is.False);
        Assert.That(versionResult.Diagnostics[0].Offset, Is.EqualTo(4));
        Assert.That(versionResult.Diagnostics[0].Message, Does.Contain("version"));
    }

    [Test]
    public void Reader_CanStartAtConfiguredBitPositionAndRejectsInvalidPositions()
    {
        WebpVp8LHeader header = new WebpVp8LHeader();
        byte[] data = new byte[] { 0xF0 };
        WebpVp8LBitReader reader = WebpVp8LBitReader.FromState(data, header, 4);
        data[0] = 0;

        Assert.That(reader.ReadBits(4), Is.EqualTo(0xFU));
        Assert.That(reader.Header, Is.SameAs(header));
        Assert.Throws<ArgumentNullException>(() => WebpVp8LBitReader.FromState(null!, header, 0));
        Assert.Throws<ArgumentNullException>(() => WebpVp8LBitReader.FromState(new byte[] { 0 }, null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LBitReader.FromState(new byte[] { 0 }, header, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LBitReader.FromState(new byte[] { 0 }, header, 9));
    }

    [Test]
    public void Reader_RejectsInvalidOrUnavailableBitCounts()
    {
        WebpVp8LBitReader reader = new WebpVp8LBitReader(new byte[] { 0xFF });

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBits(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadBits(33));
        Assert.Throws<ArgumentException>(() => reader.ReadBits(9));
    }

    static byte[] NewVp8LPayload(int width, int height, bool alpha, int version, byte[] body)
    {
        byte[] payload = new byte[WebpConstants.Vp8LHeaderSize + body.Length];
        uint bits = (uint)(width - 1);
        bits = bits | ((uint)(height - 1) << 14);
        if (alpha)
        {
            bits = bits | (1U << 28);
        }

        bits = bits | ((uint)version << 29);
        payload[0] = WebpConstants.Vp8LSignature;
        WebpEndianWriter.WriteUInt32(payload, 1, bits);
        int index = 0;
        while (index < body.Length)
        {
            payload[WebpConstants.Vp8LHeaderSize + index] = body[index];
            index = index + 1;
        }

        return payload;
    }
}
