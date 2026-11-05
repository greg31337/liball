using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicTransformerFactoryEndToEndTests
{
    [TestCaseSource(nameof(CreatePixelFormatCases))]
    public void PublicFactoryAndTransformer_HandlePaddedRowsForEveryPixelFormat(PngPixelFormat format)
    {
        int width = 9;
        int height = 2;
        int rowBytes = checked((int)PngSizeCalculator.CalculateFrameRowBytes(width, format));
        int stride = rowBytes + 2;
        byte[] source = new byte[stride + rowBytes];
        Fill(source, 0, rowBytes, 0x11);
        Fill(source, rowBytes, 2, 0xEE);
        Fill(source, stride, rowBytes, 0xCC);
        PngImage image = new PngImageFactory().CreateImageWithStride(width, height, format, stride, source);
        PngPixelConversionOptions identityOptions = new PngPixelConversionOptions();
        identityOptions.TargetFormat = format;
        IPngTransformer transformer = new PngTransformer();

        PngPixelBuffer compact = transformer.ConvertPixelFormat(image.Pixels, identityOptions);
        PngPixelBuffer flipped = transformer.FlipVertical(image.Pixels);
        PngPixelBuffer restored = transformer.FlipVertical(flipped);
        source[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
            Assert.That(image.Pixels.Data[0], Is.EqualTo(0x11));
            Assert.That(compact.Format, Is.EqualTo(format));
            Assert.That(compact.Stride, Is.EqualTo(rowBytes));
            Assert.That(compact.Data, Is.EqualTo(Join(Filled(rowBytes, 0x11), Filled(rowBytes, 0xCC))));
            Assert.That(flipped.Stride, Is.EqualTo(rowBytes));
            Assert.That(flipped.Data, Is.EqualTo(Join(Filled(rowBytes, 0xCC), Filled(rowBytes, 0x11))));
            Assert.That(restored.Data, Is.EqualTo(compact.Data));
        });
    }

    [TestCase(PngPixelFormat.Grayscale1, 1)]
    [TestCase(PngPixelFormat.Grayscale2, 2)]
    [TestCase(PngPixelFormat.Grayscale4, 4)]
    [TestCase(PngPixelFormat.Indexed1, 1)]
    [TestCase(PngPixelFormat.Indexed2, 2)]
    [TestCase(PngPixelFormat.Indexed4, 4)]
    public void PublicFactoryAndTransformer_PreservePackedSamplesAcrossByteBoundaries(PngPixelFormat format, int bits)
    {
        int samplesPerByte = 8 / bits;
        int width = samplesPerByte + 1;
        byte[] samples = new byte[width];
        int maximum = (1 << bits) - 1;
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (byte)(index % (maximum + 1));
            index += 1;
        }

        PngBitDepth bitDepth = GetBitDepth(bits);
        byte[] packed = PngPackedSampleWriter.Pack(samples, 0, samples.Length, bitDepth);
        PngImage image = new PngImageFactory().CreateImageFromSpan(width, 1, format, packed);
        PngPixelBuffer cropped = new PngTransformer().Crop(image.Pixels, width - 1, 0, 1, 1);
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = IsIndexed(format) ? PngPixelFormat.Indexed8 : PngPixelFormat.Grayscale8;

        PngPixelBuffer expanded = new PngTransformer().ConvertPixelFormat(image.Pixels, options);
        PngPixelBuffer expandedCrop = new PngTransformer().ConvertPixelFormat(cropped, options);
        byte[] expected = new byte[samples.Length];
        index = 0;
        while (index < samples.Length)
        {
            expected[index] = IsIndexed(format) ? samples[index] : (byte)(samples[index] * 255 / maximum);
            index += 1;
        }

        Assert.That(expanded.Data, Is.EqualTo(expected));
        Assert.That(expandedCrop.Data, Is.EqualTo(new byte[] { expected[expected.Length - 1] }));
        Assert.That(cropped.Stride, Is.EqualTo(1));
    }

    [Test]
    public void PublicTransformer_HandlesAlphaAndTransparencyEdgeValuesWithReorderedChannels()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer bgr = new PngImageFactory().CreateImageFromSpan(
            2,
            1,
            PngPixelFormat.Bgr8,
            new byte[] { 0, 0, 0, 255, 255, 255 }).Pixels;
        PngTruecolorTransparency truecolorTransparency = new PngTruecolorTransparency();
        truecolorTransparency.Red = 0;
        truecolorTransparency.Green = 0;
        truecolorTransparency.Blue = 0;
        PngPixelConversionOptions truecolorOptions = new PngPixelConversionOptions();
        truecolorOptions.TargetFormat = PngPixelFormat.Argb8;
        truecolorOptions.TruecolorTransparency = truecolorTransparency;

        PngPixelBuffer argb = transformer.ConvertPixelFormat(bgr, truecolorOptions);
        PngPixelBuffer alphaEdges = new PngImageFactory().CreateImageFromSpan(
            4,
            1,
            PngPixelFormat.Rgba8,
            new byte[]
            {
                10, 20, 30, 0,
                10, 20, 30, 1,
                10, 20, 30, 254,
                10, 20, 30, 255
            }).Pixels;
        PngPixelBuffer inverted = transformer.InvertAlpha(alphaEdges);
        PngPixelBuffer cleared = transformer.ClearTransparentPixelColors(alphaEdges);

        PngPalette palette = CreatePalette(2);
        PngIndexedTransparency indexedTransparency = new PngIndexedTransparency();
        indexedTransparency.Alpha = new byte[] { 0, 255 };
        PngPixelBuffer indexed = new PngImageFactory().CreateIndexedImage(2, 1, PngPixelFormat.Indexed1, 1, new byte[] { 0x40 }, palette).Pixels;
        PngPixelBuffer indexedRgba = transformer.ExpandIndexedTransparency(indexed, palette, indexedTransparency);

        Assert.That(argb.Data, Is.EqualTo(new byte[] { 0, 0, 0, 0, 255, 255, 255, 255 }));
        Assert.That(inverted.Data, Is.EqualTo(new byte[]
        {
            10, 20, 30, 255,
            10, 20, 30, 254,
            10, 20, 30, 1,
            10, 20, 30, 0
        }));
        Assert.That(cleared.Data, Is.EqualTo(new byte[]
        {
            0, 0, 0, 0,
            10, 20, 30, 1,
            10, 20, 30, 254,
            10, 20, 30, 255
        }));
        Assert.That(indexedRgba.Data, Is.EqualTo(new byte[] { 0, 0, 0, 0, 1, 3, 7, 255 }));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public void PublicTransformer_RoundTripsEveryEightBitSignificantBitBoundary(int significantBits)
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer source = new PngImageFactory().CreateImageFromSpan(
            2,
            1,
            PngPixelFormat.Grayscale8,
            new byte[] { 0, 255 }).Pixels;
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = (byte)significantBits;

        PngPixelBuffer downshifted = transformer.DownshiftToSignificantBits(source, bits);
        PngPixelBuffer restored = transformer.UpscaleFromSignificantBits(downshifted, bits);

        Assert.That(downshifted.Data, Is.EqualTo(new byte[] { 0, (byte)((1 << significantBits) - 1) }));
        Assert.That(restored.Data, Is.EqualTo(new byte[] { 0, 255 }));
    }

    [Test]
    public void PublicTransformer_BuildsAndAppliesGammaTablesAtSampleBoundaries()
    {
        IPngTransformer transformer = new PngTransformer();
        byte[] identity8 = transformer.BuildIdentity8BitGammaTable();
        ushort[] identity16 = transformer.BuildIdentity16BitGammaTable();
        byte[] corrected8 = transformer.Build8BitGammaTableFromExponent(0.5, 1.0);
        ushort[] corrected16 = transformer.Build16BitGammaTableFromExponent(2.0, 1.0);
        PngPixelBuffer bgra = new PngImageFactory().CreateImageFromSpan(
            3,
            1,
            PngPixelFormat.Bgra8,
            new byte[] { 0, 0, 0, 7, 128, 128, 128, 8, 255, 255, 255, 9 }).Pixels;
        PngPixelBuffer applied = transformer.Apply8BitGammaTable(bgra, identity8);

        Assert.Multiple(() =>
        {
            Assert.That(identity8, Has.Length.EqualTo(256));
            Assert.That(identity8[0], Is.EqualTo(0));
            Assert.That(identity8[255], Is.EqualTo(255));
            Assert.That(identity16, Has.Length.EqualTo(65536));
            Assert.That(identity16[0], Is.EqualTo(0));
            Assert.That(identity16[65535], Is.EqualTo(65535));
            Assert.That(corrected8[0], Is.EqualTo(0));
            Assert.That(corrected8[255], Is.EqualTo(255));
            Assert.That(corrected16[0], Is.EqualTo(0));
            Assert.That(corrected16[65535], Is.EqualTo(65535));
            Assert.That(applied.Format, Is.EqualTo(PngPixelFormat.Bgra8));
            Assert.That(applied.Data, Is.EqualTo(bgra.Data));
        });
    }

    [Test]
    public void PublicTransformer_HandlesGeometryBoundariesAndChannelOrder()
    {
        PngPixelBuffer source = new PngImageFactory().CreateImageWithStride(
            2,
            2,
            PngPixelFormat.Bgra8,
            10,
            new byte[]
            {
                3, 2, 1, 4, 7, 6, 5, 8, 90, 91,
                11, 10, 9, 12, 15, 14, 13, 16
            }).Pixels;
        IPngTransformer transformer = new PngTransformer();

        PngPixelBuffer cropped = transformer.Crop(source, 1, 1, 1, 1);
        PngPixelBuffer padded = transformer.PadCanvas(cropped, 1, 1, 0, 0);
        PngPixelBuffer rgba = transformer.SwapRgbBgr(source);
        PngPixelBuffer restored = transformer.SwapRgbBgr(rgba);
        PngPixelBuffer destination = new PngImageFactory().CreateImage(2, 2, PngPixelFormat.Bgra8).Pixels;
        transformer.CopyRectangle(source, 1, 1, destination, 0, 0, 1, 1);

        Assert.That(cropped.Data, Is.EqualTo(new byte[] { 15, 14, 13, 16 }));
        Assert.That(padded.Width, Is.EqualTo(2));
        Assert.That(padded.Height, Is.EqualTo(2));
        Assert.That(padded.Data, Is.EqualTo(new byte[]
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 15, 14, 13, 16
        }));
        Assert.That(rgba.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(rgba.Data.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        Assert.That(restored.Format, Is.EqualTo(PngPixelFormat.Bgra8));
        Assert.That(restored.Data, Is.EqualTo(Join(source.Data.AsSpan(0, 8).ToArray(), source.Data.AsSpan(10, 8).ToArray())));
        Assert.That(destination.Data.AsSpan(0, 4).ToArray(), Is.EqualTo(new byte[] { 15, 14, 13, 16 }));
    }

    [Test]
    public void PublicTransformer_EnforcesPaletteCapacityAtExactBoundary()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer exact = new PngImageFactory().CreateImageFromSpan(256, 1, PngPixelFormat.Rgb8, CreateRgbRamp(256)).Pixels;
        PngPixelBuffer over = new PngImageFactory().CreateImageFromSpan(257, 1, PngPixelFormat.Rgb8, CreateRgbRamp(257)).Pixels;

        PngPalette palette = transformer.GeneratePalette(exact);

        Assert.That(palette.Entries, Has.Length.EqualTo(256));
        Assert.Throws<InvalidDataException>(() => transformer.GeneratePalette(over));
    }

    [Test]
    public void PublicFactoryAndTransformer_RejectInvalidOverflowAndAllocationRequests()
    {
        PngImageFactory factory = new PngImageFactory();
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer valid = factory.CreateImage(1, 1, PngPixelFormat.Rgba16).Pixels;
        PngPixelConversionOptions invalidTarget = new PngPixelConversionOptions();
        invalidTarget.TargetFormat = PngPixelFormat.Unknown;

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateImage(0, 1, PngPixelFormat.Rgba8));
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateImageWithStride(1, 2, PngPixelFormat.Rgba8, 3, new byte[8]));
        Assert.Throws<OverflowException>(() => factory.CreateImage(int.MaxValue, 1, PngPixelFormat.Rgba16));
        Assert.Throws<OverflowException>(() => factory.CreateImageWithStride(1, int.MaxValue, PngPixelFormat.Rgba8, int.MaxValue, new byte[4]));
        Assert.Throws<ArgumentOutOfRangeException>(() => transformer.ConvertPixelFormat(valid, invalidTarget));
        Assert.Throws<ArgumentOutOfRangeException>(() => transformer.Crop(valid, 1, 0, 1, 1));
        Assert.Throws<OverflowException>(() => transformer.PadCanvas(valid, int.MaxValue, 0, 0, 0));
        Assert.Throws<OverflowException>(() => transformer.ResizeNearestNeighbor(valid, int.MaxValue, 1));
        Assert.Throws<OverflowException>(() => transformer.ResizeBilinear(valid, int.MaxValue, 1));
    }

    static IEnumerable<TestCaseData> CreatePixelFormatCases()
    {
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Grayscale1,
            PngPixelFormat.Grayscale2,
            PngPixelFormat.Grayscale4,
            PngPixelFormat.Grayscale8,
            PngPixelFormat.Grayscale16,
            PngPixelFormat.Indexed1,
            PngPixelFormat.Indexed2,
            PngPixelFormat.Indexed4,
            PngPixelFormat.Indexed8,
            PngPixelFormat.Rgb8,
            PngPixelFormat.Rgb16,
            PngPixelFormat.GrayscaleAlpha8,
            PngPixelFormat.GrayscaleAlpha16,
            PngPixelFormat.Rgba8,
            PngPixelFormat.Rgba16,
            PngPixelFormat.Bgr8,
            PngPixelFormat.Bgr16,
            PngPixelFormat.Bgra8,
            PngPixelFormat.Bgra16,
            PngPixelFormat.Argb8,
            PngPixelFormat.Argb16
        };
        int index = 0;
        while (index < formats.Length)
        {
            yield return new TestCaseData(formats[index]).SetName("Public padded " + formats[index]);
            index += 1;
        }
    }

    static PngBitDepth GetBitDepth(int bits)
    {
        if (bits == 1)
        {
            return PngBitDepth.One;
        }

        if (bits == 2)
        {
            return PngBitDepth.Two;
        }

        return PngBitDepth.Four;
    }

    static bool IsIndexed(PngPixelFormat format)
    {
        return format == PngPixelFormat.Indexed1 || format == PngPixelFormat.Indexed2 || format == PngPixelFormat.Indexed4;
    }

    static PngPalette CreatePalette(int count)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[count];
        int index = 0;
        while (index < count)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)index;
            entry.Green = (byte)(index * 3);
            entry.Blue = (byte)(index * 7);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static byte[] CreateRgbRamp(int count)
    {
        byte[] data = new byte[count * 3];
        int index = 0;
        while (index < count)
        {
            data[index * 3] = (byte)index;
            data[index * 3 + 1] = (byte)(index >> 8);
            data[index * 3 + 2] = (byte)(255 - (index & 0xFF));
            index += 1;
        }

        return data;
    }

    static void Fill(byte[] data, int offset, int length, byte value)
    {
        int index = 0;
        while (index < length)
        {
            data[offset + index] = value;
            index += 1;
        }
    }

    static byte[] Filled(int length, byte value)
    {
        byte[] data = new byte[length];
        Fill(data, 0, length, value);
        return data;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
