using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkListReaderTests
{
    [Test]
    public void PngChunkListResult_UsesPublicFieldsOnly()
    {
        Assert.That(typeof(PngChunkListResult).GetProperties(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngChunkListResult).GetEvents(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        Assert.That(typeof(PngChunkListResult).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(PngChunkListResult).GetFields(BindingFlags.Public | BindingFlags.Instance), Has.Length.EqualTo(4));
    }

    [Test]
    public void Read_ArrayStopsAtIendAndReportsTrailingBytes()
    {
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });
        byte[] iend = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] trailing = new byte[] { 9, 8, 7 };
        byte[] data = Join(new byte[] { 0xAA, 0xBB }, text, iend, trailing);

        PngChunkListResult result = PngChunkListReader.Read(data, 2);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(result.Chunks[1].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(result.TrailingBytes, Is.EqualTo(trailing));
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Read_ArrayPreservesChunkOffsetsFromStartOffset()
    {
        byte[] prefix = new byte[] { 0xAA, 0xBB, 0xCC };
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65, 66 });
        byte[] iend = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] data = Join(prefix, text, iend);

        PngChunkListResult result = PngChunkListReader.Read(data, prefix.Length);

        Assert.That(result.Chunks[0].Offset, Is.EqualTo(3));
        Assert.That(result.Chunks[1].Offset, Is.EqualTo(17));
        Assert.That(result.TrailingBytes, Is.Empty);
    }

    [Test]
    public void Read_ArrayAtEndReturnsMissingIendWarningWithoutChunks()
    {
        byte[] data = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });

        PngChunkListResult result = PngChunkListReader.Read(data, data.Length);

        Assert.That(result.FoundIend, Is.False);
        Assert.That(result.Chunks, Is.Empty);
        Assert.That(result.TrailingBytes, Is.Empty);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_IEND_MISSING"));
    }

    [Test]
    public void Read_ArrayTreatsChunksAfterIendAsTrailingBytes()
    {
        byte[] iend = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] laterChunk = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65, 66 });
        byte[] data = Join(iend, laterChunk);

        PngChunkListResult result = PngChunkListReader.Read(data, 0);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(1));
        Assert.That(result.Chunks[0].Type, Is.EqualTo(PngConstants.ChunkIEND));
        Assert.That(result.TrailingBytes, Is.EqualTo(laterChunk));
    }

    [Test]
    public void Read_StreamStopsAtIendAndReportsTrailingBytes()
    {
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });
        byte[] iend = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] trailing = new byte[] { 1, 2 };
        MemoryStream stream = new MemoryStream(Join(new byte[] { 0xAA }, text, iend, trailing));
        stream.Position = 1;

        PngChunkListResult result = PngChunkListReader.Read(stream);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(2));
        Assert.That(result.TrailingBytes, Is.EqualTo(trailing));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }

    [Test]
    public void Read_StreamWithoutIendReturnsWarning()
    {
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });
        MemoryStream stream = new MemoryStream(text);

        PngChunkListResult result = PngChunkListReader.Read(stream);

        Assert.That(result.FoundIend, Is.False);
        Assert.That(result.Chunks, Has.Length.EqualTo(1));
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_IEND_MISSING"));
    }

    [Test]
    public void Read_ArrayWithoutIendReturnsWarning()
    {
        byte[] data = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });

        PngChunkListResult result = PngChunkListReader.Read(data, 0);

        Assert.That(result.FoundIend, Is.False);
        Assert.That(result.Chunks, Has.Length.EqualTo(1));
        Assert.That(result.TrailingBytes, Is.Empty);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_IEND_MISSING"));
    }

    [Test]
    public void Read_EnforcesChunkCountLimit()
    {
        byte[] text = CreateChunkBytes(PngConstants.ChunkText, new byte[] { 65 });
        byte[] iend = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        byte[] data = Join(text, iend);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = 1;

        Assert.Throws<OverflowException>(() => PngChunkListReader.Read(data, 0, limits));
    }

    [Test]
    public void Read_AllowsIendWithinChunkCountLimit()
    {
        byte[] data = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxChunkCount = 1;

        PngChunkListResult result = PngChunkListReader.Read(data, 0, limits);

        Assert.That(result.FoundIend, Is.True);
        Assert.That(result.Chunks, Has.Length.EqualTo(1));
    }

    [Test]
    public void Read_RejectsInvalidInputs()
    {
        byte[] data = CreateChunkBytes(PngConstants.ChunkIEND, new byte[0]);

        Assert.Throws<ArgumentNullException>(() => PngChunkListReader.Read((byte[])null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngChunkListReader.Read(data, 0, null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkListReader.Read((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkListReader.Read(new MemoryStream(data), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChunkListReader.Read(data, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChunkListReader.Read(data, data.Length + 1));
    }

    static byte[] CreateChunkBytes(string chunkType, byte[] chunkData)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = chunkData;
        return PngRawChunkWriter.WriteToBytes(chunk);
    }

    static byte[] Join(params byte[][] arrays)
    {
        int totalLength = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            totalLength += arrays[index].Length;
            index += 1;
        }

        byte[] result = new byte[totalLength];
        int offset = 0;
        index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }
}
