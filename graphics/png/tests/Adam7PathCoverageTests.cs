using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class Adam7PathCoverageTests
{
    [Test]
    public void Geometry_CoversEveryPixelExactlyOnceForOddDimensions()
    {
        int width = 9;
        int height = 7;
        bool[] seen = new bool[width * height];
        int coveredPixels = 0;

        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(width, height, PngColorType.TruecolorAlpha, PngBitDepth.Eight);

        int passIndex = 0;
        while (passIndex < passes.Length)
        {
            PngAdam7PassGeometry pass = passes[passIndex];
            int passRow = 0;
            while (passRow < pass.Height)
            {
                int y = PngAdam7PassGeometryCalculator.GetPixelY(pass.PassIndex, passRow);
                Assert.That(y, Is.InRange(0, height - 1));

                int passColumn = 0;
                while (passColumn < pass.Width)
                {
                    int x = PngAdam7PassGeometryCalculator.GetPixelX(pass.PassIndex, passColumn);
                    Assert.That(x, Is.InRange(0, width - 1));

                    int pixelIndex = y * width + x;
                    Assert.That(seen[pixelIndex], Is.False);
                    seen[pixelIndex] = true;
                    coveredPixels += 1;
                    passColumn += 1;
                }

                passRow += 1;
            }

            passIndex += 1;
        }

        Assert.That(coveredPixels, Is.EqualTo(width * height));
        int index = 0;
        while (index < seen.Length)
        {
            Assert.That(seen[index], Is.True);
            index += 1;
        }
    }

    [TestCaseSource(nameof(CreateFixedFilterCases))]
    public void EncodeAndDecode_RoundTripsEveryFixedFilter(PngFilterType filterType)
    {
        PngIhdr ihdr = CreateIhdr(9, 7, PngColorType.Grayscale, PngBitDepth.Eight);
        PngEncoderOptions options = CreateFixedOptions(filterType);
        byte[] imageData = CreateBytePattern(ihdr.Width * ihdr.Height, 17, 29);

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);
        byte[] decoded = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(decoded, Is.EqualTo(imageData));
    }

    [TestCaseSource(nameof(CreatePackedRoundTripCases))]
    public void EncodeAndDecode_RoundTripsPackedBitDepths(PngColorType colorType, PngBitDepth bitDepth, byte[] imageData)
    {
        PngIhdr ihdr = CreateIhdr(9, 5, colorType, bitDepth);
        PngEncoderOptions options = CreateFixedOptions(PngFilterType.None);

        byte[] filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);
        byte[] decoded = PngAdam7Decoder.Decode(filteredScanlines, ihdr);

        Assert.That(decoded, Is.EqualTo(imageData));
    }

    static IEnumerable<TestCaseData> CreateFixedFilterCases()
    {
        yield return CreateFixedFilterCase(PngFilterType.None);
        yield return CreateFixedFilterCase(PngFilterType.Sub);
        yield return CreateFixedFilterCase(PngFilterType.Up);
        yield return CreateFixedFilterCase(PngFilterType.Average);
        yield return CreateFixedFilterCase(PngFilterType.Paeth);
    }

    static TestCaseData CreateFixedFilterCase(PngFilterType filterType)
    {
        return new TestCaseData(filterType).SetName(filterType.ToString());
    }

    static IEnumerable<TestCaseData> CreatePackedRoundTripCases()
    {
        yield return new TestCaseData(
            PngColorType.Grayscale,
            PngBitDepth.Two,
            new byte[] { 0x1B, 0x1B, 0x00, 0x6C, 0x6C, 0x40, 0xB1, 0xB1, 0x80, 0xC6, 0xC6, 0xC0, 0x27, 0xD8, 0x80 }).SetName("grayscale 2-bit");
        yield return new TestCaseData(
            PngColorType.IndexedColor,
            PngBitDepth.Four,
            new byte[] { 0x01, 0x23, 0x45, 0x67, 0x80, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0xFE, 0xDC, 0xBA, 0x98, 0x70, 0x10, 0x32, 0x54, 0x76, 0x90, 0x0F, 0x1E, 0x2D, 0x3C, 0x40 }).SetName("indexed 4-bit");
    }

    static byte[] CreateBytePattern(int length, int multiplier, int increment)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)((index * multiplier + increment) & 255);
            index += 1;
        }

        return data;
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
        ihdr.InterlaceMethod = PngInterlaceMethod.Adam7;
        return ihdr;
    }
}
