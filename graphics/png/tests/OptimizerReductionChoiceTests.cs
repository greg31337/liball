using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class OptimizerReductionChoiceTests
{
    [Test]
    public void CompressionTrials_ChooseSmallerCompressedOutputThatRoundTrips()
    {
        PngImage image = CreateImage(CreateRepeatedRgba8Buffer(16, 16));
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 0;
        options.MaximumCompressionLevel = 9;
        options.TryFilters = false;
        options.FixedFilterType = PngFilterType.None;
        options.UseEarlyBailout = false;

        PngCompressionTrialResult result = PngCompressionLevelTrialRunner.Run(image, options);
        PngCompressionTrial noCompression = FindTrial(result.Trials, 0);
        PngImage decodedBest = DecodeAs(result.BestTrial.EncodedBytes, image.Pixels.Format);

        Assert.That(result.TriedCompressionLevels, Is.True);
        Assert.That(result.BestTrial.IsBest, Is.True);
        Assert.That(result.BestTrial.ByteCount, Is.LessThan(noCompression.ByteCount));
        AssertImage(decodedBest, image.Pixels);
    }

    [Test]
    public void FilterStrategyTrials_ChooseSmallestFilterOutputThatRoundTrips()
    {
        PngImage image = CreateImage(CreateGradientRgba8Buffer());
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.UseEarlyBailout = false;

        PngFilterStrategyTrialResult result = PngFilterStrategyTrialRunner.Run(image, options);
        PngImage decodedBest = DecodeAs(result.BestTrial.EncodedBytes, image.Pixels.Format);

        Assert.That(result.TriedFilterStrategies, Is.True);
        Assert.That(result.BestTrial.IsBest, Is.True);
        Assert.That(result.BestTrial.ByteCount, Is.EqualTo(FindSmallestByteCount(result.Trials)));
        Assert.That(CountFixedTrials(result.Trials), Is.EqualTo(5));
        AssertImage(decodedBest, image.Pixels);
    }

    [Test]
    public void ReductionHelpers_CreateEncodableGrayscaleAndTrimmedPaletteOutputs()
    {
        PngPixelBuffer grayRgb = CreateBuffer(PngPixelFormat.Rgb8, 3, 2, 10, new byte[]
        {
            1, 1, 1, 2, 2, 2, 3, 3, 3, 99,
            4, 4, 4, 5, 5, 5, 6, 6, 6, 88
        });
        PngPixelBuffer grayscale = PngRgbToGrayscaleConverter.ConvertIfGrayscale(grayRgb);
        byte[] grayscalePng = Png.EncodeToBytes(CreateImage(grayscale));
        PngImage decodedGrayscale = Png.DecodeBytes(grayscalePng);

        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed4, 4, 1, 2, new byte[] { 0x12, 0x20 });
        PngPalette trimmedPalette = PngPaletteTrimmer.Trim(indexed, CreatePalette(5));
        PngImage indexedImage = CreateImage(indexed);
        indexedImage.Metadata.Palette = trimmedPalette;
        byte[] indexedPng = Png.EncodeToBytes(indexedImage, CreateIndexed4Options());
        PngImage decodedIndexed = Png.DecodeBytes(indexedPng);

        AssertImage(decodedGrayscale, grayscale);
        Assert.That(decodedGrayscale.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(decodedGrayscale.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(trimmedPalette.Entries, Has.Length.EqualTo(3));
        AssertImage(decodedIndexed, indexed);
        Assert.That(decodedIndexed.Metadata.Palette.Entries, Has.Length.EqualTo(3));
        AssertPaletteEntry(decodedIndexed.Metadata.Palette.Entries[2], 2, 3, 4);
    }

    static PngImage CreateImage(PngPixelBuffer pixels)
    {
        PngImage image = new PngImage();
        image.Pixels = pixels;
        return image;
    }

    static PngImage DecodeAs(byte[] data, PngPixelFormat format)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = format;
        return Png.DecodeBytes(data, options);
    }

    static PngPixelBuffer CreateRepeatedRgba8Buffer(int width, int height)
    {
        byte[] data = new byte[width * height * 4];
        int pixelIndex = 0;
        while (pixelIndex < width * height)
        {
            int offset = pixelIndex * 4;
            data[offset] = 10;
            data[offset + 1] = 20;
            data[offset + 2] = 30;
            data[offset + 3] = 255;
            pixelIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgba8, width, height, width * 4, data);
    }

    static PngPixelBuffer CreateGradientRgba8Buffer()
    {
        int width = 8;
        int height = 4;
        byte[] data = new byte[width * height * 4];
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int offset = (y * width + x) * 4;
                data[offset] = (byte)(x * 8 + y);
                data[offset + 1] = (byte)(x * 8 + y + 1);
                data[offset + 2] = (byte)(x * 8 + y + 2);
                data[offset + 3] = 255;
                x += 1;
            }

            y += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgba8, width, height, width * 4, data);
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

    static PngPalette CreatePalette(int entryCount)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];
        int index = 0;
        while (index < entryCount)
        {
            palette.Entries[index] = CreatePaletteEntry(index, index + 1, index + 2);
            index += 1;
        }

        return palette;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static PngEncoderOptions CreateIndexed4Options()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = PngColorType.IndexedColor;
        options.BitDepth = PngBitDepth.Four;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngCompressionTrial FindTrial(PngCompressionTrial[] trials, int compressionLevel)
    {
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].CompressionLevel == compressionLevel)
            {
                return trials[index];
            }

            index += 1;
        }

        Assert.Fail("Missing compression trial.");
        return trials[0];
    }

    static long FindSmallestByteCount(PngFilterStrategyTrial[] trials)
    {
        long smallest = trials[0].ByteCount;
        int index = 1;
        while (index < trials.Length)
        {
            if (trials[index].ByteCount < smallest)
            {
                smallest = trials[index].ByteCount;
            }

            index += 1;
        }

        return smallest;
    }

    static int CountFixedTrials(PngFilterStrategyTrial[] trials)
    {
        int count = 0;
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].UseFixedFilter)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static void AssertImage(PngImage image, PngPixelBuffer expected)
    {
        Assert.That(image.Pixels.Format, Is.EqualTo(expected.Format));
        Assert.That(image.Pixels.Width, Is.EqualTo(expected.Width));
        Assert.That(image.Pixels.Height, Is.EqualTo(expected.Height));
        Assert.That(image.Pixels.Stride, Is.EqualTo(expected.Width == 0 ? 0 : expected.Data.Length / expected.Height));
        Assert.That(image.Pixels.Data, Is.EqualTo(TightCopy(expected)));
    }

    static byte[] TightCopy(PngPixelBuffer buffer)
    {
        int rowBytes = buffer.Data.Length / buffer.Height;
        if (buffer.Stride == rowBytes)
        {
            byte[] copy = new byte[buffer.Data.Length];
            Array.Copy(buffer.Data, copy, buffer.Data.Length);
            return copy;
        }

        byte[] result = new byte[rowBytes * buffer.Height];
        int row = 0;
        while (row < buffer.Height)
        {
            Array.Copy(buffer.Data, row * buffer.Stride, result, row * rowBytes, rowBytes);
            row += 1;
        }

        return result;
    }

    static void AssertPaletteEntry(PngPaletteEntry entry, int red, int green, int blue)
    {
        Assert.That(entry.Red, Is.EqualTo(red));
        Assert.That(entry.Green, Is.EqualTo(green));
        Assert.That(entry.Blue, Is.EqualTo(blue));
    }
}
