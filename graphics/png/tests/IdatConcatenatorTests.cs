using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class IdatConcatenatorTests
{
    [Test]
    public void Concatenate_JoinsIdatPayloadsInChunkOrder()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, new byte[] { 9 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 8, 8, 8 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[0]),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 3, 4, 5 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };

        byte[] result = PngIdatConcatenator.Concatenate(chunks);

        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Concatenate_AcceptsChunkListResult()
    {
        PngChunkListResult chunkList = new PngChunkListResult();
        chunkList.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 10 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 11, 12 })
        };

        byte[] result = PngIdatConcatenator.Concatenate(chunkList);

        Assert.That(result, Is.EqualTo(new byte[] { 10, 11, 12 }));
    }

    [Test]
    public void Concatenate_IgnoresCriticalNonIdatChunks()
    {
        PngChunk ihdr = new PngChunk();
        ihdr.Type = PngConstants.ChunkIHDR;
        ihdr.Data = null!;
        PngChunk iend = new PngChunk();
        iend.Type = PngConstants.ChunkIEND;
        iend.Data = null!;
        PngChunk[] chunks = new PngChunk[]
        {
            ihdr,
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2 }),
            iend
        };

        byte[] result = PngIdatConcatenator.Concatenate(chunks);

        Assert.That(result, Is.EqualTo(new byte[] { 1, 2 }));
    }

    [Test]
    public void Concatenate_ReturnsEmptyArrayWhenNoIdatChunksExist()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, new byte[0]),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };

        byte[] result = PngIdatConcatenator.Concatenate(chunks);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Concatenate_EnforcesCumulativeIdatLimit()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 3, 4 })
        };
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = 3;

        Assert.Throws<OverflowException>(() => PngIdatConcatenator.Concatenate(chunks, limits));
    }

    [Test]
    public void Concatenate_AllowsUnlimitedIdatWhenLimitIsZero()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 3, 4 })
        };
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxIdatBytes = 0;

        byte[] result = PngIdatConcatenator.Concatenate(chunks, limits);

        Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Concatenate_RejectsInvalidInputs()
    {
        PngChunk valid = CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 });
        PngChunk nullType = CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 });
        nullType.Type = null!;
        PngChunk nullData = CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 });
        nullData.Data = null!;
        PngChunkListResult nullChunks = new PngChunkListResult();
        nullChunks.Chunks = null!;

        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate(new PngChunk[] { valid }, null!));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate((PngChunkListResult)null!));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate(nullChunks));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngIdatConcatenator.Concatenate(new PngChunk[] { nullData }));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        return chunk;
    }
}
