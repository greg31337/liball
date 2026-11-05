using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngOptimizationWorkflowTests
{
    [Test]
    public void OptimizeBytes_AppliesReductionsMetadataPolicyAndBothTrialFamilies()
    {
        byte[] input = CreateReduciblePng();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 6;
        options.MaximumCompressionLevel = 9;
        options.MetadataPolicy = PngMetadataPolicy.Strip;
        options.UseEarlyBailout = false;
        IPngOptimizer optimizer = new PngOptimizationService();

        PngOptimizerReport report = optimizer.OptimizeBytes(input, options);
        PngValidationResult validation = Png.ValidateBytes(report.BestEncodedBytes);
        PngIdentifyResult identified = Png.IdentifyBytes(report.BestEncodedBytes);
        PngDecoderOptions rgba16 = new PngDecoderOptions();
        rgba16.PreferredPixelFormat = PngPixelFormat.Rgba16;
        PngImage sourcePixels = Png.DecodeBytes(input, rgba16);
        PngImage optimizedPixels = Png.DecodeBytes(report.BestEncodedBytes, rgba16);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(report.FoundBestResult, Is.True);
        Assert.That(report.BestIsOriginal, Is.False);
        Assert.That(report.BestByteCount, Is.EqualTo(report.BestEncodedBytes.Length));
        Assert.That(report.ByteSavings, Is.EqualTo(input.Length - report.BestByteCount));
        Assert.That(report.ByteSavings, Is.GreaterThan(0));
        Assert.That(report.CompressionResult.Trials, Has.Length.EqualTo(4));
        Assert.That(report.FilterStrategyResult.Trials, Has.Length.EqualTo(7));
        Assert.That(report.OriginalColorType, Is.EqualTo(PngColorType.TruecolorAlpha));
        Assert.That(report.OriginalBitDepth, Is.EqualTo(PngBitDepth.Sixteen));
        Assert.That(report.OptimizedColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(report.OptimizedBitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(identified.Metadata.TextEntryCount, Is.EqualTo(0));
        Assert.That(optimizedPixels.Pixels.Data, Is.EqualTo(sourcePixels.Pixels.Data));
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_COLOR_TYPE_REDUCTION", true, true);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_BIT_DEPTH_REDUCTION", true, true);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_PALETTE_REDUCTION", true, false);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_METADATA_SELECTION", true, true);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_FILTER_TRIALS", true, true);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_COMPRESSION_TRIALS", true, true);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_EARLY_BAILOUT", false, false);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_RESULT_SELECTION", true, true);
    }

    [Test]
    public void OptimizeBytes_HonorsDisabledColorAndBitDepthReductions()
    {
        byte[] input = CreateUncompressedRgbPng();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.MaximumCompressionLevel = 9;
        options.TryCompressionLevels = false;
        options.TryFilters = false;
        options.AllowColorTypeReduction = false;
        options.AllowBitDepthReduction = false;
        options.UseEarlyBailout = false;

        PngOptimizerReport report = new PngOptimizationService().OptimizeBytes(input, options);

        Assert.That(report.BestIsOriginal, Is.False);
        Assert.That(report.OptimizedColorType, Is.EqualTo(PngColorType.Truecolor));
        Assert.That(report.OptimizedBitDepth, Is.EqualTo(PngBitDepth.Sixteen));
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_COLOR_TYPE_REDUCTION", false, false);
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_BIT_DEPTH_REDUCTION", false, false);
    }

    [Test]
    public void OptimizeBytes_TrimsUnusedPaletteEntriesWhenPermitted()
    {
        byte[] input = CreateIndexedPng();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 9;
        options.MaximumCompressionLevel = 9;
        options.TryCompressionLevels = false;
        options.TryFilters = false;
        options.AllowColorTypeReduction = false;
        options.AllowBitDepthReduction = false;
        options.AllowPaletteReduction = true;
        options.UseEarlyBailout = false;

        PngOptimizerReport report = new PngOptimizationService().OptimizeBytes(input, options);

        Assert.That(report.OriginalPaletteEntryCount, Is.EqualTo(5));
        Assert.That(report.OptimizedPaletteEntryCount, Is.EqualTo(3));
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_PALETTE_REDUCTION", true, true);
    }

    [Test]
    public void OptimizeBytes_UsesRealEarlyBailoutAndFallsBackToOriginal()
    {
        byte[] input = CreateUncompressedRgbPng();
        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 0;
        options.MaximumCompressionLevel = 3;
        options.BestKnownByteCount = 1;
        options.UseEarlyBailout = true;

        PngOptimizerReport report = new PngOptimizationService().OptimizeBytes(input, options);

        Assert.That(report.BailedOutTrialCount, Is.GreaterThan(0));
        Assert.That(report.BestIsOriginal, Is.True);
        Assert.That(report.BestEncodedBytes, Is.EqualTo(input));
        AssertDecision(report.Decisions, "PNG_OPTIMIZE_EARLY_BAILOUT", true, true);
        Assert.That(HasProducedPartialBailout(report.CompressionResult.Trials), Is.True);
    }

    [Test]
    public void Encoder_EarlyBailoutStopsBeforeReturningACompleteDatastream()
    {
        PngImage image = CreateRgb16Image(64, 64);
        PngEncoderOptions options = CreateExplicitRgb16Options(9);
        options.EarlyBailoutByteCount = 20;
        PngEncoderOptions streamOptions = CreateExplicitRgb16Options(9);
        streamOptions.EarlyBailoutByteCount = 20;
        PngEncoderOptions invalidOptions = CreateExplicitRgb16Options(9);
        invalidOptions.EarlyBailoutByteCount = -1;
        MemoryStream stream = new MemoryStream();

        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        new PngCodec().EncodeToStream(stream, image, streamOptions);

        Assert.That(encoded, Is.Empty);
        Assert.That(options.WasEarlyBailedOut, Is.True);
        Assert.That(options.ProducedByteCount, Is.GreaterThan(options.EarlyBailoutByteCount));
        Assert.That(streamOptions.WasEarlyBailedOut, Is.True);
        Assert.That(stream.Length, Is.LessThanOrEqualTo(streamOptions.EarlyBailoutByteCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngCodec().EncodeToBytes(image, invalidOptions));
    }

    [Test]
    public void OptimizeBytes_RejectsInvalidArgumentsAndDatastreams()
    {
        PngOptimizationService optimizer = new PngOptimizationService();
        PngOptimizerOptions options = new PngOptimizerOptions();
        PngOptimizerOptions nullLimits = new PngOptimizerOptions();
        nullLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => optimizer.OptimizeBytes(null!, options));
        Assert.Throws<ArgumentNullException>(() => optimizer.OptimizeBytes(new byte[0], null!));
        Assert.Throws<ArgumentNullException>(() => optimizer.OptimizeBytes(new byte[0], nullLimits));
        Assert.Throws<InvalidDataException>(() => optimizer.OptimizeBytes(new byte[] { 1, 2, 3 }, options));
    }

    static byte[] CreateReduciblePng()
    {
        PngImage image = CreateRgba16GrayscaleImage(24, 16);
        image.Metadata.Chunks = new PngChunk[] { PngTextWriter.WriteChunk(CreateText("Comment", CreateRepeatedText(200))) };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Sixteen;
        options.CompressionLevel = 0;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return new PngCodec().EncodeToBytes(image, options);
    }

    static byte[] CreateUncompressedRgbPng()
    {
        PngImage image = CreateRgb16Image(32, 16);
        return new PngCodec().EncodeToBytes(image, CreateExplicitRgb16Options(0));
    }

    static byte[] CreateIndexedPng()
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Indexed4;
        pixels.Width = 4;
        pixels.Height = 1;
        pixels.Stride = 2;
        pixels.Data = new byte[] { 0x12, 0x20 };
        PngImage image = new PngImage();
        image.Pixels = pixels;
        image.Metadata.Palette = CreatePalette(5);
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.IndexedColor;
        options.BitDepth = PngBitDepth.Four;
        options.AllowPaletteReduction = false;
        options.CompressionLevel = 0;
        return new PngCodec().EncodeToBytes(image, options);
    }

    static PngImage CreateRgba16GrayscaleImage(int width, int height)
    {
        byte[] data = new byte[width * height * 8];
        int pixel = 0;
        while (pixel < width * height)
        {
            byte value = (byte)(pixel % 31 + 16);
            int offset = pixel * 8;
            data[offset] = value;
            data[offset + 1] = value;
            data[offset + 2] = value;
            data[offset + 3] = value;
            data[offset + 4] = value;
            data[offset + 5] = value;
            data[offset + 6] = 255;
            data[offset + 7] = 255;
            pixel += 1;
        }

        return CreateImage(PngPixelFormat.Rgba16, width, height, width * 8, data);
    }

    static PngImage CreateRgb16Image(int width, int height)
    {
        byte[] data = new byte[width * height * 6];
        int pixel = 0;
        while (pixel < width * height)
        {
            int offset = pixel * 6;
            data[offset] = (byte)(pixel * 17 + 3);
            data[offset + 1] = (byte)(pixel * 29 + 5);
            data[offset + 2] = (byte)(pixel * 31 + 7);
            data[offset + 3] = (byte)(pixel * 37 + 11);
            data[offset + 4] = (byte)(pixel * 41 + 13);
            data[offset + 5] = (byte)(pixel * 43 + 17);
            pixel += 1;
        }

        return CreateImage(PngPixelFormat.Rgb16, width, height, width * 6, data);
    }

    static PngImage CreateImage(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = format;
        pixels.Width = width;
        pixels.Height = height;
        pixels.Stride = stride;
        pixels.Data = data;
        PngImage image = new PngImage();
        image.Pixels = pixels;
        return image;
    }

    static PngEncoderOptions CreateExplicitRgb16Options(int compressionLevel)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.Truecolor;
        options.BitDepth = PngBitDepth.Sixteen;
        options.CompressionLevel = compressionLevel;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngPalette CreatePalette(int count)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[count];
        int index = 0;
        while (index < count)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)(index * 30);
            entry.Green = (byte)(index * 30 + 1);
            entry.Blue = (byte)(index * 30 + 2);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static PngText CreateText(string keyword, string value)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = value;
        return text;
    }

    static string CreateRepeatedText(int length)
    {
        char[] characters = new char[length];
        int index = 0;
        while (index < length)
        {
            characters[index] = 'x';
            index += 1;
        }

        return new string(characters);
    }

    static bool HasProducedPartialBailout(PngCompressionTrial[] trials)
    {
        int index = 0;
        while (index < trials.Length)
        {
            if (trials[index].WasBailedOut && trials[index].EncodedBytes.Length == 0 && trials[index].ByteCount > 1)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void AssertDecision(PngOptimizationDecision[] decisions, string code, bool enabled, bool applied)
    {
        int index = 0;
        while (index < decisions.Length)
        {
            if (decisions[index].Code == code)
            {
                Assert.That(decisions[index].Enabled, Is.EqualTo(enabled));
                Assert.That(decisions[index].Applied, Is.EqualTo(applied));
                return;
            }

            index += 1;
        }

        Assert.Fail("Missing optimization decision " + code + ".");
    }
}
