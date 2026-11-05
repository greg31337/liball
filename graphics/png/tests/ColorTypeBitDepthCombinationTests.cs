using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ColorTypeBitDepthCombinationTests
{
    [TestCaseSource(nameof(CreateValidCombinationCases))]
    public void NonInterlacedEncodeAndDecode_RoundTripsEveryValidCombination(PngColorType colorType, PngBitDepth bitDepth, PngPixelFormat pixelFormat)
    {
        PngIhdr ihdr = CreateIhdr(9, 2, colorType, bitDepth);
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        PngPixelBuffer buffer = CreateBuffer(pixelFormat, ihdr.Width, ihdr.Height, (int)layout.RowDataBytes, CreateImageData(layout));
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);

        byte[] filteredScanlines = PngNonInterlacedImageEncoder.Encode(buffer, ihdr, options);
        PngPixelBuffer decoded = PngNonInterlacedImageDecoder.Decode(filteredScanlines, ihdr);

        Assert.That(filteredScanlines.Length, Is.EqualTo((int)layout.FilteredImageBytes));
        Assert.That(decoded.Format, Is.EqualTo(pixelFormat));
        Assert.That(decoded.Width, Is.EqualTo(ihdr.Width));
        Assert.That(decoded.Height, Is.EqualTo(ihdr.Height));
        Assert.That(decoded.Stride, Is.EqualTo((int)layout.RowDataBytes));
        Assert.That(decoded.Data, Is.EqualTo(buffer.Data));
    }

    [TestCaseSource(nameof(CreateInvalidCombinationCases))]
    public void IhdrValidation_RejectsEveryInvalidKnownCombination(PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = CreateIhdr(1, 1, colorType, bitDepth);

        PngValidationResult result = PngIhdrValidator.Validate(ihdr);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_IHDR_BIT_DEPTH_COLOR_TYPE_INVALID"));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngNonInterlacedImageDecoder.GetNativePixelFormat(colorType, bitDepth));
    }

    static IEnumerable<TestCaseData> CreateValidCombinationCases()
    {
        yield return CreateValidCombinationCase(PngColorType.Grayscale, PngBitDepth.One, PngPixelFormat.Grayscale1);
        yield return CreateValidCombinationCase(PngColorType.Grayscale, PngBitDepth.Two, PngPixelFormat.Grayscale2);
        yield return CreateValidCombinationCase(PngColorType.Grayscale, PngBitDepth.Four, PngPixelFormat.Grayscale4);
        yield return CreateValidCombinationCase(PngColorType.Grayscale, PngBitDepth.Eight, PngPixelFormat.Grayscale8);
        yield return CreateValidCombinationCase(PngColorType.Grayscale, PngBitDepth.Sixteen, PngPixelFormat.Grayscale16);
        yield return CreateValidCombinationCase(PngColorType.Truecolor, PngBitDepth.Eight, PngPixelFormat.Rgb8);
        yield return CreateValidCombinationCase(PngColorType.Truecolor, PngBitDepth.Sixteen, PngPixelFormat.Rgb16);
        yield return CreateValidCombinationCase(PngColorType.IndexedColor, PngBitDepth.One, PngPixelFormat.Indexed1);
        yield return CreateValidCombinationCase(PngColorType.IndexedColor, PngBitDepth.Two, PngPixelFormat.Indexed2);
        yield return CreateValidCombinationCase(PngColorType.IndexedColor, PngBitDepth.Four, PngPixelFormat.Indexed4);
        yield return CreateValidCombinationCase(PngColorType.IndexedColor, PngBitDepth.Eight, PngPixelFormat.Indexed8);
        yield return CreateValidCombinationCase(PngColorType.GrayscaleAlpha, PngBitDepth.Eight, PngPixelFormat.GrayscaleAlpha8);
        yield return CreateValidCombinationCase(PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen, PngPixelFormat.GrayscaleAlpha16);
        yield return CreateValidCombinationCase(PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngPixelFormat.Rgba8);
        yield return CreateValidCombinationCase(PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, PngPixelFormat.Rgba16);
    }

    static IEnumerable<TestCaseData> CreateInvalidCombinationCases()
    {
        yield return CreateInvalidCombinationCase(PngColorType.Truecolor, PngBitDepth.One);
        yield return CreateInvalidCombinationCase(PngColorType.Truecolor, PngBitDepth.Two);
        yield return CreateInvalidCombinationCase(PngColorType.Truecolor, PngBitDepth.Four);
        yield return CreateInvalidCombinationCase(PngColorType.IndexedColor, PngBitDepth.Sixteen);
        yield return CreateInvalidCombinationCase(PngColorType.GrayscaleAlpha, PngBitDepth.One);
        yield return CreateInvalidCombinationCase(PngColorType.GrayscaleAlpha, PngBitDepth.Two);
        yield return CreateInvalidCombinationCase(PngColorType.GrayscaleAlpha, PngBitDepth.Four);
        yield return CreateInvalidCombinationCase(PngColorType.TruecolorAlpha, PngBitDepth.One);
        yield return CreateInvalidCombinationCase(PngColorType.TruecolorAlpha, PngBitDepth.Two);
        yield return CreateInvalidCombinationCase(PngColorType.TruecolorAlpha, PngBitDepth.Four);
    }

    static TestCaseData CreateValidCombinationCase(PngColorType colorType, PngBitDepth bitDepth, PngPixelFormat pixelFormat)
    {
        return new TestCaseData(colorType, bitDepth, pixelFormat).SetName(colorType.ToString() + " " + bitDepth.ToString());
    }

    static TestCaseData CreateInvalidCombinationCase(PngColorType colorType, PngBitDepth bitDepth)
    {
        return new TestCaseData(colorType, bitDepth).SetName(colorType.ToString() + " " + bitDepth.ToString());
    }

    static byte[] CreateImageData(PngScanlineLayout layout)
    {
        if (layout.BitDepth == PngBitDepth.One || layout.BitDepth == PngBitDepth.Two || layout.BitDepth == PngBitDepth.Four)
        {
            return CreatePackedImageData(layout);
        }

        byte[] data = new byte[(int)layout.ImageDataBytes];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)((index * 37 + 11) & 255);
            index += 1;
        }

        return data;
    }

    static byte[] CreatePackedImageData(PngScanlineLayout layout)
    {
        byte[] data = new byte[(int)layout.ImageDataBytes];
        int rowIndex = 0;
        while (rowIndex < layout.Height)
        {
            byte[] samples = CreatePackedSamples(layout.Width, rowIndex, layout.BitDepth);
            byte[] row = PngPackedSampleWriter.Pack(samples, 0, samples.Length, layout.BitDepth);
            CopyBytes(row, 0, data, rowIndex * (int)layout.RowDataBytes, row.Length);
            rowIndex += 1;
        }

        return data;
    }

    static byte[] CreatePackedSamples(int width, int rowIndex, PngBitDepth bitDepth)
    {
        int maximumSample = (1 << PngSizeCalculator.GetBitDepthValue(bitDepth)) - 1;
        byte[] samples = new byte[width];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (byte)((rowIndex * 3 + index) & maximumSample);
            index += 1;
        }

        return samples;
    }

    static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    static PngEncoderOptions CreateFixedOptions(PngFilterType filterType)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseFixedFilter = true;
        options.FixedFilterType = filterType;
        return options;
    }

    static PngIhdr CreateIhdr(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.ColorType = colorType;
        ihdr.BitDepth = bitDepth;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }
}
