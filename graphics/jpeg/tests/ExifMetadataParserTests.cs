namespace jpeg.Tests;

internal class ExifMetadataParserTests
{
    [Test]
    public void Parser_ReadsRawExifApp1Payload()
    {
        byte[] payload = CreateExifPayload(new byte[] { 0x49, 0x49, 0x2A, 0x00, 8, 0, 0, 0 });
        global::jpeg.JpegExifMetadataParser parser = new global::jpeg.JpegExifMetadataParser();

        global::jpeg.JpegExifMetadata metadata = parser.Parse(payload);
        payload[6] = 0x4D;

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.TiffOffset, Is.EqualTo(global::jpeg.JpegConstants.ExifIdentifierLength));
        Assert.That(metadata.RawPayload, Is.EqualTo(CreateExifPayload(new byte[] { 0x49, 0x49, 0x2A, 0x00, 8, 0, 0, 0 })));
        Assert.That(metadata.TiffData, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2A, 0x00, 8, 0, 0, 0 }));
        Assert.That(metadata.TiffHeader.IsPresent, Is.True);
        Assert.That(metadata.TiffHeader.ByteOrder, Is.EqualTo(global::jpeg.JpegTiffByteOrder.LittleEndian));
        Assert.That(metadata.TiffHeader.FirstIfdOffset, Is.EqualTo(8));
    }

    [Test]
    public void Parser_DetectsOnlyExifApp1Payloads()
    {
        global::jpeg.JpegExifMetadataParser parser = new global::jpeg.JpegExifMetadataParser();

        Assert.That(parser.IsExif(null), Is.False);
        Assert.That(parser.IsExif(new byte[] { 0x45, 0x78, 0x69, 0x66, 0 }), Is.False);
        Assert.That(parser.IsExif(new byte[] { 0x45, 0x78, 0x69, 0x66, 0, 1 }), Is.False);
        Assert.That(parser.IsExif(CreateExifPayload(new byte[0])), Is.True);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                parser.Parse(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(new byte[] { 1, 2, 3 });
            });
    }

    [Test]
    public void Identifier_StoresExifMetadataFromApp1()
    {
        byte[] payload = CreateExifPayload(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 });
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.Exif, Is.EqualTo(payload));
        Assert.That(info.Metadata.ExifMetadata.RawPayload, Is.EqualTo(payload));
        Assert.That(info.Metadata.ExifMetadata.TiffData, Is.EqualTo(new byte[] { 0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8 }));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Identifier_IgnoresNonExifApp1ForExifMetadata()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, new byte[] { 1, 2, 3 });
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasExif, Is.False);
        Assert.That(info.Metadata.Exif, Is.EqualTo(new byte[0]));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Writer_WritesRawExifApp1Payload()
    {
        byte[] payload = CreateExifPayload(new byte[] { 1, 2, 3, 4 });
        global::jpeg.JpegExifMetadata metadata = new global::jpeg.JpegExifMetadataParser().Parse(payload);
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        byte[] writtenPayload = writer.WritePayload(metadata);
        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(writtenPayload, Is.EqualTo(payload));
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(segment.Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_RejectsInvalidExifMetadata()
    {
        global::jpeg.JpegExifMetadataWriter writer = new global::jpeg.JpegExifMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(new global::jpeg.JpegExifMetadata());
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, new global::jpeg.JpegExifMetadataParser().Parse(CreateExifPayload(new byte[0])));
            });
    }

    public static byte[] CreateExifPayload(byte[] tiffData)
    {
        byte[] payload = new byte[global::jpeg.JpegConstants.ExifIdentifierLength + tiffData.Length];
        payload[0] = 0x45;
        payload[1] = 0x78;
        payload[2] = 0x69;
        payload[3] = 0x66;
        payload[4] = 0;
        payload[5] = 0;
        if (tiffData.Length > 0)
        {
            System.Array.Copy(tiffData, 0, payload, global::jpeg.JpegConstants.ExifIdentifierLength, tiffData.Length);
        }

        return payload;
    }
}
