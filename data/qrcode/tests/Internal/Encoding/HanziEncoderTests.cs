using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class HanziEncoderTests
{
    [Test]
    public void RecognizesOnlyTheTwoPermittedGb2312Ranges()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xA1A1), Is.True);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xAAFE), Is.True);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xB0A1), Is.True);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xFAFE), Is.True);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xA1A0), Is.False);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xAAFF), Is.False);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xB0A0), Is.False);
            Assert.That(HanziEncoder.IsPermittedGb2312Value(0xFAFF), Is.False);
        });
    }

    [TestCase(0xA1A1, 0x0000)]
    [TestCase(0xAAFE, 0x03BD)]
    [TestCase(0xB0A1, 0x03C0)]
    [TestCase(0xCEC4, 0x0F23)]
    [TestCase(0xD6D0, 0x122F)]
    [TestCase(0xFAFE, 0x1FDD)]
    public void TransformsGb2312ValuesToExactThirteenBitValues(int gb2312Value, int expected)
    {
        Assert.That(HanziEncoder.TransformGb2312Value(gb2312Value), Is.EqualTo(expected));
    }

    [Test]
    public void EncodesStandardHanziTextExactlyWhenEnabled()
    {
        BitBuffer buffer = new BitBuffer(0);

        HanziEncoder.EncodeText("中文", buffer, true);

        Assert.Multiple(() =>
        {
            Assert.That(HanziEncoder.GetGb2312Value('中'), Is.EqualTo(0xD6D0));
            Assert.That(HanziEncoder.GetGb2312Value('文'), Is.EqualTo(0xCEC4));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(26));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0x91, 0x7B, 0xC8, 0xC0 }));
        });
    }

    [Test]
    public void WritesTheSubsetOneIndicatorOnlyWhenEnabled()
    {
        BitBuffer enabled = new BitBuffer(0);
        enabled.AppendBits(0b10U, 2);
        HanziEncoder.EncodeSubsetIndicator(enabled, true);
        BitBuffer disabled = new BitBuffer(0);
        disabled.AppendBits(0b10U, 2);

        QrInputException exception = Assert.Throws<QrInputException>(() =>
            HanziEncoder.EncodeSubsetIndicator(disabled, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(enabled.GetBitLength(), Is.EqualTo(6));
            Assert.That(enabled.GetBytes(), Is.EqualTo(new byte[] { 0x84 }));
            Assert.That(exception.InputName, Is.EqualTo("enableHanziCompatibility"));
            Assert.That(disabled.GetBitLength(), Is.EqualTo(2));
            Assert.That(disabled.GetBytes(), Is.EqualTo(new byte[] { 0x80 }));
        });
    }

    [Test]
    public void CreatesSegmentWithRawGb2312AndVisibleCompatibilityMetadata()
    {
        QrSegment segment = HanziEncoder.CreateCompatibilitySegment("中文", true);

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Hanzi));
            Assert.That(segment.CharacterCount, Is.EqualTo(2));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0xD6, 0xD0, 0xCE, 0xC4 }));
            Assert.That(segment.Text, Is.EqualTo("中文"));
            Assert.That(segment.IsHanziCompatibilityMode, Is.True);
        });
    }

    [Test]
    public void RejectsDisabledVendorAndUnmappedInputBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b101U, 3);

        QrInputException disabled = Assert.Throws<QrInputException>(() =>
            HanziEncoder.EncodeText("中文", buffer, false))!;
        QrInputException vendorConflict = Assert.Throws<QrInputException>(() =>
            HanziEncoder.EncodeText("·", buffer, true))!;
        QrInputException ascii = Assert.Throws<QrInputException>(() =>
            HanziEncoder.EncodeText("中A", buffer, true))!;

        Assert.Multiple(() =>
        {
            Assert.That(disabled.InputName, Is.EqualTo("enableHanziCompatibility"));
            Assert.That(vendorConflict.InputName, Is.EqualTo("character"));
            Assert.That(ascii.InputName, Is.EqualTo("character"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(3));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void RejectsNullInvalidRangeAndBitLengthOverflow()
    {
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            HanziEncoder.ValidateText(null!, true))!;
        QrInputException invalidRange = Assert.Throws<QrInputException>(() =>
            HanziEncoder.TransformGb2312Value(0xAAFF))!;
        QrInputException negativeCount = Assert.Throws<QrInputException>(() =>
            HanziEncoder.CalculateBitLength(-1))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            HanziEncoder.CalculateBitLength(int.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullText.InputName, Is.EqualTo("text"));
            Assert.That(invalidRange.InputName, Is.EqualTo("gb2312Value"));
            Assert.That(negativeCount.InputName, Is.EqualTo("characterCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("HanziBitLength"));
        });
    }

    [TestCase(0, 0)]
    [TestCase(1, 13)]
    [TestCase(2, 26)]
    [TestCase(3, 39)]
    public void CalculatesExactBitLength(int characterCount, int expectedBitLength)
    {
        Assert.That(HanziEncoder.CalculateBitLength(characterCount), Is.EqualTo(expectedBitLength));
    }
}
