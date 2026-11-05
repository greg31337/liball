namespace jpeg.Tests;

internal class XmpMetadataParserTests
{
    [Test]
    public void Parser_ReadsRawXmpApp1Payload()
    {
        byte[] packet = System.Text.Encoding.UTF8.GetBytes("<x:xmpmeta>test</x:xmpmeta>");
        byte[] payload = CreateXmpPayload(packet);
        global::jpeg.JpegXmpMetadataParser parser = new global::jpeg.JpegXmpMetadataParser();

        global::jpeg.JpegXmpMetadata metadata = parser.Parse(payload);
        payload[global::jpeg.JpegConstants.XmpIdentifierLength] = 0x41;

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.RawPayload, Is.EqualTo(CreateXmpPayload(packet)));
        Assert.That(metadata.PacketData, Is.EqualTo(packet));
        Assert.That(metadata.Packet, Is.EqualTo("<x:xmpmeta>test</x:xmpmeta>"));
    }

    [Test]
    public void Parser_DetectsOnlyStandardXmpApp1Payloads()
    {
        global::jpeg.JpegXmpMetadataParser parser = new global::jpeg.JpegXmpMetadataParser();

        Assert.That(parser.IsXmp(null), Is.False);
        Assert.That(parser.IsXmp(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsXmp(CreateXmpPayload(new byte[0])), Is.True);

        byte[] payload = CreateXmpPayload(new byte[] { 1 });
        payload[0] = 0x48;
        Assert.That(parser.IsXmp(payload), Is.False);

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
    public void Identifier_StoresXmpMetadataFromApp1()
    {
        byte[] packet = System.Text.Encoding.UTF8.GetBytes("<rdf:RDF />");
        byte[] payload = CreateXmpPayload(packet);
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasXmp, Is.True);
        Assert.That(info.Metadata.Xmp, Is.EqualTo(payload));
        Assert.That(info.Metadata.XmpMetadata.RawPayload, Is.EqualTo(payload));
        Assert.That(info.Metadata.XmpMetadata.PacketData, Is.EqualTo(packet));
        Assert.That(info.Metadata.XmpMetadata.Packet, Is.EqualTo("<rdf:RDF />"));
        Assert.That(info.Metadata.HasExif, Is.False);
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_WritesRawXmpApp1Payload()
    {
        byte[] payload = CreateXmpPayload(System.Text.Encoding.UTF8.GetBytes("<x />"));
        global::jpeg.JpegXmpMetadata metadata = new global::jpeg.JpegXmpMetadataParser().Parse(payload);
        global::jpeg.JpegXmpMetadataWriter writer = new global::jpeg.JpegXmpMetadataWriter();
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
    public void Writer_RejectsInvalidXmpMetadata()
    {
        global::jpeg.JpegXmpMetadataWriter writer = new global::jpeg.JpegXmpMetadataWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(new global::jpeg.JpegXmpMetadata());
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegment(null!, new global::jpeg.JpegXmpMetadataParser().Parse(CreateXmpPayload(new byte[0])));
            });
    }

    public static byte[] CreateXmpPayload(byte[] packet)
    {
        byte[] identifier = new global::jpeg.JpegXmpMetadataParser().GetIdentifier();
        byte[] payload = new byte[identifier.Length + packet.Length];
        System.Array.Copy(identifier, 0, payload, 0, identifier.Length);
        if (packet.Length > 0)
        {
            System.Array.Copy(packet, 0, payload, identifier.Length, packet.Length);
        }

        return payload;
    }
}
