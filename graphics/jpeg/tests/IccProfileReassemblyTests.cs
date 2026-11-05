using System.Collections.Generic;

namespace jpeg.Tests;

internal class IccProfileReassemblyTests
{
    [Test]
    public void Assembler_ReassemblesChunksInSequenceOrder()
    {
        global::jpeg.JpegIccProfileChunkParser parser = new global::jpeg.JpegIccProfileChunkParser();
        List<global::jpeg.JpegIccProfileChunk> chunks = new List<global::jpeg.JpegIccProfileChunk>();
        chunks.Add(parser.Parse(IccProfileChunkParserTests.CreateIccPayload(2, 2, new byte[] { 3, 4 })));
        chunks.Add(parser.Parse(IccProfileChunkParserTests.CreateIccPayload(1, 2, new byte[] { 1, 2 })));
        global::jpeg.JpegIccProfileAssembler assembler = new global::jpeg.JpegIccProfileAssembler();

        byte[] profile = assembler.Reassemble(chunks);

        Assert.That(profile, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Assembler_ReturnsEmptyProfileForEmptyChunkList()
    {
        global::jpeg.JpegIccProfileAssembler assembler = new global::jpeg.JpegIccProfileAssembler();

        byte[] profile = assembler.Reassemble(new List<global::jpeg.JpegIccProfileChunk>());

        Assert.That(profile, Is.Empty);
    }

    [Test]
    public void Identifier_ReassemblesIccProfileFromApp2Chunks()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, IccProfileChunkParserTests.CreateIccPayload(2, 2, new byte[] { 3, 4 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App2, IccProfileChunkParserTests.CreateIccPayload(1, 2, new byte[] { 1, 2 }));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        writer.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(writer.ToArray());

        Assert.That(info.Metadata.HasIccProfileChunks, Is.True);
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Assembler_RejectsInvalidReassemblyInputs()
    {
        global::jpeg.JpegIccProfileAssembler assembler = new global::jpeg.JpegIccProfileAssembler();
        global::jpeg.JpegIccProfileChunk chunk = new global::jpeg.JpegIccProfileChunk();
        chunk.IsPresent = true;
        chunk.SequenceNumber = 1;
        chunk.ChunkCount = 1;
        chunk.Data = null!;
        List<global::jpeg.JpegIccProfileChunk> chunks = new List<global::jpeg.JpegIccProfileChunk>();
        chunks.Add(chunk);

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                assembler.Reassemble(null!);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                assembler.Reassemble(chunks);
            });
    }
}
