using System.Collections.Generic;

namespace jpeg.Tests;

internal class IccProfileChunkParserTests
{
    [Test]
    public void Parser_ReadsIccProfileChunk()
    {
        byte[] payload = CreateIccPayload(2, 3, new byte[] { 10, 20, 30 });
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();

        global::jpeg.JpegIccProfileChunk chunk = parser.Parse(payload);

        Assert.That(chunk.IsPresent, Is.True);
        Assert.That(chunk.SequenceNumber, Is.EqualTo(2));
        Assert.That(chunk.ChunkCount, Is.EqualTo(3));
        Assert.That(chunk.Data, Is.EqualTo(new byte[] { 10, 20, 30 }));
    }

    [Test]
    public void Parser_ValidatesCompleteIccChunkSequence()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();
        List<global::jpeg.JpegIccProfileChunk> chunks = new List<global::jpeg.JpegIccProfileChunk>();
        chunks.Add(parser.Parse(CreateIccPayload(2, 2, new byte[] { 2 })));
        chunks.Add(parser.Parse(CreateIccPayload(1, 2, new byte[] { 1 })));

        parser.ValidateSequence(chunks);

        Assert.That(chunks.Count, Is.EqualTo(2));
    }

    [Test]
    public void Identifier_StoresIccProfileChunksFromApp2()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, CreateIccPayload(1, 2, new byte[] { 1, 2 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, CreateIccPayload(2, 2, new byte[] { 3, 4 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasIccProfileChunks, Is.True);
        Assert.That(info.Metadata.IccProfileChunks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.IccProfileChunks[0].SequenceNumber, Is.EqualTo(1));
        Assert.That(info.Metadata.IccProfileChunks[1].SequenceNumber, Is.EqualTo(2));
        Assert.That(info.Metadata.IccProfileChunks[0].Data, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(info.Metadata.IccProfileChunks[1].Data, Is.EqualTo(new byte[] { 3, 4 }));
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Identifier_RejectsIncompleteIccProfileChunkSequence()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, CreateIccPayload(1, 2, new byte[] { 1, 2 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                global::jpeg.JpegIdentifier.Identify(writer.ToArray());
            });
    }

    [Test]
    public void Parser_IgnoresNonIccPayloadWhenCheckingSignature()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();

        Assert.That(parser.IsIccProfile(null), Is.False);
        Assert.That(parser.IsIccProfile(new byte[] { 1, 2, 3 }), Is.False);
        Assert.That(parser.IsIccProfile(new byte[] { 0x49, 0x43, 0x43, 0x5F, 0x50, 0x52, 0x4F, 0x46, 0x49, 0x4C, 0x45, 1 }), Is.False);
    }

    [Test]
    public void Parser_RejectsMalformedIccPayloadsAndSequences()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();

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

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateIccHeaderOnlyPayload());
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateIccPayload(0, 1, new byte[] { 1 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.Parse(CreateIccPayload(2, 1, new byte[] { 1 }));
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.ValidateSequence(CreateDuplicateSequence());
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                parser.ValidateSequence(CreateMismatchedCountSequence());
            });
    }

    public static byte[] CreateIccPayload(int sequenceNumber, int chunkCount, byte[] data)
    {
        byte[] payload = new byte[14 + data.Length];
        WriteIccSignature(payload);
        payload[12] = (byte)sequenceNumber;
        payload[13] = (byte)chunkCount;
        System.Array.Copy(data, 0, payload, 14, data.Length);
        return payload;
    }

    public static byte[] CreateIccHeaderOnlyPayload()
    {
        byte[] payload = new byte[12];
        WriteIccSignature(payload);
        return payload;
    }

    public static void WriteIccSignature(byte[] payload)
    {
        payload[0] = 0x49;
        payload[1] = 0x43;
        payload[2] = 0x43;
        payload[3] = 0x5F;
        payload[4] = 0x50;
        payload[5] = 0x52;
        payload[6] = 0x4F;
        payload[7] = 0x46;
        payload[8] = 0x49;
        payload[9] = 0x4C;
        payload[10] = 0x45;
        payload[11] = 0;
    }

    public static List<global::jpeg.JpegIccProfileChunk> CreateDuplicateSequence()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();
        List<global::jpeg.JpegIccProfileChunk> chunks = new List<global::jpeg.JpegIccProfileChunk>();
        chunks.Add(parser.Parse(CreateIccPayload(1, 2, new byte[] { 1 })));
        chunks.Add(parser.Parse(CreateIccPayload(1, 2, new byte[] { 2 })));
        return chunks;
    }

    public static List<global::jpeg.JpegIccProfileChunk> CreateMismatchedCountSequence()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();
        List<global::jpeg.JpegIccProfileChunk> chunks = new List<global::jpeg.JpegIccProfileChunk>();
        chunks.Add(parser.Parse(CreateIccPayload(1, 2, new byte[] { 1 })));
        chunks.Add(parser.Parse(CreateIccPayload(2, 3, new byte[] { 2 })));
        return chunks;
    }
}
