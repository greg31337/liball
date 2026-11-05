using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class UnknownChunkTests
{
    [Test]
    public void PngUnknownChunk_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngUnknownChunk).IsNotPublic, Is.True);
        Assert.That(typeof(PngUnknownChunk).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngUnknownChunk).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngUnknownChunk).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngUnknownChunk).GetFields(flags), Has.Length.EqualTo(8));
    }

    [Test]
    public void Create_StoresUnknownChunkPreservationMetadata()
    {
        PngChunk chunk = CreateChunk("vpAg", new byte[] { 1, 2, 3 }, 44);

        PngUnknownChunk unknownChunk = PngUnknownChunkPreservation.Create(chunk, 7);

        Assert.That(unknownChunk.Chunk, Is.SameAs(chunk));
        Assert.That(unknownChunk.Type, Is.EqualTo("vpAg"));
        Assert.That(unknownChunk.OriginalIndex, Is.EqualTo(7));
        Assert.That(unknownChunk.OriginalOffset, Is.EqualTo(44));
        Assert.That(unknownChunk.IsAncillary, Is.True);
        Assert.That(unknownChunk.IsSafeToCopy, Is.True);
        Assert.That(unknownChunk.Disposition, Is.EqualTo(PngChunkDisposition.Preserve));
        Assert.That(unknownChunk.ReplacementData, Is.Empty);
    }

    [Test]
    public void Create_RejectsKnownChunkTypes()
    {
        PngChunk chunk = CreateChunk(PngConstants.ChunkIEND, new byte[0], 0);

        Assert.Throws<ArgumentException>(() => PngUnknownChunkPreservation.Create(chunk, 0));
    }

    [Test]
    public void Create_RejectsInvalidInputs()
    {
        PngChunk invalidChunk = CreateChunk("abca", new byte[0], 0);

        Assert.Throws<ArgumentNullException>(() => PngUnknownChunkPreservation.Create(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[0], 0), -1));
        Assert.Throws<ArgumentException>(() => PngUnknownChunkPreservation.Create(invalidChunk, 0));
    }

    [Test]
    public void ShouldWrite_FollowsDisposition()
    {
        PngUnknownChunk unknownChunk = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[0], 0), 0);

        unknownChunk.Disposition = PngChunkDisposition.Preserve;
        Assert.That(PngUnknownChunkPreservation.ShouldWrite(unknownChunk), Is.True);

        unknownChunk.Disposition = PngChunkDisposition.Drop;
        Assert.That(PngUnknownChunkPreservation.ShouldWrite(unknownChunk), Is.False);

        unknownChunk.Disposition = PngChunkDisposition.Replace;
        Assert.That(PngUnknownChunkPreservation.ShouldWrite(unknownChunk), Is.True);
    }

    [Test]
    public void CreateOutputChunk_PreservesOriginalUnknownChunkData()
    {
        PngChunk chunk = CreateChunk("vpAg", new byte[] { 1, 2, 3 }, 44);
        PngUnknownChunk unknownChunk = PngUnknownChunkPreservation.Create(chunk, 0);

        PngChunk outputChunk = PngUnknownChunkPreservation.CreateOutputChunk(unknownChunk);

        Assert.That(outputChunk.Type, Is.EqualTo("vpAg"));
        Assert.That(outputChunk.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(outputChunk.Length, Is.EqualTo(3));
        Assert.That(outputChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc("vpAg", outputChunk.Data)));
        Assert.That(outputChunk.Offset, Is.EqualTo(44));
        Assert.That(outputChunk.IsKnown, Is.False);
        Assert.That(outputChunk.IsSafeToCopy, Is.True);
    }

    [Test]
    public void CreateOutputChunk_ReplacesDataWhenDispositionIsReplace()
    {
        PngUnknownChunk unknownChunk = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[] { 1 }, 10), 0);
        unknownChunk.Disposition = PngChunkDisposition.Replace;
        unknownChunk.ReplacementData = new byte[] { 9, 8 };

        PngChunk outputChunk = PngUnknownChunkPreservation.CreateOutputChunk(unknownChunk);

        Assert.That(outputChunk.Type, Is.EqualTo("vpAg"));
        Assert.That(outputChunk.Data, Is.EqualTo(new byte[] { 9, 8 }));
        Assert.That(outputChunk.Length, Is.EqualTo(2));
        Assert.That(outputChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc("vpAg", outputChunk.Data)));
        Assert.That(outputChunk.Offset, Is.EqualTo(10));
    }

    [Test]
    public void CreateOutputChunk_ReturnsIndependentDataCopy()
    {
        PngUnknownChunk unknownChunk = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[] { 1, 2 }, 0), 0);

        PngChunk outputChunk = PngUnknownChunkPreservation.CreateOutputChunk(unknownChunk);
        outputChunk.Data[0] = 99;

        Assert.That(unknownChunk.Chunk.Data[0], Is.EqualTo(1));
    }

    [Test]
    public void CreateOutputChunk_RejectsDroppedOrInvalidEntries()
    {
        PngUnknownChunk dropped = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[0], 0), 0);
        dropped.Disposition = PngChunkDisposition.Drop;
        PngUnknownChunk invalidDisposition = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[0], 0), 0);
        invalidDisposition.Disposition = (PngChunkDisposition)99;
        PngUnknownChunk nullReplacement = PngUnknownChunkPreservation.Create(CreateChunk("vpAg", new byte[0], 0), 0);
        nullReplacement.Disposition = PngChunkDisposition.Replace;
        nullReplacement.ReplacementData = null!;

        Assert.Throws<ArgumentNullException>(() => PngUnknownChunkPreservation.ShouldWrite(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngUnknownChunkPreservation.ShouldWrite(invalidDisposition));
        Assert.Throws<InvalidOperationException>(() => PngUnknownChunkPreservation.CreateOutputChunk(dropped));
        Assert.Throws<ArgumentNullException>(() => PngUnknownChunkPreservation.CreateOutputChunk(null!));
        Assert.Throws<ArgumentNullException>(() => PngUnknownChunkPreservation.CreateOutputChunk(nullReplacement));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data, long offset)
    {
        PngChunk chunk = new PngChunk();
        PngChunkType parsed = PngChunkTypeParser.Parse((byte)chunkType[0], (byte)chunkType[1], (byte)chunkType[2], (byte)chunkType[3]);
        chunk.Type = chunkType;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.Crc = PngCrc32.CalculateChunkCrc(chunkType, data);
        chunk.Offset = offset;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunkType);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(parsed);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(parsed);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(parsed);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(parsed);
        return chunk;
    }
}
