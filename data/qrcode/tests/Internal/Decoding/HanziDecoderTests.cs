using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class HanziDecoderTests
{
    [TestCase(0x0000, 0xA1A1)]
    [TestCase(0x03BD, 0xAAFE)]
    [TestCase(0x03C0, 0xB0A1)]
    [TestCase(0x0F23, 0xCEC4)]
    [TestCase(0x122F, 0xD6D0)]
    [TestCase(0x1FDD, 0xFAFE)]
    public void RestoresThirteenBitValuesToExactGb2312Values(int transformedValue, int expected)
    {
        Assert.That(HanziDecoder.RestoreGb2312Value(transformedValue), Is.EqualTo(expected));
    }

    [TestCase(0x03BE)]
    [TestCase(0x03BF)]
    [TestCase(0x1FDE)]
    [TestCase(0x1FFF)]
    [TestCase(-1)]
    [TestCase(0x2000)]
    public void RejectsValuesWithoutAValidInverse(int transformedValue)
    {
        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.RestoreGb2312Value(transformedValue))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void ReadsOnlySubsetOneWhenCompatibilityIsEnabled()
    {
        BitBuffer buffer = new BitBuffer(0);
        HanziEncoder.EncodeSubsetIndicator(buffer, true);
        BitReader enabled = new BitReader(buffer.GetBytes(), 4);
        BitReader disabled = new BitReader(buffer.GetBytes(), 4);

        int subset = HanziDecoder.ReadSubsetIndicator(enabled, true);
        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.ReadSubsetIndicator(disabled, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(subset, Is.EqualTo(HanziEncoder.Subset1));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
            Assert.That(disabled.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsUnknownAndTruncatedSubsetIndicators()
    {
        BitReader unknown = new BitReader(new byte[] { 0x20 }, 4);
        BitReader truncated = new BitReader(new byte[] { 0x00 }, 3);

        QrDecodeException unknownException = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.ReadSubsetIndicator(unknown, true))!;
        QrDecodeException truncatedException = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.ReadSubsetIndicator(truncated, true))!;

        Assert.Multiple(() =>
        {
            Assert.That(unknownException.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
            Assert.That(truncatedException.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(truncated.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesSegmentWithRawGb2312AndVisibleCompatibilityMetadata()
    {
        BitBuffer buffer = new BitBuffer(0);
        HanziEncoder.EncodeText("中文", buffer, true);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        QrSegment segment = HanziDecoder.DecodeCompatibilitySegment(reader, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Hanzi));
            Assert.That(segment.CharacterCount, Is.EqualTo(2));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0xD6, 0xD0, 0xCE, 0xC4 }));
            Assert.That(segment.Text, Is.EqualTo("中文"));
            Assert.That(segment.IsHanziCompatibilityMode, Is.True);
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [TestCase("")]
    [TestCase("中")]
    [TestCase("中文")]
    [TestCase("汉字")]
    [TestCase("　、。・")]
    public void RoundTripsStandardHanziTextLosslessly(string text)
    {
        BitBuffer buffer = new BitBuffer(0);
        HanziEncoder.EncodeText(text, buffer, true);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        string decoded = HanziDecoder.DecodeText(reader, text.Length, true);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(text));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsUnassignedStandardValueAndDisabledDecoding()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits((uint)HanziEncoder.TransformGb2312Value(0xA2A1), 13);
        BitReader unassigned = new BitReader(buffer.GetBytes(), 13);
        BitReader disabled = new BitReader(buffer.GetBytes(), 13);

        QrDecodeException unassignedException = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.DecodeText(unassigned, 1, true))!;
        QrDecodeException disabledException = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.DecodeText(disabled, 1, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(unassignedException.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(disabledException.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
            Assert.That(disabled.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void TruncatedSegmentIsRejectedBeforeReaderPositionChanges()
    {
        BitReader reader = new BitReader(new byte[] { 0, 0 }, 12);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            HanziDecoder.DecodeText(reader, 1, true))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsNullReaderAndInvalidOrOverLimitCounts()
    {
        BitReader reader = new BitReader(new byte[0], 0);
        QrInputException nullReader = Assert.Throws<QrInputException>(() =>
            HanziDecoder.DecodeText(null!, 0, true))!;
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            HanziDecoder.DecodeText(reader, -1, true))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            HanziDecoder.DecodeText(reader, QrLimits.DefaultMaximumInputBytes + 1, true))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullReader.InputName, Is.EqualTo("reader"));
            Assert.That(negative.InputName, Is.EqualTo("characterCount"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }
}
