using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Sample16BitRowTests
{
    [TestCase(PngConstants.ColorTypeGrayscale, 2)]
    [TestCase(PngConstants.ColorTypeTruecolor, 6)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, 4)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, 8)]
    public void Read_ReturnsBigEndian16BitSamplesForEvery16BitColorType(int colorType, int sampleCount)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, (PngColorType)colorType, PngBitDepth.Sixteen);
        ushort[] expected = CreateSequence(sampleCount);
        byte[] row = CreateBigEndianRow(expected);

        ushort[] samples = PngSample16BitRow.Read(row, layout);

        Assert.That(samples, Is.EqualTo(expected));
    }

    [TestCase(PngConstants.ColorTypeGrayscale, 2)]
    [TestCase(PngConstants.ColorTypeTruecolor, 6)]
    [TestCase(PngConstants.ColorTypeGrayscaleAlpha, 4)]
    [TestCase(PngConstants.ColorTypeTruecolorAlpha, 8)]
    public void Write_ReturnsBigEndian16BitRowForEvery16BitColorType(int colorType, int sampleCount)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, (PngColorType)colorType, PngBitDepth.Sixteen);
        ushort[] samples = CreateSequence(sampleCount);

        byte[] row = PngSample16BitRow.Write(samples, layout);

        Assert.That(row, Is.EqualTo(CreateBigEndianRow(samples)));
    }

    [Test]
    public void Read_UsesOffset()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, PngColorType.Truecolor, PngBitDepth.Sixteen);
        ushort[] expected = new ushort[] { 0x1234, 0xABCD, 0x0001, 0x00FF, 0xFF00, 0x8001 };
        byte[] row = CreateBigEndianRow(expected);
        byte[] wrapped = new byte[row.Length + 2];
        wrapped[0] = 99;
        Array.Copy(row, 0, wrapped, 1, row.Length);
        wrapped[wrapped.Length - 1] = 88;

        ushort[] samples = PngSample16BitRow.Read(wrapped, 1, layout);

        Assert.That(samples, Is.EqualTo(expected));
    }

    [Test]
    public void WriteInto_UsesOffsets()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(2, 1, PngColorType.Truecolor, PngBitDepth.Sixteen);
        ushort[] samples = new ushort[] { 0x9999, 0x1234, 0xABCD, 0x0001, 0x00FF, 0xFF00, 0x8001, 0x8888 };
        byte[] destination = new byte[14];
        destination[0] = 77;
        destination[13] = 66;

        PngSample16BitRow.WriteInto(destination, 1, samples, 1, layout);

        byte[] expected = new byte[14];
        expected[0] = 77;
        Array.Copy(CreateBigEndianRow(new ushort[] { 0x1234, 0xABCD, 0x0001, 0x00FF, 0xFF00, 0x8001 }), 0, expected, 1, 12);
        expected[13] = 66;
        Assert.That(destination, Is.EqualTo(expected));
    }

    [Test]
    public void Write_RoundTripsThroughRead()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(3, 1, PngColorType.TruecolorAlpha, PngBitDepth.Sixteen);
        ushort[] samples = CreateSequence(12);

        byte[] row = PngSample16BitRow.Write(samples, layout);
        ushort[] readSamples = PngSample16BitRow.Read(row, layout);

        Assert.That(readSamples, Is.EqualTo(samples));
    }

    [Test]
    public void Read_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Sixteen);
        PngScanlineLayout eightBit = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.Read(null!, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.Read(new byte[2], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Read(new byte[2], eightBit));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Read(new byte[2], -1, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Read(new byte[2], 3, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Read(new byte[1], layout));
    }

    [Test]
    public void Write_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Sixteen);
        PngScanlineLayout eightBit = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Eight);

        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.Write(null!, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.Write(new ushort[1], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Write(new ushort[1], eightBit));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.Write(new ushort[0], layout));
    }

    [Test]
    public void WriteInto_RejectsInvalidInputs()
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(1, 1, PngColorType.Grayscale, PngBitDepth.Sixteen);

        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.WriteInto(null!, 0, new ushort[1], 0, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.WriteInto(new byte[2], 0, null!, 0, layout));
        Assert.Throws<ArgumentNullException>(() => PngSample16BitRow.WriteInto(new byte[2], 0, new ushort[1], 0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[2], -1, new ushort[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[2], 3, new ushort[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[1], 0, new ushort[1], 0, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[2], 0, new ushort[1], -1, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[2], 0, new ushort[1], 2, layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSample16BitRow.WriteInto(new byte[2], 0, new ushort[0], 0, layout));
    }

    static ushort[] CreateSequence(int length)
    {
        ushort[] data = new ushort[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (ushort)(0x1200 + index);
            index += 1;
        }

        return data;
    }

    static byte[] CreateBigEndianRow(ushort[] samples)
    {
        byte[] row = new byte[samples.Length * 2];
        int index = 0;
        int offset = 0;
        while (index < samples.Length)
        {
            PngBinaryWriter.WriteUInt16(row, offset, samples[index]);
            offset += 2;
            index += 1;
        }

        return row;
    }
}
