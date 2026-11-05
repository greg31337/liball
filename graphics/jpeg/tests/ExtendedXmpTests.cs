using System.Collections.Generic;

namespace jpeg.Tests;

internal class ExtendedXmpTests
{
    public const string Guid = "0123456789ABCDEF0123456789ABCDEF";

    [Test]
    public void Parser_ReadsExtendedXmpChunk()
    {
        byte[] payload = CreateExtendedXmpPayload(Guid, 6, 2, new byte[] { 3, 4 });
        global::jpeg.JpegExtendedXmpChunkParser parser = new global::jpeg.JpegExtendedXmpChunkParser();

        global::jpeg.JpegExtendedXmpChunk chunk = parser.Parse(payload);

        Assert.That(parser.IsExtendedXmp(payload), Is.True);
        Assert.That(chunk.IsPresent, Is.True);
        Assert.That(chunk.Guid, Is.EqualTo(Guid));
        Assert.That(chunk.FullLength, Is.EqualTo(6));
        Assert.That(chunk.Offset, Is.EqualTo(2));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 3, 4 }));
    }

    [Test]
    public void Assembler_ReassemblesExtendedXmpChunks()
    {
        global::jpeg.JpegExtendedXmpChunkParser parser = new global::jpeg.JpegExtendedXmpChunkParser();
        List<global::jpeg.JpegExtendedXmpChunk> chunks = new List<global::jpeg.JpegExtendedXmpChunk>();
        chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 6, 3, new byte[] { 4, 5, 6 })));
        chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 6, 0, new byte[] { 1, 2, 3 })));
        global::jpeg.JpegExtendedXmpAssembler assembler = new global::jpeg.JpegExtendedXmpAssembler();

        byte[] packet = assembler.Reassemble(chunks);

        Assert.That(packet, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Identifier_ReassemblesExtendedXmpFromApp1Chunks()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, CreateExtendedXmpPayload(Guid, 5, 3, new byte[] { 4, 5 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App1, CreateExtendedXmpPayload(Guid, 5, 0, new byte[] { 1, 2, 3 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasExtendedXmpChunks, Is.True);
        Assert.That(info.Metadata.ExtendedXmpChunks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.HasExtendedXmp, Is.True);
        Assert.That(info.Metadata.ExtendedXmp, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(info.Metadata.RawMarkers.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.RawMarkers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
    }

    [Test]
    public void Assembler_RejectsInvalidExtendedXmpChunkSets()
    {
        global::jpeg.JpegExtendedXmpChunkParser parser = new global::jpeg.JpegExtendedXmpChunkParser();
        global::jpeg.JpegExtendedXmpAssembler assembler = new global::jpeg.JpegExtendedXmpAssembler();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                List<global::jpeg.JpegExtendedXmpChunk> chunks = new List<global::jpeg.JpegExtendedXmpChunk>();
                chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 4, 0, new byte[] { 1, 2 })));
                assembler.Reassemble(chunks);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                List<global::jpeg.JpegExtendedXmpChunk> chunks = new List<global::jpeg.JpegExtendedXmpChunk>();
                chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 4, 0, new byte[] { 1, 2 })));
                chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 4, 1, new byte[] { 2, 3, 4 })));
                assembler.Reassemble(chunks);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                List<global::jpeg.JpegExtendedXmpChunk> chunks = new List<global::jpeg.JpegExtendedXmpChunk>();
                chunks.Add(parser.Parse(CreateExtendedXmpPayload(Guid, 2, 0, new byte[] { 1 })));
                chunks.Add(parser.Parse(CreateExtendedXmpPayload("FEDCBA9876543210FEDCBA9876543210", 2, 1, new byte[] { 2 })));
                assembler.Reassemble(chunks);
            });
    }

    [Test]
    public void Writer_WritesExtendedXmpChunksThatParseBack()
    {
        byte[] packet = new byte[] { 1, 2, 3, 4, 5 };
        global::jpeg.JpegExtendedXmpChunkWriter writer = new global::jpeg.JpegExtendedXmpChunkWriter();
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();

        List<global::jpeg.JpegExtendedXmpChunk> chunks = writer.CreateChunks(packet, Guid);
        byte[] payload = writer.WritePayload(chunks[0]);
        writer.WriteMarkerSegments(segmentWriter, packet, Guid);

        global::jpeg.JpegExtendedXmpChunk parsed = new global::jpeg.JpegExtendedXmpChunkParser().Parse(payload);
        global::jpeg.JpegMarkerSegmentReader reader = new global::jpeg.JpegMarkerSegmentReader(segmentWriter.ToArray());
        global::jpeg.JpegMarkerSegment segment = reader.ReadNextSegment();
        Assert.That(chunks.Count, Is.EqualTo(1));
        Assert.That(parsed.Guid, Is.EqualTo(Guid));
        Assert.That(parsed.FullLength, Is.EqualTo(packet.Length));
        Assert.That(parsed.Data, Is.EqualTo(packet));
        Assert.That(segment.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
        Assert.That(segment.Payload, Is.EqualTo(payload));
    }

    [Test]
    public void ParserAndWriter_RejectInvalidExtendedXmpPayloads()
    {
        global::jpeg.JpegExtendedXmpChunkParser parser = new global::jpeg.JpegExtendedXmpChunkParser();
        global::jpeg.JpegExtendedXmpChunkWriter writer = new global::jpeg.JpegExtendedXmpChunkWriter();

        Assert.That(parser.IsExtendedXmp(null), Is.False);
        Assert.That(parser.IsExtendedXmp(new byte[] { 1, 2, 3 }), Is.False);

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

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.CreateChunks(null!, Guid);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.CreateChunks(new byte[] { 1 }, "short");
            });
    }

    public static byte[] CreateExtendedXmpPayload(string guid, long fullLength, long offset, byte[] data)
    {
        byte[] identifier = new global::jpeg.JpegExtendedXmpChunkParser().GetIdentifier();
        byte[] payload = new byte[global::jpeg.JpegConstants.ExtendedXmpChunkHeaderLength + data.Length];
        System.Array.Copy(identifier, 0, payload, 0, identifier.Length);
        byte[] guidBytes = System.Text.Encoding.ASCII.GetBytes(guid);
        System.Array.Copy(guidBytes, 0, payload, global::jpeg.JpegConstants.ExtendedXmpIdentifierLength, 32);
        global::jpeg.JpegByteWriter.WriteUInt32(payload, global::jpeg.JpegConstants.ExtendedXmpIdentifierLength + 32, fullLength);
        global::jpeg.JpegByteWriter.WriteUInt32(payload, global::jpeg.JpegConstants.ExtendedXmpIdentifierLength + 36, offset);
        System.Array.Copy(data, 0, payload, global::jpeg.JpegConstants.ExtendedXmpChunkHeaderLength, data.Length);
        return payload;
    }
}
