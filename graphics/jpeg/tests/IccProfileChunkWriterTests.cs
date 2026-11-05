using System.Collections.Generic;

namespace jpeg.Tests;

internal class IccProfileChunkWriterTests
{
    [Test]
    public void Writer_WritesSingleIccChunkPayload()
    {
        global::jpeg.JpegIccProfileChunkWriter writer = new global::jpeg.JpegIccProfileChunkWriter();
        byte[] profile = new byte[] { 1, 2, 3 };

        List<global::jpeg.JpegIccProfileChunk> chunks = writer.CreateChunks(profile);
        byte[] payload = writer.WritePayload(chunks[0]);
        global::jpeg.JpegIccProfileChunk parsed = new global::jpeg.JpegIccProfileChunkParser().Parse(payload);

        Assert.That(chunks.Count, Is.EqualTo(1));
        Assert.That(payload, Is.EqualTo(IccProfileChunkParserTests.CreateIccPayload(1, 1, profile)));
        Assert.That(parsed.SequenceNumber, Is.EqualTo(1));
        Assert.That(parsed.ChunkCount, Is.EqualTo(1));
        Assert.That(parsed.Data, Is.EqualTo(profile));
    }

    [Test]
    public void Writer_SplitsIccProfileAtMarkerPayloadLimit()
    {
        int firstChunkLength = global::jpeg.JpegConstants.MaximumIccProfileChunkDataLength;
        byte[] profile = CreateProfile(firstChunkLength + 2);
        global::jpeg.JpegIccProfileChunkWriter writer = new global::jpeg.JpegIccProfileChunkWriter();

        List<global::jpeg.JpegIccProfileChunk> chunks = writer.CreateChunks(profile);
        byte[] firstPayload = writer.WritePayload(chunks[0]);
        byte[] secondPayload = writer.WritePayload(chunks[1]);
        byte[] reassembled = new global::jpeg.JpegIccProfileAssembler().Reassemble(chunks);

        Assert.That(chunks.Count, Is.EqualTo(2));
        Assert.That(chunks[0].SequenceNumber, Is.EqualTo(1));
        Assert.That(chunks[1].SequenceNumber, Is.EqualTo(2));
        Assert.That(chunks[0].ChunkCount, Is.EqualTo(2));
        Assert.That(chunks[1].ChunkCount, Is.EqualTo(2));
        Assert.That(chunks[0].Data.Length, Is.EqualTo(firstChunkLength));
        Assert.That(chunks[1].Data.Length, Is.EqualTo(2));
        Assert.That(firstPayload.Length, Is.EqualTo(global::jpeg.JpegConstants.MaximumMarkerPayloadLength));
        Assert.That(secondPayload.Length, Is.EqualTo(global::jpeg.JpegConstants.IccProfileChunkHeaderLength + 2));
        Assert.That(reassembled, Is.EqualTo(profile));
    }

    [Test]
    public void Writer_WritesApp2SegmentsThatIdentifyReassembles()
    {
        byte[] profile = CreateProfile(global::jpeg.JpegConstants.MaximumIccProfileChunkDataLength + 1);
        global::jpeg.JpegMarkerSegmentWriter segmentWriter = new global::jpeg.JpegMarkerSegmentWriter();
        global::jpeg.JpegIccProfileChunkWriter writer = new global::jpeg.JpegIccProfileChunkWriter();
        segmentWriter.WriteStartOfImage();

        writer.WriteMarkerSegments(segmentWriter, profile);
        segmentWriter.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame0, 3));
        segmentWriter.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(3));
        segmentWriter.WriteEndOfImage();

        global::jpeg.JpegInfo info = global::jpeg.JpegIdentifier.Identify(segmentWriter.ToArray());

        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfileChunks.Count, Is.EqualTo(2));
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(profile));
    }

    [Test]
    public void Writer_RejectsInvalidIccProfileChunks()
    {
        global::jpeg.JpegIccProfileChunkWriter writer = new global::jpeg.JpegIccProfileChunkWriter();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.CreateChunks(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.CreateChunks(new byte[0]);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.CalculateChunkCount(global::jpeg.JpegConstants.MaximumIccProfileChunkDataLength * global::jpeg.JpegConstants.MaximumIccProfileChunkCount + 1);
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WritePayload(null!);
            });

        Assert.Throws<global::jpeg.JpegWriteValidationException>(
            delegate
            {
                writer.WritePayload(CreateInvalidChunk());
            });

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                writer.WriteMarkerSegments(null!, new byte[] { 1 });
            });
    }

    public static byte[] CreateProfile(int length)
    {
        byte[] profile = new byte[length];
        int index = 0;
        while (index < profile.Length)
        {
            profile[index] = (byte)(index & 255);
            index = index + 1;
        }

        return profile;
    }

    public static global::jpeg.JpegIccProfileChunk CreateInvalidChunk()
    {
        global::jpeg.JpegIccProfileChunk chunk = new global::jpeg.JpegIccProfileChunk();
        chunk.IsPresent = true;
        chunk.SequenceNumber = 2;
        chunk.ChunkCount = 1;
        chunk.Data = new byte[] { 1 };
        return chunk;
    }
}
