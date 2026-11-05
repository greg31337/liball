using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRationalCodecTests
{
    [Test]
    public void Decode_ReadsLittleEndianUnsignedRationals()
    {
        byte[] bytes = new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x2C, 0x01, 0x00, 0x00,
            0x64, 0x00, 0x00, 0x00
        };

        TiffRationalCodecResult result = TiffRationalCodec.Decode(bytes, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Values.Length, Is.EqualTo(2));
        Assert.That(result.Values[0].Numerator, Is.EqualTo(1));
        Assert.That(result.Values[0].Denominator, Is.EqualTo(2));
        Assert.That(result.Values[0].ToDouble(), Is.EqualTo(0.5d));
        Assert.That(result.Values[1].Numerator, Is.EqualTo(300));
        Assert.That(result.Values[1].Denominator, Is.EqualTo(100));
    }

    [Test]
    public void Decode_ReadsBigEndianSignedRationals()
    {
        byte[] bytes = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x03,
            0xFF, 0xFF, 0xFF, 0xFC
        };

        TiffRationalCodecResult result = TiffRationalCodec.Decode(bytes, TiffDataType.SRational, TiffByteOrder.BigEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Values.Length, Is.EqualTo(2));
        Assert.That(result.Values[0].Numerator, Is.EqualTo(-1));
        Assert.That(result.Values[0].Denominator, Is.EqualTo(2));
        Assert.That(result.Values[1].Numerator, Is.EqualTo(3));
        Assert.That(result.Values[1].Denominator, Is.EqualTo(-4));
    }

    [Test]
    public void Decode_ReportsByteLengthThatIsNotMultipleOfEight()
    {
        TiffRationalCodecResult result = TiffRationalCodec.Decode(new byte[] { 1, 2, 3 }, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
        Assert.That(result.Values.Length, Is.EqualTo(0));
    }

    [Test]
    public void Decode_ReportsZeroDenominator()
    {
        byte[] bytes = new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };

        TiffRationalCodecResult result = TiffRationalCodec.Decode(bytes, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
        Assert.That(result.Values[0].HasValidDenominator(), Is.False);
        Assert.That(result.Values[0].ToDouble(), Is.EqualTo(0.0d));
    }

    [Test]
    public void Encode_WritesLittleEndianUnsignedRationals()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create(1, 2), TiffRational.Create(300, 100) };

        TiffRationalCodecResult result = TiffRationalCodec.Encode(values, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[]
        {
            0x01, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x00, 0x00,
            0x2C, 0x01, 0x00, 0x00,
            0x64, 0x00, 0x00, 0x00
        }));
    }

    [Test]
    public void Encode_WritesBigEndianSignedRationals()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create(-1, 2), TiffRational.Create(3, -4) };

        TiffRationalCodecResult result = TiffRationalCodec.Encode(values, TiffDataType.SRational, TiffByteOrder.BigEndian);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Bytes, Is.EqualTo(new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x03,
            0xFF, 0xFF, 0xFF, 0xFC
        }));
    }

    [Test]
    public void Encode_RejectsUnsignedRationalNegativeOrTooLargeValues()
    {
        TiffRational[] negative = new TiffRational[] { TiffRational.Create(-1, 2) };
        TiffRational[] tooLarge = new TiffRational[] { TiffRational.Create((long)uint.MaxValue + 1L, 2) };

        TiffRationalCodecResult negativeResult = TiffRationalCodec.Encode(negative, TiffDataType.Rational, TiffByteOrder.LittleEndian);
        TiffRationalCodecResult tooLargeResult = TiffRationalCodec.Encode(tooLarge, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(negativeResult.Success, Is.False);
        Assert.That(negativeResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
        Assert.That(negativeResult.Bytes.Length, Is.EqualTo(0));
        Assert.That(tooLargeResult.Success, Is.False);
        Assert.That(tooLargeResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
    }

    [Test]
    public void Encode_RejectsSignedRationalValuesOutsideInt32Range()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create((long)int.MaxValue + 1L, 2) };

        TiffRationalCodecResult result = TiffRationalCodec.Encode(values, TiffDataType.SRational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
    }

    [Test]
    public void Encode_RejectsZeroDenominator()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create(1, 0) };

        TiffRationalCodecResult result = TiffRationalCodec.Encode(values, TiffDataType.Rational, TiffByteOrder.LittleEndian);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidRationalValue), Is.True);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
    }

    [Test]
    public void Codec_RejectsInvalidDataTypeAndByteOrder()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create(1, 2) };

        TiffRationalCodecResult typeResult = TiffRationalCodec.Encode(values, TiffDataType.Double, TiffByteOrder.LittleEndian);
        TiffRationalCodecResult byteOrderResult = TiffRationalCodec.Decode(new byte[8], TiffDataType.Rational, TiffByteOrder.Unknown);

        Assert.That(typeResult.Success, Is.False);
        Assert.That(typeResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(byteOrderResult.Success, Is.False);
        Assert.That(byteOrderResult.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
    }

    [Test]
    public void Encode_CopiesInputRationals()
    {
        TiffRational[] values = new TiffRational[] { TiffRational.Create(1, 2) };

        TiffRationalCodecResult result = TiffRationalCodec.Encode(values, TiffDataType.Rational, TiffByteOrder.LittleEndian);
        values[0].Numerator = 99;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Values[0].Numerator, Is.EqualTo(1));
    }
}
