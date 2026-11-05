namespace jpeg.Tests;

internal class IdentifierTests
{
    [Test]
    public void Identify_ReadsBaselineImageStructureWithoutDecodingEntropy()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App0, JfifMetadataParserTests.CreateJfifPayload(1, 2, 1, 72, 72, 0, 0));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, CreateDqtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, CreateDhtPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateSosPayload(3));
        writer.Writer.WriteBytes(new byte[] { 0x55, 0xFF, 0x00, 0x44 });
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.SawStartOfImage, Is.True);
        Assert.That(info.SawStartOfFrame, Is.True);
        Assert.That(info.SawStartOfScan, Is.True);
        Assert.That(info.SawEndOfImage, Is.True);
        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.Precision, Is.EqualTo(8));
        Assert.That(info.ComponentCount, Is.EqualTo(3));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.Progressive, Is.False);
        Assert.That(info.ArithmeticCoding, Is.False);
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio420));
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(1));
        Assert.That(info.AppSegmentCount, Is.EqualTo(1));
        Assert.That(info.ScanCount, Is.EqualTo(1));
        Assert.That(info.LastScanComponentCount, Is.EqualTo(3));
        Assert.That(info.LastScanSpectralStart, Is.EqualTo(0));
        Assert.That(info.LastScanSpectralEnd, Is.EqualTo(63));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App0));
        Assert.That(info.Metadata.HasJfif, Is.True);
        Assert.That(info.Metadata.Jfif.MajorVersion, Is.EqualTo(1));
        Assert.That(info.Metadata.Jfif.MinorVersion, Is.EqualTo(2));
        Assert.That(info.Metadata.XDensity, Is.EqualTo(72));
        Assert.That(info.MetadataBytes, Is.EqualTo(14));
    }

    [Test]
    public void Identify_PreservesCommentsAndUnknownMarkers()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.Comment, new byte[] { 1, 2 });
        writer.WriteMarkerSegmentValue(0xFFF0, new byte[] { 3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.CommentCount, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.Comment));
        Assert.That(info.Metadata.RawMarkers[1].Value, Is.EqualTo(0xFFF0));
        Assert.That(info.Metadata.RawMarkers[1].IsUnknownMarker, Is.True);
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.Grayscale));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Grayscale));
    }

    [Test]
    public void Identify_ReportsProgressiveAndArithmeticProcesses()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame10, CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame10, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDctArithmetic));
        Assert.That(info.Progressive, Is.True);
        Assert.That(info.ArithmeticCoding, Is.True);
    }

    [Test]
    public void Identify_ReadsFromStream()
    {
        byte[] data = CreateMinimalBaselineJpeg();
        System.IO.MemoryStream stream = new System.IO.MemoryStream(data);

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(stream);

        Assert.That(info.Width, Is.EqualTo(32));
        Assert.That(info.Height, Is.EqualTo(16));
        Assert.That(info.SawEndOfImage, Is.True);
    }

    [Test]
    public void Identify_RejectsMalformedFrameHeader()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, new byte[] { 8, 0, 1, 0, 1, 3 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void Identify_RejectsMalformedScanHeader()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, new byte[] { 3, 1, 0, 2 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void Identify_RejectsMalformedTablePayloads()
    {
        global::jpeg.JpegMarkerSegmentWriter dqtWriter = new global::jpeg.JpegMarkerSegmentWriter();
        dqtWriter.WriteStartOfImage();
        dqtWriter.WriteMarkerSegment(global::jpeg.JpegMarker.DefineQuantizationTable, new byte[] { 0 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(dqtWriter.ToArray());
            });

        global::jpeg.JpegMarkerSegmentWriter dhtWriter = new global::jpeg.JpegMarkerSegmentWriter();
        dhtWriter.WriteStartOfImage();
        dhtWriter.WriteMarkerSegment(global::jpeg.JpegMarker.DefineHuffmanTable, new byte[] { 0, 1 });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(dhtWriter.ToArray());
            });
    }

    public static byte[] CreateMinimalBaselineJpeg()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateSosPayload(3));
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateDqtPayload()
    {
        byte[] payload = new byte[65];
        payload[0] = 0;
        return payload;
    }

    public static byte[] CreateDhtPayload()
    {
        byte[] payload = new byte[18];
        payload[0] = 0;
        payload[1] = 1;
        payload[17] = 0;
        return payload;
    }

    public static byte[] CreateSofPayload(int markerValue, int componentCount)
    {
        if (componentCount == 1)
        {
            return new byte[] { 8, 0, 16, 0, 32, 1, 1, 0x11, 0 };
        }

        return new byte[]
        {
            8, 0, 16, 0, 32, 3,
            1, 0x22, 0,
            2, 0x11, 1,
            3, 0x11, 1
        };
    }

    public static byte[] CreateSosPayload(int componentCount)
    {
        if (componentCount == 1)
        {
            return new byte[] { 1, 1, 0, 0, 63, 0 };
        }

        return new byte[]
        {
            3,
            1, 0,
            2, 0x11,
            3, 0x11,
            0, 63, 0
        };
    }
}
