using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class KanjiDecoderTests
{
    [TestCase(0x0000, 0x8140)]
    [TestCase(0x0120, 0x82A0)]
    [TestCase(0x073F, 0x8ABF)]
    [TestCase(0x173C, 0x9FFC)]
    [TestCase(0x1740, 0xE040)]
    [TestCase(0x1FFF, 0xEBBF)]
    public void RestoresThirteenBitValuesToExactShiftJisValues(int transformedValue, int expected)
    {
        Assert.That(KanjiDecoder.RestoreShiftJisValue(transformedValue), Is.EqualTo(expected));
    }

    [TestCase(0x173D)]
    [TestCase(0x173E)]
    [TestCase(0x173F)]
    [TestCase(-1)]
    [TestCase(0x2000)]
    public void RejectsValuesWithoutAValidInverse(int transformedValue)
    {
        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            KanjiDecoder.RestoreShiftJisValue(transformedValue))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [TestCase(0x8140, '\u3000')]
    [TestCase(0x82A0, 'あ')]
    [TestCase(0x8ABF, '漢')]
    [TestCase(0x8E9A, '字')]
    public void MapsStandardShiftJisValuesToText(int shiftJisValue, char expected)
    {
        Assert.That(KanjiDecoder.GetCharacter(shiftJisValue), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("あ")]
    [TestCase("漢字")]
    [TestCase("日本語")]
    [TestCase("　、。・")]
    public void RoundTripsStandardKanjiTextLosslessly(string text)
    {
        BitBuffer buffer = new BitBuffer(0);
        KanjiEncoder.EncodeText(text, buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());

        string decoded = KanjiDecoder.DecodeText(reader, text.Length);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(text));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void DecodesFromAnUnalignedReaderPosition()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);
        KanjiEncoder.EncodeText("漢字", buffer);
        BitReader reader = new BitReader(buffer.GetBytes(), buffer.GetBitLength());
        reader.SkipBits(3);

        string decoded = KanjiDecoder.DecodeText(reader, 2);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo("漢字"));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsAnUnassignedShiftJisValue()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits((uint)KanjiEncoder.TransformShiftJisValue(0x817F), 13);
        BitReader reader = new BitReader(buffer.GetBytes(), 13);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            KanjiDecoder.DecodeText(reader, 1))!;

        Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
    }

    [Test]
    public void TruncatedSegmentIsRejectedBeforeReaderPositionChanges()
    {
        BitReader reader = new BitReader(new byte[] { 0, 0 }, 12);

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            KanjiDecoder.DecodeText(reader, 1))!;

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
            KanjiDecoder.DecodeText(null!, 0))!;
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            KanjiDecoder.DecodeText(reader, -1))!;
        QrResourceLimitException overLimit = Assert.Throws<QrResourceLimitException>(() =>
            KanjiDecoder.DecodeText(reader, QrLimits.DefaultMaximumInputBytes + 1))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullReader.InputName, Is.EqualTo("reader"));
            Assert.That(negative.InputName, Is.EqualTo("characterCount"));
            Assert.That(overLimit.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(reader.GetPosition(), Is.EqualTo(0));
        });
    }
}
