namespace jpeg.Tests;

internal class MpfMetadataParserTests
{
    [Test]
    public void Parser_ReadsRawMpfApp2Payload()
    {
        byte[] payload = CreateMpfPayload(new byte[] { 0x49, 0x49, 42, 0, 8, 0, 0, 0 });
        global::jpeg.JpegMpfMetadataParser parser = new global::jpeg.JpegMpfMetadataParser();

        global::jpeg.JpegMpfMetadata metadata = parser.Parse(payload);
        payload[4] = 0x4D;

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.RawPayload, Is.EqualTo(CreateMpfPayload(new byte[] { 0x49, 0x49, 42, 0, 8, 0, 0, 0 })));
        Assert.That(metadata.TiffData, Is.EqualTo(new byte[] { 0x49, 0x49, 42, 0, 8, 0, 0, 0 }));
    }

    [Test]
    public void Parser_DetectsOnlyMpfApp2Payloads()
    {
        global::jpeg.JpegMpfMetadataParser parser = new global::jpeg.JpegMpfMetadataParser();

        Assert.That(parser.IsMpf(null), Is.False);
        Assert.That(parser.IsMpf(new byte[] { 0x4D, 0x50, 0x46 }), Is.False);
        Assert.That(parser.IsMpf(new byte[] { 0x4D, 0x50, 0x46, 1 }), Is.False);
        Assert.That(parser.IsMpf(CreateMpfPayload(new byte[0])), Is.True);

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
    public void Identifier_StoresMpfMetadataFromApp2()
    {
        byte[] payload = CreateMpfPayload(new byte[] { 1, 2, 3, 4 });
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasMpf, Is.True);
        Assert.That(info.Metadata.Mpf, Is.EqualTo(payload));
        Assert.That(info.Metadata.MpfMetadata.RawPayload, Is.EqualTo(payload));
        Assert.That(info.Metadata.MpfMetadata.TiffData, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(info.Metadata.HasIccProfileChunks, Is.False);
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App2));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_WritesRawMpfApp2Payload()
    {
        byte[] payload = CreateMpfPayload(new byte[] { 1, 2, 3, 4 });
        global::jpeg.JpegMpfMetadata metadata = new global::jpeg.JpegMpfMetadataParser().Parse(payload);
        global::jpeg.JpegMpfMetadataWriter writer = new global::jpeg.JpegMpfMetadataWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        byte[] writtenPayload = writer.WritePayload(metadata);
        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(writtenPayload, Is.EqualTo(payload));
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App2));
        Assert.That(segment.Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_RejectsInvalidMpfMetadata()
    {
        global::jpeg.JpegMpfMetadataWriter writer = new global::jpeg.JpegMpfMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(new global::jpeg.JpegMpfMetadata());
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, new global::jpeg.JpegMpfMetadataParser().Parse(CreateMpfPayload(new byte[0])));
            });
    }

    public static byte[] CreateMpfPayload(byte[] tiffData)
    {
        byte[] payload = new byte[global::jpeg.JpegConstants.MpfIdentifierLength + tiffData.Length];
        payload[0] = 0x4D;
        payload[1] = 0x50;
        payload[2] = 0x46;
        payload[3] = 0;
        if (tiffData.Length > 0)
        {
            System.Array.Copy(tiffData, 0, payload, global::jpeg.JpegConstants.MpfIdentifierLength, tiffData.Length);
        }

        return payload;
    }
}
