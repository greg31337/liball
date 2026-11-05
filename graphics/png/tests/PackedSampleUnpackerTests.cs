using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PackedSampleUnpackerTests
{
    [Test]
    public void Unpack1Bit_ReadsSamplesMostSignificantBitFirst()
    {
        byte[] data = new byte[] { 0b10110010 };

        byte[] samples = PngPackedSampleUnpacker.Unpack1Bit(data, 0, 8);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 0, 1, 1, 0, 0, 1, 0 }));
    }

    [Test]
    public void Unpack1Bit_HandlesPartialFinalByte()
    {
        byte[] data = new byte[] { 0b10100000 };

        byte[] samples = PngPackedSampleUnpacker.Unpack1Bit(data, 0, 3);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 0, 1 }));
    }

    [Test]
    public void Unpack2Bit_ReadsSamplesMostSignificantBitsFirst()
    {
        byte[] data = new byte[] { 0b00011011, 0b11100100 };

        byte[] samples = PngPackedSampleUnpacker.Unpack2Bit(data, 0, 6);

        Assert.That(samples, Is.EqualTo(new byte[] { 0, 1, 2, 3, 3, 2 }));
    }

    [Test]
    public void Unpack4Bit_ReadsSamplesMostSignificantNibbleFirst()
    {
        byte[] data = new byte[] { 0x1F, 0xA0 };

        byte[] samples = PngPackedSampleUnpacker.Unpack4Bit(data, 0, 3);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 15, 10 }));
    }

    [Test]
    public void Unpack_ReadsAtOffset()
    {
        byte[] data = new byte[] { 0xFF, 0b01001110, 0xEE };

        byte[] samples = PngPackedSampleUnpacker.Unpack(data, 1, 4, PngBitDepth.Two);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 0, 3, 2 }));
    }

    [Test]
    public void Unpack_AllowsZeroSamples()
    {
        byte[] samples = PngPackedSampleUnpacker.Unpack(new byte[0], 0, 0, PngBitDepth.One);

        Assert.That(samples, Is.Not.Null);
        Assert.That(samples, Is.Empty);
    }

    [Test]
    public void UnpackRow_UsesGrayscaleLayoutWidthAndBitDepth()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(5, 1, PngColorType.Grayscale, PngBitDepth.One);
        byte[] row = new byte[] { 0b10101000 };

        byte[] samples = PngPackedSampleUnpacker.UnpackRow(row, layout);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 0, 1, 0, 1 }));
    }

    [Test]
    public void UnpackRow_UsesIndexedLayoutWidthAndBitDepth()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(3, 1, PngColorType.IndexedColor, PngBitDepth.Four);
        byte[] row = new byte[] { 0x12, 0x30 };

        byte[] samples = PngPackedSampleUnpacker.UnpackRow(row, layout);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Unpack_RejectsInvalidInputs()
    {
        byte[] data = new byte[] { 0xFF };

        Assert.Throws<ArgumentNullException>(() => PngPackedSampleUnpacker.Unpack(null!, 0, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.Unpack(data, -1, 1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.Unpack(data, 2, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.Unpack(data, 0, -1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.Unpack(data, 0, 9, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.Unpack(data, 0, 1, PngBitDepth.Eight));
    }

    [Test]
    public void UnpackRow_RejectsInvalidInputs()
    {
        PngScanlineLayout grayscale = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.One);
        PngScanlineLayout truecolor = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Truecolor, PngBitDepth.Eight);
        PngScanlineLayout grayscale8 = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngPackedSampleUnpacker.UnpackRow(null!, grayscale));
        Assert.Throws<ArgumentNullException>(() => PngPackedSampleUnpacker.UnpackRow(new byte[1], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.UnpackRow(new byte[1], truecolor));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.UnpackRow(new byte[1], grayscale8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleUnpacker.UnpackRow(new byte[0], grayscale));
    }
}
