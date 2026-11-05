namespace jpeg.Tests;

internal class ReadColorControlTests
{
    [Test]
    public void Read_ChromaUpsamplingControlSelectsBoxOrFancyReconstruction()
    {
        byte[] data = GoldenDecodeTests.CreateReadableYCbCr420PartialHeightRestartJpeg();
        global::jpeg.JpegReadOptions boxOptions = new global::jpeg.JpegReadOptions();
        boxOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        boxOptions.ChromaUpsamplingMode = global::jpeg.JpegChromaUpsamplingMode.Box;
        global::jpeg.JpegReadOptions fancyOptions = new global::jpeg.JpegReadOptions();
        fancyOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        fancyOptions.ChromaUpsamplingMode = global::jpeg.JpegChromaUpsamplingMode.Fancy;

        global::jpeg.JpegImage box = global::jpeg.Jpeg.Read(data, boxOptions);
        global::jpeg.JpegImage fancy = global::jpeg.Jpeg.Read(data, fancyOptions);

        Assert.That(box.Width, Is.EqualTo(fancy.Width));
        Assert.That(box.Height, Is.EqualTo(fancy.Height));
        Assert.That(fancy.Pixels, Is.Not.EqualTo(box.Pixels));
        Assert.That(GetPixelComponent(box, 15, 0, 1), Is.EqualTo(133));
        Assert.That(GetPixelComponent(fancy, 15, 0, 1), Is.LessThan(133));
    }

