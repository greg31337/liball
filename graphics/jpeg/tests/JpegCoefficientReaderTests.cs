namespace jpeg.Tests;

internal class JpegCoefficientReaderTests
{
    [Test]
    public void ReadCoefficients_ReadsBaselineGrayscaleCoefficientBlocks()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);

        global::jpeg.JpegCoefficientImage image = global::jpeg.JpegInspector.ReadCoefficients(data);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Precision, Is.EqualTo(8));
        Assert.That(image.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(image.ComponentCount, Is.EqualTo(1));
        Assert.That(image.Components.Count, Is.EqualTo(1));
        Assert.That(image.Components[0].Identifier, Is.EqualTo(1));
        Assert.That(image.Components[0].BlocksPerRow, Is.EqualTo(1));
        Assert.That(image.Components[0].BlockRows, Is.EqualTo(1));
        Assert.That(image.Components[0].Coefficients.Length, Is.EqualTo(64));
        Assert.That(image.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(image.Components[0].Coefficients[1], Is.EqualTo(0));
        Assert.That(image.QuantizationTables.Count, Is.EqualTo(1));
        Assert.That(image.HuffmanTables.Count, Is.EqualTo(2));
        Assert.That(image.ScanHeaders.Count, Is.EqualTo(1));
        Assert.That(image.EntropyCoding, Is.EqualTo(global::jpeg.JpegEntropyCoding.Huffman));
        Assert.That(image.Progressive, Is.False);
        Assert.That(image.ArithmeticCoding, Is.False);
        Assert.That(image.RestartIntervalUnit, Is.EqualTo(global::jpeg.JpegRestartIntervalUnit.McuBlocks));
        Assert.That(image.Markers.Select(marker => marker.Marker), Does.Contain(global::jpeg.JpegMarker.StartOfFrame0));
        Assert.That(image.Markers.Select(marker => marker.Marker), Does.Contain(global::jpeg.JpegMarker.StartOfScan));
    }

    [Test]
    public void ReadCoefficients_ConsumesRestartMarkers()
    {
        byte[] data = CreateTwoBlockRestartCoefficientJpeg();

        global::jpeg.JpegCoefficientImage image = global::jpeg.JpegInspector.ReadCoefficients(data);

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.HasRestartInterval, Is.True);
        Assert.That(image.RestartInterval, Is.EqualTo(1));
        Assert.That(image.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(image.Markers.Count(marker => marker.Marker == global::jpeg.JpegMarker.Restart0), Is.EqualTo(1));
        Assert.That(image.Components[0].BlocksPerRow, Is.EqualTo(2));
        Assert.That(image.Components[0].Coefficients[0], Is.EqualTo(2));
        Assert.That(image.Components[0].Coefficients[64], Is.EqualTo(2));
    }

    [Test]
    public void ReadCoefficients_ReadsFromStreamAndFile()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(3);
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);
        string path = JpegFilePathApiTests.CreateTempJpegPath();
        System.IO.File.WriteAllBytes(path, data);

        global::jpeg.JpegCoefficientImage fromStream = global::jpeg.JpegInspector.ReadCoefficients(stream);
        global::jpeg.JpegCoefficientImage fromFile = global::jpeg.JpegInspector.ReadCoefficientsFile(path);

        Assert.That(fromStream.Components[0].Coefficients[0], Is.EqualTo(3));
        Assert.That(fromFile.Components[0].Coefficients[0], Is.EqualTo(3));

        System.IO.File.Delete(path);
    }

    [Test]
    public void ReadCoefficients_RejectsLosslessProcessWithoutDctCoefficients()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame3, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame3, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadCoefficients(writer.ToArray());
            });
    }

    [Test]
    public void ReadCoefficients_RejectsNullOptions()
    {
        byte[] data = JpegReadApiTests.CreateReadableGrayscaleJpeg(2);
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadCoefficients(data, null!);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                global::jpeg.JpegInspector.ReadCoefficients(stream, null!);
            });
    }

    public static byte[] CreateTwoBlockRestartCoefficientJpeg()
    {
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegBaselineHuffmanEntropyEncoder encoder = new global::jpeg.JpegBaselineHuffmanEntropyEncoder();
        encoder.ConfigureRestartInterval(1);
        encoder.EncodeBlock(
            BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2),
            tables.HuffmanTables[0],
            tables.HuffmanTables[1],
            0);
        encoder.RecordMcuEncoded();
        encoder.WriteRestartMarker();
        encoder.EncodeBlock(
            BaselineSof0FullImageDecoderTests.CreateDcOnlyBlock(2),
            tables.HuffmanTables[0],
            tables.HuffmanTables[1],
            0);
        encoder.FlushToByteBoundary();

        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new global::jpeg.JpegQuantizationTableWriter().WritePayloads(tables.QuantizationTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new global::jpeg.JpegHuffmanTableWriter().WritePayloads(tables.HuffmanTables));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineRestartInterval, new byte[] { 0, 1 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateGrayscaleSofPayload(16, 8));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.Writer.WriteBytes(encoder.ToArray());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateGrayscaleSofPayload(int width, int height)
    {
        return new byte[]
        {
            8,
            (byte)((height >> 8) & 255),
            (byte)(height & 255),
            (byte)((width >> 8) & 255),
            (byte)(width & 255),
            1,
            1,
            0x11,
            0
        };
    }
}
