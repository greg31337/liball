using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PackedSampleWriterTests
{
    [Test]
    public void Pack1Bit_WritesSamplesMostSignificantBitFirst()
    {
        byte[] packed = PngPackedSampleWriter.Pack1Bit(new byte[] { 1, 0, 1, 1, 0, 0, 1, 0 }, 0, 8);

        Assert.That(packed, Is.EqualTo(new byte[] { 0b10110010 }));
    }

    [Test]
    public void Pack1Bit_PadsPartialFinalByteWithZeroBits()
    {
        byte[] packed = PngPackedSampleWriter.Pack1Bit(new byte[] { 1, 0, 1 }, 0, 3);

        Assert.That(packed, Is.EqualTo(new byte[] { 0b10100000 }));
    }

    [Test]
    public void Pack2Bit_WritesSamplesMostSignificantBitsFirst()
    {
        byte[] packed = PngPackedSampleWriter.Pack2Bit(new byte[] { 0, 1, 2, 3, 3, 2 }, 0, 6);

        Assert.That(packed, Is.EqualTo(new byte[] { 0b00011011, 0b11100000 }));
    }

    [Test]
    public void Pack4Bit_WritesSamplesMostSignificantNibbleFirst()
    {
        byte[] packed = PngPackedSampleWriter.Pack4Bit(new byte[] { 1, 15, 10 }, 0, 3);

        Assert.That(packed, Is.EqualTo(new byte[] { 0x1F, 0xA0 }));
    }

    [Test]
    public void Pack_ReadsSamplesAtOffset()
    {
        byte[] samples = new byte[] { 9, 1, 0, 3, 2, 9 };

        byte[] packed = PngPackedSampleWriter.Pack(samples, 1, 4, PngBitDepth.Two);

        Assert.That(packed, Is.EqualTo(new byte[] { 0b01001110 }));
    }

    [Test]
    public void Pack_AllowsZeroSamples()
    {
        byte[] packed = PngPackedSampleWriter.Pack(new byte[0], 0, 0, PngBitDepth.One);

        Assert.That(packed, Is.Not.Null);
        Assert.That(packed, Is.Empty);
    }

    [Test]
    public void PackInto_WritesAtOffsetAndClearsDestinationBytes()
    {
        byte[] destination = new byte[] { 0xFF, 0xFF, 0xFF };
        byte[] samples = new byte[] { 1, 0, 3, 2 };

        PngPackedSampleWriter.PackInto(destination, 1, samples, 0, samples.Length, PngBitDepth.Two);

        Assert.That(destination, Is.EqualTo(new byte[] { 0xFF, 0b01001110, 0xFF }));
    }

    [Test]
    public void PackRow_UsesGrayscaleLayoutWidthAndBitDepth()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(5, 1, PngColorType.Grayscale, PngBitDepth.One);
        byte[] samples = new byte[] { 1, 0, 1, 0, 1 };

        byte[] row = PngPackedSampleWriter.PackRow(samples, layout);

        Assert.That(row, Is.EqualTo(new byte[] { 0b10101000 }));
    }

    [Test]
    public void PackRow_UsesIndexedLayoutWidthAndBitDepth()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(3, 1, PngColorType.IndexedColor, PngBitDepth.Four);
        byte[] samples = new byte[] { 1, 2, 3 };

        byte[] row = PngPackedSampleWriter.PackRow(samples, layout);

        Assert.That(row, Is.EqualTo(new byte[] { 0x12, 0x30 }));
    }

    [Test]
    public void Pack_RoundTripsThroughUnpacker()
    {
        byte[] samples = new byte[] { 0, 1, 2, 3, 0, 2, 1 };

        byte[] packed = PngPackedSampleWriter.Pack2Bit(samples, 0, samples.Length);
        byte[] unpacked = PngPackedSampleUnpacker.Unpack2Bit(packed, 0, samples.Length);

        Assert.That(unpacked, Is.EqualTo(samples));
    }

    [Test]
    public void Pack_RejectsInvalidInputs()
    {
        byte[] samples = new byte[] { 0, 1, 2, 3 };

        Assert.Throws<ArgumentNullException>(() => PngPackedSampleWriter.Pack(null!, 0, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(samples, -1, 1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(samples, 5, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(samples, 0, -1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(samples, 1, samples.Length, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(samples, 0, 1, PngBitDepth.Eight));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(new byte[] { 2 }, 0, 1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(new byte[] { 4 }, 0, 1, PngBitDepth.Two));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.Pack(new byte[] { 16 }, 0, 1, PngBitDepth.Four));
    }

    [Test]
    public void PackInto_RejectsInvalidInputs()
    {
        byte[] destination = new byte[1];
        byte[] samples = new byte[] { 1 };

        Assert.Throws<ArgumentNullException>(() => PngPackedSampleWriter.PackInto(null!, 0, samples, 0, 1, PngBitDepth.One));
        Assert.Throws<ArgumentNullException>(() => PngPackedSampleWriter.PackInto(destination, 0, null!, 0, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackInto(destination, -1, samples, 0, 1, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackInto(destination, 2, samples, 0, 0, PngBitDepth.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackInto(destination, 0, new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }, 0, 9, PngBitDepth.One));
    }

    [Test]
    public void PackRow_RejectsInvalidInputs()
    {
        PngScanlineLayout grayscale = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.One);
        PngScanlineLayout truecolor = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Truecolor, PngBitDepth.Eight);
        PngScanlineLayout grayscale8 = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngPackedSampleWriter.PackRow(null!, grayscale));
        Assert.Throws<ArgumentNullException>(() => PngPackedSampleWriter.PackRow(new byte[1], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackRow(new byte[1], truecolor));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackRow(new byte[1], grayscale8));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngPackedSampleWriter.PackRow(new byte[0], grayscale));
    }
}
