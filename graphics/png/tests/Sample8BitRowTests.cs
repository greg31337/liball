using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Sample8BitRowTests
{
    [TestCase(PngConstants.ColorTypeGrayscale, 2)]
    [TestCase(PngConstants.ColorTypeIndexedColor, 2)]
    [TestCase(PngConstants.ColorTypeTruecolor, 6)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, 4)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, 8)]
    public void Read_ReturnsRaw8BitSamplesForEvery8BitColorType(int colorType, int sampleCount)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, (PngColorType)colorType, PngBitDepth.Eight);
        byte[] row = CreateSequence(sampleCount);

        byte[] samples = PngSample8BitRow.Read(row, layout);

        Assert.That(samples, Is.EqualTo(row));
    }

    [TestCase(PngConstants.ColorTypeGrayscale, 2)]
    [TestCase(PngConstants.ColorTypeIndexedColor, 2)]
    [TestCase(PngConstants.ColorTypeTruecolor, 6)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, 4)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, 8)]
    public void Write_ReturnsRaw8BitRowForEvery8BitColorType(int colorType, int sampleCount)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, (PngColorType)colorType, PngBitDepth.Eight);
        byte[] samples = CreateSequence(sampleCount);

        byte[] row = PngSample8BitRow.Write(samples, layout);

        Assert.That(row, Is.EqualTo(samples));
    }

    [Test]
    public void Read_UsesOffset()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, PngColorType.Truecolor, PngBitDepth.Eight);
        byte[] wrapped = new byte[] { 99, 1, 2, 3, 4, 5, 6, 88 };

        byte[] samples = PngSample8BitRow.Read(wrapped, 1, layout);

        Assert.That(samples, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void WriteInto_UsesOffsets()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, PngColorType.Truecolor, PngBitDepth.Eight);
        byte[] destination = new byte[] { 99, 0, 0, 0, 0, 0, 0, 88 };
        byte[] samples = new byte[] { 77, 1, 2, 3, 4, 5, 6, 66 };

        PngSample8BitRow.WriteInto(destination, 1, samples, 1, layout);

        Assert.That(destination, Is.EqualTo(new byte[] { 99, 1, 2, 3, 4, 5, 6, 88 }));
    }

    [Test]
    public void Write_RoundTripsThroughRead()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(3, 1, PngColorType.TruecolorAlpha, PngBitDepth.Eight);
        byte[] samples = CreateSequence(12);

        byte[] row = PngSample8BitRow.Write(samples, layout);
        byte[] readSamples = PngSample8BitRow.Read(row, layout);

        Assert.That(readSamples, Is.EqualTo(samples));
    }

    [Test]
    public void Read_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngScanlineLayout packed = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.One);

        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.Read(null!, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.Read(new byte[1], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Read(new byte[1], packed));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Read(new byte[1], -1, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Read(new byte[1], 2, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Read(new byte[0], layout));
    }

    [Test]
    public void Write_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);
        PngScanlineLayout packed = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.One);

        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.Write(null!, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.Write(new byte[1], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Write(new byte[1], packed));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.Write(new byte[0], layout));
    }

    [Test]
    public void WriteInto_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.WriteInto(null!, 0, new byte[1], 0, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.WriteInto(new byte[1], 0, null!, 0, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample8BitRow.WriteInto(new byte[1], 0, new byte[1], 0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[1], -1, new byte[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[1], 2, new byte[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[0], 0, new byte[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[1], 0, new byte[1], -1, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[1], 0, new byte[1], 2, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample8BitRow.WriteInto(new byte[1], 0, new byte[0], 0, layout));
    }

    static byte[] CreateSequence(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(index + 1);
            index += 1;
        }

        return data;
    }
}