    [Test]
    public void Read_RawComponentOutputPreservesSubsampledPlanes()
    {
        byte[] data = GoldenDecodeTests.CreateReadableYCbCr420PartialHeightRestartJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.RawComponentOutput = true;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.RawComponents));
        Assert.That(image.BufferLayout, Is.EqualTo(global::jpeg.JpegBufferLayout.Planar));
        Assert.That(image.Pixels, Is.Empty);
        Assert.That(image.PlanarBuffer.Components.Count, Is.EqualTo(3));
        Assert.That(image.PlanarBuffer.Components[0].Width, Is.EqualTo(32));
        Assert.That(image.PlanarBuffer.Components[0].Height, Is.EqualTo(9));
        Assert.That(image.PlanarBuffer.Components[1].Width, Is.EqualTo(16));
        Assert.That(image.PlanarBuffer.Components[1].Height, Is.EqualTo(5));
        Assert.That(image.PlanarBuffer.Components[2].Width, Is.EqualTo(16));
        Assert.That(image.PlanarBuffer.Components[2].Height, Is.EqualTo(5));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
    }

    [Test]
    public void Read_RawComponentOutputSupportsRegionCropping()
    {
        byte[] data = GoldenDecodeTests.CreateReadableYCbCr420PartialHeightRestartJpeg();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.RawComponentOutput = true;
        options.DecodeRegion.X = 8;
        options.DecodeRegion.Y = 0;
        options.DecodeRegion.Width = 16;
        options.DecodeRegion.Height = 8;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.PlanarBuffer.Components[0].Width, Is.EqualTo(16));
        Assert.That(image.PlanarBuffer.Components[0].Height, Is.EqualTo(8));
        Assert.That(image.PlanarBuffer.Components[1].Width, Is.EqualTo(8));
        Assert.That(image.PlanarBuffer.Components[1].Height, Is.EqualTo(5));
        Assert.That(image.PlanarBuffer.Components[0].Samples[0], Is.EqualTo(143));
    }

    [Test]
    public void Read_UnknownColorControlReturnsRawOrRejects()
    {
        byte[] data = CreateConstantJpeg(2, new int[] { 80, 160 }, -1);
        global::jpeg.JpegReadOptions bestEffortOptions = new global::jpeg.JpegReadOptions();
        bestEffortOptions.UnknownColorHandling = global::jpeg.JpegUnknownColorHandling.BestEffort;
        global::jpeg.JpegReadOptions exactOptions = new global::jpeg.JpegReadOptions();
        exactOptions.UnknownColorHandling = global::jpeg.JpegUnknownColorHandling.Exact;

        global::jpeg.JpegImage bestEffort = global::jpeg.Jpeg.Read(data, bestEffortOptions);

        Assert.That(bestEffort.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Unknown));
        Assert.That(bestEffort.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.RawComponents));
        Assert.That(bestEffort.PlanarBuffer.Components[0].Samples[0], Is.EqualTo(78));
        Assert.That(bestEffort.PlanarBuffer.Components[1].Samples[0], Is.EqualTo(158));
        Assert.Throws<global::jpeg.JpegUnsupportedException>(() => global::jpeg.Jpeg.Read(data, exactOptions));
    }

    [Test]
    public void Read_AdobeInversionControlOverridesMarkerInterpretation()
    {
        byte[] data = CreateConstantJpeg(4, new int[] { 138, 138, 138, 138 }, global::jpeg.JpegConstants.AdobeTransformUnknown);
        global::jpeg.JpegReadOptions automaticOptions = new global::jpeg.JpegReadOptions();
        global::jpeg.JpegReadOptions invertedOptions = new global::jpeg.JpegReadOptions();
        invertedOptions.AdobeInversionMode = global::jpeg.JpegAdobeInversionMode.ForceInverted;
        global::jpeg.JpegReadOptions nonInvertedOptions = new global::jpeg.JpegReadOptions();
        nonInvertedOptions.AdobeInversionMode = global::jpeg.JpegAdobeInversionMode.ForceNonInverted;
        global::jpeg.JpegReadOptions strippedOptions = new global::jpeg.JpegReadOptions();
        strippedOptions.MetadataPolicy = global::jpeg.JpegMetadataPolicy.StripAll;

        global::jpeg.JpegImage automatic = global::jpeg.Jpeg.Read(data, automaticOptions);
        global::jpeg.JpegImage inverted = global::jpeg.Jpeg.Read(data, invertedOptions);
        global::jpeg.JpegImage nonInverted = global::jpeg.Jpeg.Read(data, nonInvertedOptions);
        global::jpeg.JpegImage stripped = global::jpeg.Jpeg.Read(data, strippedOptions);

        Assert.That(automatic.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(automatic.Pixels[0], Is.EqualTo(117));
        Assert.That(inverted.Pixels[0], Is.EqualTo(117));
        Assert.That(nonInverted.Pixels[0], Is.EqualTo(138));
        Assert.That(stripped.Pixels[0], Is.EqualTo(117));
        Assert.That(stripped.Metadata.HasAdobe, Is.False);
    }

    [Test]
    public void Read_YCbCrRangeControlExpandsLimitedRangeBeforeOutput()
    {
        byte[] data = CreateConstantJpeg(3, new int[] { 16, 128, 128 }, -1);
        global::jpeg.JpegReadOptions fullOptions = new global::jpeg.JpegReadOptions();
        fullOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        fullOptions.YCbCrRange = global::jpeg.JpegYCbCrRange.Full;
        global::jpeg.JpegReadOptions limitedOptions = new global::jpeg.JpegReadOptions();
        limitedOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        limitedOptions.YCbCrRange = global::jpeg.JpegYCbCrRange.Limited;

        global::jpeg.JpegImage full = global::jpeg.Jpeg.Read(data, fullOptions);
        global::jpeg.JpegImage limited = global::jpeg.Jpeg.Read(data, limitedOptions);

        Assert.That(full.Pixels[0], Is.EqualTo(18));
        Assert.That(full.Pixels[1], Is.EqualTo(128));
        Assert.That(full.Pixels[2], Is.EqualTo(128));
        Assert.That(limited.Pixels[0], Is.EqualTo(2));
        Assert.That(limited.Pixels[1], Is.EqualTo(128));
        Assert.That(limited.Pixels[2], Is.EqualTo(128));
    }

    public static int GetPixelComponent(global::jpeg.JpegImage image, int x, int y, int component)
    {
        return image.Pixels[(y * image.Width + x) * image.ComponentCount + component];
    }

    public static byte[] CreateConstantJpeg(int componentCount, int[] values, int adobeTransform)
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        int index = 0;
        while (index < componentCount)
        {
            frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(index + 1, 1, 1, 0));
            index = index + 1;
        }

        frame.ComponentCount = componentCount;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        index = 0;
        while (index < componentCount)
        {
            BaselineSof0EncoderTests.FillRectangle(source.Components[index], 0, 0, 8, 8, values[index]);
            index = index + 1;
        }

        tables.HuffmanTables = new global::jpeg.JpegImageWriter().CreateOptimizedHuffmanTables(frame, scan, tables.QuantizationTables, source, 0);
        byte[] entropyData = new global::jpeg.JpegBaselineSof0Encoder().Encode(frame, scan, tables, source);
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        if (adobeTransform >= 0)
        {
            writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, AdobeMetadataParserTests.CreateAdobePayload(100, 0, 0, adobeTransform));
        }

        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        new global::jpeg.JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new global::jpeg.JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }
}
