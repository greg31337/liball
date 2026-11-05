namespace jpeg.Tests;

internal class PhotoshopMetadataParserTests
{
    [Test]
    public void Parser_ReadsPhotoshopResourceBlocksAndDiscoversIptc()
    {
        byte[] iptc = new byte[] { 0x1C, 2, 5, 0, 3, 65, 66, 67 };
        byte[] payload = CreatePhotoshopPayload(iptc);
        global::jpeg.JpegPhotoshopMetadataParser parser = new global::jpeg.JpegPhotoshopMetadataParser();

        global::jpeg.JpegPhotoshopMetadata metadata = parser.Parse(payload);
        payload[payload.Length - 1] = 0x99;

        Assert.That(metadata.IsPresent, Is.True);
        Assert.That(metadata.RawPayload, Is.EqualTo(CreatePhotoshopPayload(iptc)));
        Assert.That(metadata.ResourceBlocks.Count, Is.EqualTo(2));
        Assert.That(metadata.ResourceBlocks[0].Signature, Is.EqualTo("8BIM"));
        Assert.That(metadata.ResourceBlocks[0].ResourceId, Is.EqualTo(0x03ED));
        Assert.That(metadata.ResourceBlocks[0].Name, Is.EqualTo(""));
        Assert.That(metadata.ResourceBlocks[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(metadata.ResourceBlocks[1].ResourceId, Is.EqualTo(global::jpeg.JpegConstants.PhotoshopIptcResourceId));
        Assert.That(metadata.ResourceBlocks[1].Name, Is.EqualTo("IPTC"));
        Assert.That(metadata.ResourceBlocks[1].IsIptc, Is.True);
        Assert.That(metadata.HasIptc, Is.True);
        Assert.That(metadata.IptcData, Is.EqualTo(iptc));
    }

    [Test]
    public void Parser_DetectsOnlyPhotoshopApp13Payloads()
    {
        global::jpeg.JpegPhotoshopMetadataParser parser = new global::jpeg.JpegPhotoshopMetadataParser();

        Assert.That(parser.IsPhotoshop(null), Is.False);
        Assert.That(parser.IsPhotoshop(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsPhotoshop(CreatePhotoshopPayload(new byte[0])), Is.True);

        byte[] payload = CreatePhotoshopPayload(new byte[0]);
        payload[0] = 0x70;
        Assert.That(parser.IsPhotoshop(payload), Is.False);

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
    public void Identifier_StoresPhotoshopIptcFromApp13()
    {
        byte[] iptc = new byte[] { 0x1C, 2, 120, 0, 1, 88 };
        byte[] payload = CreatePhotoshopPayload(iptc);
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App13, payload);
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasPhotoshop, Is.True);
        Assert.That(info.Metadata.Photoshop.ResourceBlocks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.HasIptc, Is.True);
        Assert.That(info.Metadata.Iptc, Is.EqualTo(iptc));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(1));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App13));
        Assert.That(info.Metadata.RawMarkers[0].Payload, Is.EqualTo(payload));
    }

    [Test]
    public void Writer_WritesRawPhotoshopApp13Payload()
    {
        byte[] payload = CreatePhotoshopPayload(new byte[] { 1, 2, 3 });
        global::jpeg.JpegPhotoshopMetadata metadata = new global::jpeg.JpegPhotoshopMetadataParser().Parse(payload);
        global::jpeg.JpegPhotoshopMetadataWriter writer = new global::jpeg.JpegPhotoshopMetadataWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        byte[] writtenPayload = writer.WritePayload(metadata);
        writer.WriteMarkerSegment(segmentWriter, metadata);

        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(writtenPayload, Is.EqualTo(payload));
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App13));
        Assert.That(segment.Payload, Is.EqualTo(payload));
    }

    [Test]
    public void ParserAndWriter_RejectInvalidPhotoshopMetadata()
    {
        global::jpeg.JpegPhotoshopMetadataWriter writer = new global::jpeg.JpegPhotoshopMetadataWriter();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                byte[] payload = CreatePhotoshopPayload(new byte[] { 1 });
                payload[14] = 0x39;
                new global::jpeg.JpegPhotoshopMetadataParser().Parse(payload);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(new global::jpeg.JpegPhotoshopMetadata());
            });
    }

    public static byte[] CreatePhotoshopPayload(byte[] iptc)
    {
        global::jpeg.JpegPhotoshopMetadataParser parser = new global::jpeg.JpegPhotoshopMetadataParser();
        byte[] identifier = parser.GetIdentifier();
        global::jpeg.JpegByteWriter writer = new global::jpeg.JpegByteWriter();
        writer.WriteBytes(identifier);
        WriteResourceBlock(writer, "8BIM", 0x03ED, "", new byte[] { 1, 2 });
        WriteResourceBlock(writer, "8BIM", global::jpeg.JpegConstants.PhotoshopIptcResourceId, "IPTC", iptc);
        return writer.ToArray();
    }

    public static void WriteResourceBlock(global::jpeg.JpegByteWriter writer, string signature, int resourceId, string name, byte[] data)
    {
        writer.WriteBytes(System.Text.Encoding.ASCII.GetBytes(signature));
        writer.WriteUInt16(resourceId);
        byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(name);
        writer.WriteByte(nameBytes.Length);
        if (nameBytes.Length > 0)
        {
            writer.WriteBytes(nameBytes);
        }

        if ((1 + nameBytes.Length) % 2 != 0)
        {
            writer.WriteByte(0);
        }

        writer.WriteUInt32(data.Length);
        if (data.Length > 0)
        {
            writer.WriteBytes(data);
        }

        if (data.Length % 2 != 0)
        {
            writer.WriteByte(0);
        }
    }
}
