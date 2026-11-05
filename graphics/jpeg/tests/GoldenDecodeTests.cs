using System.Collections.Generic;

namespace jpeg.Tests;

internal class GoldenDecodeTests
{
    [Test]
    public void GoldenDecode_BaselineGrayscaleFile()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(image.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void GoldenDecode_BaselineYCbCrAndRgbOutputFile()
    {
        byte[] data = CreateReadableYCbCr444Jpeg();
        global::jpeg.JpegReadOptions rgbOptions = new global::jpeg.JpegReadOptions();
        rgbOptions.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;

        global::jpeg.JpegImage yCbCr = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegImage rgb = global::jpeg.Jpeg.Read(data, rgbOptions);

        Assert.That(yCbCr.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(yCbCr.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(yCbCr.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(yCbCr.Pixels[0], Is.EqualTo(128));
        Assert.That(yCbCr.Pixels[1], Is.EqualTo(133));
        Assert.That(yCbCr.Pixels[2], Is.EqualTo(138));

        Assert.That(rgb.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(rgb.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        Assert.That(rgb.Pixels[0], Is.EqualTo(142));
        Assert.That(rgb.Pixels[1], Is.EqualTo(119));
        Assert.That(rgb.Pixels[2], Is.EqualTo(137));
    }

    [Test]
    public void GoldenDecode_BaselineRestartIntervalFile()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;

        byte[] data = global::jpeg.Jpeg.Write(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);

        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(decoded.Width, Is.EqualTo(16));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[8], Is.EqualTo(143));
    }

    [Test]
    public void GoldenDecode_BaselineFourTwoZeroPartialHeightRestartFile()
    {
        byte[] data = CreateReadableYCbCr420PartialHeightRestartJpeg();
        global::jpeg.JpegReadOptions rgbOptions = new global::jpeg.JpegReadOptions();
        rgbOptions.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage yCbCr = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegImage rgb = global::jpeg.Jpeg.Read(data, rgbOptions);

        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(9));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));

        Assert.That(yCbCr.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(yCbCr.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(yCbCr.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        AssertYCbCrPixel(yCbCr, 0, 0, 138, 133, 138);
        AssertYCbCrPixel(yCbCr, 8, 0, 143, 133, 138);
        AssertYCbCrPixel(yCbCr, 16, 0, 133, 128, 128);
        AssertYCbCrPixel(yCbCr, 24, 0, 138, 128, 128);
        AssertYCbCrPixel(yCbCr, 0, 8, 133, 133, 138);
        AssertYCbCrPixel(yCbCr, 16, 8, 128, 128, 128);

        Assert.That(rgb.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(rgb.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        AssertRgbPixel(rgb, 0, 0, 152, 129, 147);
        AssertRgbPixel(rgb, 8, 0, 157, 134, 152);
        AssertRgbPixel(rgb, 16, 0, 133, 133, 133);
        AssertRgbPixel(rgb, 24, 0, 138, 138, 138);
        AssertRgbPixel(rgb, 0, 8, 147, 124, 142);
        AssertRgbPixel(rgb, 16, 8, 128, 128, 128);
    }

    [Test]
    public void GoldenDecode_BaselineFourTwoZeroPartialWidthAndHeightFile()
    {
        byte[] data = CreateReadableYCbCr420PartialWidthHeightJpeg();
        global::jpeg.JpegReadOptions rgbOptions = new global::jpeg.JpegReadOptions();
        rgbOptions.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage yCbCr = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegImage rgb = global::jpeg.Jpeg.Read(data, rgbOptions);

        Assert.That(info.Width, Is.EqualTo(17));
        Assert.That(info.Height, Is.EqualTo(17));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        Assert.That(yCbCr.Width, Is.EqualTo(17));
        Assert.That(yCbCr.Height, Is.EqualTo(17));
        Assert.That(yCbCr.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        AssertYCbCrPixel(yCbCr, 0, 0, 138, 133, 138);
        AssertYCbCrPixel(yCbCr, 16, 0, 138, 133, 138);
        AssertYCbCrPixel(yCbCr, 0, 16, 138, 133, 138);
        AssertYCbCrPixel(yCbCr, 16, 16, 138, 133, 138);
        AssertRgbPixel(rgb, 16, 16, 152, 129, 147);
    }

    [Test]
    public void GoldenDecode_ProgressiveScanData()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 8, 0, 143);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.Encode(frame, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(scans.Count, Is.EqualTo(2));
        Assert.That(scans[0].Scan.SpectralStart, Is.EqualTo(0));
        Assert.That(scans[1].Scan.SpectralStart, Is.EqualTo(1));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
    }

    [Test]
    public void GoldenDecode_CmykAndYcckColorFiles()
    {
        global::jpeg.JpegCmykOutputConverter cmykConverter = new global::jpeg.JpegCmykOutputConverter();
        global::jpeg.JpegYcckToCmykConverter ycckConverter = new global::jpeg.JpegYcckToCmykConverter();

        global::jpeg.JpegImage cmyk = cmykConverter.ConvertPlanarToImageWithInversion(CmykOutputConverterTests.CreatePlanarBuffer(), false);
        global::jpeg.JpegImage ycckAsCmyk = ycckConverter.ConvertImageToCmykImage(YcckToCmykConverterTests.CreateYcckImage());

        Assert.That(cmyk.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(cmyk.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(cmyk.Pixels, Is.EqualTo(CmykOutputConverterTests.CreateExpectedPixels()));
        Assert.That(ycckAsCmyk.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(ycckAsCmyk.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(ycckAsCmyk.Pixels, Is.EqualTo(YcckToCmykConverterTests.CreateExpectedCmykPixels()));
    }

    [Test]
    public void GoldenDecode_MetadataHeavyFileWithIccAndExif()
    {
        byte[] data = CreateReadableMetadataHeavyJpeg();

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data);

        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(image.Metadata.HasJfif, Is.True);
        Assert.That(image.Metadata.HasExif, Is.True);
        Assert.That(image.Metadata.ExifMetadata.Orientation, Is.EqualTo(6));
        Assert.That(image.Metadata.ExifMetadata.Make, Is.EqualTo("CameraCo"));
        Assert.That(image.Metadata.HasXmp, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(image.Metadata.Xmp), Does.Contain("<golden />"));
        Assert.That(image.Metadata.HasIccProfile, Is.True);
        Assert.That(image.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(image.Metadata.HasPhotoshop, Is.True);
        Assert.That(image.Metadata.HasMpf, Is.True);
        Assert.That(image.Metadata.Comments, Is.EqualTo(new string[] { "golden note" }));
        Assert.That(MetadataPolicyTests.ContainsRawMarker(image.Metadata, 0xFFF0), Is.True);
    }

    public static byte[] CreateReadableYCbCr444Jpeg()
    {
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(0), tables.HuffmanTables[0], tables.HuffmanTables[1], 0);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(1), tables.HuffmanTables[0], tables.HuffmanTables[1], 1);
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), tables.HuffmanTables[0], tables.HuffmanTables[1], 2);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateYCbCr444SofPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateThreeComponentSingleTableSosPayload());
        writer.Writer.WriteBytes(encoder.ToArray());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateReadableMetadataHeavyJpeg()
    {
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.EncodeBlock(BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2), tables.HuffmanTables[0], tables.HuffmanTables[1], 0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 96, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff()));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, XmpMetadataParserTests.CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<golden />")));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, System.Text.Encoding.Latin1.GetBytes("golden note"));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, MpfMetadataParserTests.CreateMpfPayload(new byte[] { 1, 2, 3, 4 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App13, PhotoshopMetadataParserTests.CreatePhotoshopPayload(new byte[] { 5, 6, 7 }));
        writer.WriteMarkerSegmentValue(0xFFF0, new byte[] { 7, 7 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, IccProfileChunkParserTests.CreateIccPayload(1, 1, new byte[] { 9, 8, 7 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, JpegReadApiTests.CreateGrayscaleSofPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteBytes(encoder.ToArray());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateReadableYCbCr420PartialWidthHeightJpeg()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(17, 17);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        FillComponent(source.Components[0], 138);
        FillComponent(source.Components[1], 133);
        FillComponent(source.Components[2], 138);

        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        byte[] entropyData = encoder.Encode(frame, scan, tables, source);

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        new global::jpeg.JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new global::jpeg.JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateReadableYCbCr420PartialHeightRestartJpeg()
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(32, 9);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 2, 2, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(2, 1, 1, 0));
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(3, 1, 1, 0));
        frame.ComponentCount = 3;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        FillFourTwoZeroPartialHeightSource(source);

        global::jpeg.JpegBaselineSof0Encoder encoder = new global::jpeg.JpegBaselineSof0Encoder();
        byte[] entropyData = encoder.EncodeWithRestartInterval(frame, scan, tables, source, 1);

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineRestartInterval, new byte[] { 0, 1 });
        new global::jpeg.JpegFrameHeaderWriter().WriteMarkerSegment(writer, frame);
        new global::jpeg.JpegScanHeaderWriter().WriteMarkerSegment(writer, scan);
        writer.Writer.WriteBytes(entropyData);
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static void FillFourTwoZeroPartialHeightSource(global::jpeg.JpegPlanarImageBuffer source)
    {
        global::jpeg.JpegPlanarComponentBuffer y = source.Components[0];
        BaselineSof0EncoderTests.FillBlock(y, 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(y, 8, 0, 143);
        BaselineSof0EncoderTests.FillRectangle(y, 0, 8, 8, 1, 133);
        BaselineSof0EncoderTests.FillRectangle(y, 8, 8, 8, 1, 128);
        BaselineSof0EncoderTests.FillBlock(y, 16, 0, 133);
        BaselineSof0EncoderTests.FillBlock(y, 24, 0, 138);
        BaselineSof0EncoderTests.FillRectangle(y, 16, 8, 8, 1, 128);
        BaselineSof0EncoderTests.FillRectangle(y, 24, 8, 8, 1, 133);

        global::jpeg.JpegPlanarComponentBuffer cb = source.Components[1];
        BaselineSof0EncoderTests.FillRectangle(cb, 0, 0, 8, 5, 133);
        BaselineSof0EncoderTests.FillRectangle(cb, 8, 0, 8, 5, 128);

        global::jpeg.JpegPlanarComponentBuffer cr = source.Components[2];
        BaselineSof0EncoderTests.FillRectangle(cr, 0, 0, 8, 5, 138);
        BaselineSof0EncoderTests.FillRectangle(cr, 8, 0, 8, 5, 128);
    }

    public static void FillComponent(global::jpeg.JpegPlanarComponentBuffer component, int value)
    {
        BaselineSof0EncoderTests.FillRectangle(component, 0, 0, component.Width, component.Height, value);
    }

    public static byte[] CreateYCbCr444SofPayload()
    {
        return new byte[]
        {
            8, 0, 8, 0, 8, 3,
            1, 0x11, 0,
            2, 0x11, 0,
            3, 0x11, 0
        };
    }

    public static byte[] CreateThreeComponentSingleTableSosPayload()
    {
        return new byte[]
        {
            3,
            1, 0,
            2, 0,
            3, 0,
            0, 63, 0
        };
    }

    public static void AssertYCbCrPixel(global::jpeg.JpegImage image, int x, int y, int expectedY, int expectedCb, int expectedCr)
    {
        int offset = (y * image.Width + x) * 3;
        Assert.That(image.Pixels[offset], Is.EqualTo(expectedY));
        Assert.That(image.Pixels[offset + 1], Is.EqualTo(expectedCb));
        Assert.That(image.Pixels[offset + 2], Is.EqualTo(expectedCr));
    }

    public static void AssertRgbPixel(global::jpeg.JpegImage image, int x, int y, int expectedRed, int expectedGreen, int expectedBlue)
    {
        int offset = (y * image.Width + x) * 3;
        Assert.That(image.Pixels[offset], Is.EqualTo(expectedRed));
        Assert.That(image.Pixels[offset + 1], Is.EqualTo(expectedGreen));
        Assert.That(image.Pixels[offset + 2], Is.EqualTo(expectedBlue));
    }
}
