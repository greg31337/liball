using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class KanjiEncoderTests
{
    [Test]
    public void RecognizesOnlyTheTwoPermittedShiftJisRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0x8140), Is.True);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0x9FFC), Is.True);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0xE040), Is.True);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0xEBBF), Is.True);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0x813F), Is.False);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0x9FFD), Is.False);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0xDFFF), Is.False);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0xE03F), Is.False);
            Assert.That(KanjiEncoder.IsPermittedShiftJisValue(0xEBC0), Is.False);
        });
    }

    [TestCase(0x8140, 0x0000)]
    [TestCase(0x82A0, 0x0120)]
    [TestCase(0x8ABF, 0x073F)]
    [TestCase(0x9FFC, 0x173C)]
    [TestCase(0xE040, 0x1740)]
    [TestCase(0xEBBF, 0x1FFF)]
    public void TransformsShiftJisValuesToExactThirteenBitValues(int shiftJisValue, int expected)
    {
        Assert.That(KanjiEncoder.TransformShiftJisValue(shiftJisValue), Is.EqualTo(expected));
    }

    [Test]
    public void MapsAndEncodesStandardKanjiTextExactly()
    {
        BitBuffer buffer = new BitBuffer(0);

        KanjiEncoder.EncodeText("漢字", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(KanjiEncoder.GetShiftJisValue('漢'), Is.EqualTo(0x8ABF));
            Assert.That(KanjiEncoder.GetShiftJisValue('字'), Is.EqualTo(0x8E9A));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(26));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0x39, 0xFA, 0x86, 0x80 }));
        });
    }

    [Test]
    public void AppendsAtAnUnalignedPositionWithoutChangingPrefixBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        KanjiEncoder.EncodeText("あ", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(16));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA1, 0x20 }));
        });
    }

    [Test]
    public void SelectsByteFallbackOnlyWhenExplicitlyAllowed()
    {
        Assert.Multiple(() =>
        {
            Assert.That(KanjiEncoder.SelectEncodingMode("漢字", true), Is.EqualTo(QrEncodingMode.Kanji));
            Assert.That(KanjiEncoder.SelectEncodingMode("A漢", true), Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(KanjiEncoder.SelectEncodingMode("①", true), Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(KanjiEncoder.SelectEncodingMode("😀", true), Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(() => KanjiEncoder.SelectEncodingMode("A漢", false), Throws.TypeOf<QrInputException>());
            Assert.That(() => KanjiEncoder.SelectEncodingMode("①", false), Throws.TypeOf<QrInputException>());
            Assert.That(() => KanjiEncoder.SelectEncodingMode("😀", false), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsNonKanjiMappingsBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrInputException singleByte = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.EncodeText("漢A", buffer))!;
        QrInputException vendorOnly = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.EncodeText("①", buffer))!;
        QrInputException surrogate = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.EncodeText("😀", buffer))!;

        Assert.Multiple(() =>
        {
            Assert.That(singleByte.InputName, Is.EqualTo("character"));
            Assert.That(vendorOnly.InputName, Is.EqualTo("character"));
            Assert.That(surrogate.InputName, Is.EqualTo("character"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void RejectsNullInvalidRangeAndBitLengthOverflow()
    {
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.ValidateText(null!))!;
        QrInputException invalidRange = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.TransformShiftJisValue(0x9FFD))!;
        QrInputException negativeCount = Assert.Throws<QrInputException>(() =>
            KanjiEncoder.CalculateBitLength(-1))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            KanjiEncoder.CalculateBitLength(int.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullText.InputName, Is.EqualTo("text"));
            Assert.That(invalidRange.InputName, Is.EqualTo("shiftJisValue"));
            Assert.That(negativeCount.InputName, Is.EqualTo("characterCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("KanjiBitLength"));
        });
    }

    [TestCase(0, 0)]
    [TestCase(1, 13)]
    [TestCase(2, 26)]
    [TestCase(3, 39)]
    public void CalculatesExactBitLength(int characterCount, int expectedBitLength)
    {
        Assert.That(KanjiEncoder.CalculateBitLength(characterCount), Is.EqualTo(expectedBitLength));
    }
}
