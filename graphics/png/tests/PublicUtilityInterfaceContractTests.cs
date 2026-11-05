using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicUtilityInterfaceContractTests
{
    [Test]
    public void RepairContract_ExercisesEveryInputAndRepairVariant()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 1, 2 }, png);
        IPngChunkCodec chunkCodec = new PngChunkService();
        IPngRepairer repairer = new PngRepairService();
        PngRepairOptions options = new PngRepairOptions();
        PngChunk[] chunks = chunkCodec.ReadChunks(png).Chunks;

        PngRepairResult bytes = repairer.RepairBytes(png, options);
        PngRepairResult offset = repairer.RepairBytesAtOffset(embedded, 2, new PngRepairOptions());
        MemoryStream repairStream = new MemoryStream(embedded);
        repairStream.Position = 2;
        PngRepairResult stream = repairer.RepairStream(repairStream, new PngRepairOptions());
        PngRepairResult chunkResult = repairer.RepairChunks(chunks, new PngRepairOptions());

        Assert.That(bytes.Data, Is.EqualTo(png));
        Assert.That(offset.Data, Is.EqualTo(png));
        Assert.That(stream.Data, Is.EqualTo(png));
        Assert.That(chunkResult.Data, Is.EqualTo(png));
        Assert.That(repairStream.Position, Is.EqualTo(embedded.Length));

        Assert.That(repairer.RepairAncillaryOrderingBytes(png), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryOrderingBytesAtOffset(embedded, 2), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryOrderingStream(new MemoryStream(png)), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryOrderingChunks(chunks).Select(chunk => chunk.Type), Is.EqualTo(chunks.Select(chunk => chunk.Type)));
        Assert.That(repairer.RepairAncillaryCrcBytes(png), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryCrcBytesAtOffset(embedded, 2), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryCrcStream(new MemoryStream(png)), Is.EqualTo(png));
        Assert.That(repairer.RepairAncillaryCrcChunks(chunks).Select(chunk => chunk.Crc), Is.EqualTo(chunks.Select(chunk => chunk.Crc)));

        Assert.Throws<ArgumentOutOfRangeException>(() => repairer.RepairBytesAtOffset(png, -1, new PngRepairOptions()));
        Assert.Throws<ArgumentNullException>(() => repairer.RepairStream(null!, new PngRepairOptions()));
    }

    [Test]
    public void CrcAndMetadataContracts_ExerciseEveryConvenienceVariant()
    {
        IPngCrcCalculator crc = new PngCrcCalculator();
        byte[] data = new byte[] { 99, 1, 2, 3, 4, 88 };
        byte[] payload = new byte[] { 1, 2, 3, 4 };

        Assert.That(crc.CalculateRange(data, 1, 4), Is.EqualTo(crc.Calculate(payload)));
        Assert.That(crc.CalculateChunkFromName(PngConstants.ChunkText, payload), Is.EqualTo(crc.CalculateChunkFromNameSpan(PngConstants.ChunkText, payload)));
        Assert.That(crc.CalculateChunkFromBytes(new byte[] { 116, 69, 88, 116 }, payload), Is.EqualTo(crc.CalculateChunkFromName(PngConstants.ChunkText, payload)));

        IPngMetadataEditor editor = new PngMetadataService();
        PngMetadata metadata = new PngMetadata();
        metadata = editor.SetCodingIndependentCodePointValues(metadata, 1, 2, 3, 1);
        metadata = editor.SetGammaValue(metadata, 45455);
        metadata = editor.SetStandardRgbIntent(metadata, 2);
        metadata = editor.SetPhysicalDimensionValues(metadata, 3780, 3780, 1);
        PngText text = new PngText();
        text.Keyword = "Title";
        text.Text = "Contract";
        metadata = editor.ReplaceTextModels(metadata, new PngText[] { text });

        Assert.That(metadata.CodingIndependentCodePoints!.ColorPrimaries, Is.EqualTo(1));
        Assert.That(metadata.Gamma!.Value, Is.EqualTo(45455));
        Assert.That(metadata.StandardRgb!.RenderingIntent, Is.EqualTo(2));
        Assert.That(metadata.PhysicalPixels!.PixelsPerUnitX, Is.EqualTo(3780));
        Assert.That(metadata.Latin1TextEntries, Has.Length.EqualTo(1));

        PngChunk custom = new PngChunk();
        custom.Type = "vpAg";
        custom.Data = new byte[] { 1 };
        metadata.Chunks = new PngChunk[] { custom };
        PngMetadata removed = editor.RemoveChunk(metadata, "vpAg");
        Assert.That(removed.Chunks, Is.Empty);

        IPngColorMetadataService colorService = new PngColorMetadataService();
        PngColorMetadata color = colorService.ResolveColorMetadataWithLimits(removed, new PngSecurityLimits());
        Assert.That(color.HasCodingIndependentCodePoints, Is.True);
        Assert.That(color.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
    }

    [Test]
    public void TransformerContract_ExercisesPaletteTransparencyMonochromeAndGammaVariants()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer indexed = CreateBuffer(PngPixelFormat.Indexed8, 2, 1, 2, new byte[] { 0, 1 });
        PngPalette palette = CreatePalette();
        PngChunk paletteChunk = CreateChunk(PngConstants.ChunkPLTE, new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
        PngIndexedTransparency indexedTransparency = new PngIndexedTransparency();
        indexedTransparency.Alpha = new byte[] { 255, 0 };
        PngChunk indexedTransparencyChunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 255, 0 });

        Assert.That(transformer.ExpandPaletteToRgb8(indexed, palette).Data, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        Assert.That(transformer.ExpandPaletteChunkToRgb8(indexed, paletteChunk).Data, Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60 }));
        Assert.That(transformer.ExpandIndexedTransparency(indexed, palette, indexedTransparency).Data, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 0 }));
        Assert.That(transformer.ExpandIndexedTransparencyChunks(indexed, paletteChunk, indexedTransparencyChunk).Data, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 0 }));
        Assert.That(transformer.TrimPalette(indexed, palette).Entries, Has.Length.EqualTo(2));

        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale8, 2, 1, 2, new byte[] { 7, 8 });
        PngGrayscaleTransparency grayscaleTransparency = new PngGrayscaleTransparency();
        grayscaleTransparency.Gray = 7;
        PngChunk grayscaleTransparencyChunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 7 });
        Assert.That(transformer.ExpandGrayscaleTransparency(grayscale, grayscaleTransparency).Data, Is.EqualTo(new byte[] { 7, 0, 8, 255 }));
        Assert.That(transformer.ExpandGrayscaleTransparencyChunk(grayscale, grayscaleTransparencyChunk).Data, Is.EqualTo(new byte[] { 7, 0, 8, 255 }));

        PngPixelBuffer truecolor = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 10, 20, 30 });
        PngChunk truecolorTransparencyChunk = CreateChunk(PngConstants.ChunkTransparency, new byte[] { 0, 10, 0, 20, 0, 30 });
        Assert.That(transformer.ExpandTruecolorTransparencyChunk(truecolor, truecolorTransparencyChunk).Data, Is.EqualTo(new byte[] { 10, 20, 30, 0 }));

        PngPixelBuffer monochrome = CreateBuffer(PngPixelFormat.Grayscale1, 2, 1, 1, new byte[] { 0x80 });
        Assert.That(transformer.InvertMonochrome(monochrome).Data, Is.EqualTo(new byte[] { 0x40 }));

        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        Assert.That(transformer.Build8BitGammaTable(gamma, 2.2), Has.Length.EqualTo(256));
        Assert.That(transformer.Build8BitGammaTableFromValue(45455, 2.2), Has.Length.EqualTo(256));
        Assert.That(transformer.Build8BitGammaTableFromExponent(0.45455, 2.2), Has.Length.EqualTo(256));
        Assert.That(transformer.Build8BitGammaTableWithUserExponent(0.45455, 2.2, 1.0), Has.Length.EqualTo(256));
        Assert.That(transformer.Build16BitGammaTable(gamma, 2.2), Has.Length.EqualTo(65536));
        Assert.That(transformer.Build16BitGammaTableFromValue(45455, 2.2), Has.Length.EqualTo(65536));
        Assert.That(transformer.Build16BitGammaTableFromExponent(0.45455, 2.2), Has.Length.EqualTo(65536));
        Assert.That(transformer.Build16BitGammaTableWithUserExponent(0.45455, 2.2, 1.0), Has.Length.EqualTo(65536));
    }

    [Test]
    public void OptimizerContract_ExercisesDefaultConfiguredInjectedAndReportVariants()
    {
        IPngImageFactory factory = new PngImageFactory();
        PngImage image = factory.CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 255 });
        IPngOptimizer optimizer = new PngOptimizationService();
        PngCompressionTrialResult defaultCompression = optimizer.RunCompressionTrials(image);
        PngFilterStrategyTrialResult defaultFilters = optimizer.RunFilterTrials(image);

        PngOptimizerOptions options = new PngOptimizerOptions();
        options.MinimumCompressionLevel = 2;
        options.MaximumCompressionLevel = 3;
        options.UseEarlyBailout = false;
        ContractEncoder encoder = new ContractEncoder();
        PngCompressionTrialResult configuredCompression = optimizer.RunCompressionTrialsWithOptions(image, options);
        PngCompressionTrialResult injectedCompression = optimizer.RunCompressionTrialsWithEncoder(image, options, encoder);
        PngFilterStrategyTrialResult configuredFilters = optimizer.RunFilterTrialsWithOptions(image, options);
        PngFilterStrategyTrialResult injectedFilters = optimizer.RunFilterTrialsWithEncoder(image, options, encoder);

        Assert.That(defaultCompression.Trials, Has.Length.GreaterThan(0));
        Assert.That(defaultFilters.Trials, Has.Length.GreaterThan(0));
        Assert.That(configuredCompression.Trials, Has.Length.EqualTo(2));
        Assert.That(injectedCompression.Trials, Has.Length.EqualTo(2));
        Assert.That(configuredFilters.Trials, Has.Length.EqualTo(7));
        Assert.That(injectedFilters.Trials, Has.Length.EqualTo(7));
        Assert.That(encoder.CallCount, Is.EqualTo(9));

        PngOptimizerReport fromCount = optimizer.CreateReportFromByteCount(100, injectedCompression, injectedFilters);
        PngOptimizerReport fromBytes = optimizer.CreateReportFromBytes(new byte[100], injectedCompression, injectedFilters);
        Assert.That(fromCount.BestByteCount, Is.EqualTo(fromBytes.BestByteCount));
        Assert.That(fromCount.CompressionTrialCount, Is.EqualTo(2));
        Assert.That(fromCount.FilterStrategyTrialCount, Is.EqualTo(7));
    }

    static byte[] CreatePng()
    {
        IPngImageFactory factory = new PngImageFactory();
        IPngEncoder encoder = new PngCodec();
        PngImage image = factory.CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 255 });
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        return encoder.EncodeToBytes(image, options);
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

    static PngPalette CreatePalette()
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[3];
        int index = 0;
        while (index < palette.Entries.Length)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)(10 + index * 30);
            entry.Green = (byte)(20 + index * 30);
            entry.Blue = (byte)(30 + index * 30);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static byte[] Join(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }

        return result;
    }

    class ContractEncoder : IPngEncoder
    {
        public int CallCount;

        public byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
        {
            CallCount += 1;
            return new byte[50 - options.CompressionLevel - (int)options.FixedFilterType];
        }

        public void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
        {
            byte[] bytes = EncodeToBytes(image, options);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
