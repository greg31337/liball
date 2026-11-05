namespace jpeg.Tests;

internal class JpegReadApiTests
{
    [Test]
    public void Read_DecodesBaselineGrayscaleByteArray()
    {
        byte[] data = CreateReadableGrayscaleJpeg(2);

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.ComponentCount, Is.EqualTo(1));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(image.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
        Assert.That(image.Pixels.Length, Is.EqualTo(64));
        Assert.That(image.Pixels[0], Is.EqualTo(138));
        Assert.That(image.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void Read_DecodesBaselineGrayscaleStreamWithOptions()
    {
        byte[] data = CreateReadableGrayscaleJpeg(3);
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.OutputPixelFormat = global::jpeg.JpegPixelFormat.L8;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(stream, options);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(image.Pixels[0], Is.EqualTo(143));
    }

    [Test]
    public void Read_EnforcesEntropyLimitFromOptions()
    {
        byte[] data = CreateReadableGrayscaleJpeg(2);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.MaxEntropyBytes = 0;

        Assert.Throws<global::jpeg.JpegResourceLimitException>(
            delegate
            {
                global::jpeg.Jpeg.Read(data, options);
            });
    }

    [Test]
    public void Read_RejectsMalformedProgressiveScan()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame2, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame2, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.Jpeg.Read(writer.ToArray());
            });
    }

    [Test]
    public void Read_RejectsNullOptions()
    {
        byte[] data = CreateReadableGrayscaleJpeg(2);
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Read(data, null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.Jpeg.Read(stream, null!);
            });
    }

    public static byte[] CreateReadableGrayscaleJpeg(int dcCoefficient)
    {
        global::jpeg.JpegFrameHeader frame = BaselineSof0FullImageDecoderTests.CreateFrame(8, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegScanHeader scan = BaselineSof0FullImageDecoderTests.CreateScan(frame);
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.EncodeBlock(
            BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(dcCoefficient),
            tables.HuffmanTables[0],
            tables.HuffmanTables[1],
            0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateGrayscaleSofPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteBytes(encoder.ToArray());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateGrayscaleSofPayload()
    {
        return new byte[] { 8, 0, 8, 0, 8, 1, 1, 0x11, 0 };
    }
}
