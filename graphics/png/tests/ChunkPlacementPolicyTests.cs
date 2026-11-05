using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkPlacementPolicyTests
{
    [Test]
    public void ReadChunks_RecordsPlacementAndPreservationState()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 }),
            CreateChunk(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 2 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 67, 0, 68 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };

        byte[] png = Png.WriteChunks(chunks);
        PngChunkListResult result = Png.ReadChunks(png);
        PngIdentifyResult identify = Png.IdentifyBytes(png);

        Assert.That(result.Chunks[0].Placement, Is.EqualTo(PngChunkPlacement.BeforePalette));
        Assert.That(result.Chunks[1].Placement, Is.EqualTo(PngChunkPlacement.BeforePalette));
        Assert.That(result.Chunks[2].Placement, Is.EqualTo(PngChunkPlacement.BeforePalette));
        Assert.That(result.Chunks[3].Placement, Is.EqualTo(PngChunkPlacement.BeforeImageData));
        Assert.That(result.Chunks[4].Placement, Is.EqualTo(PngChunkPlacement.BeforeImageData));
        Assert.That(result.Chunks[5].Placement, Is.EqualTo(PngChunkPlacement.ImageData));
        Assert.That(result.Chunks[6].Placement, Is.EqualTo(PngChunkPlacement.ImageData));
        Assert.That(result.Chunks[7].Placement, Is.EqualTo(PngChunkPlacement.AfterImageData));
        Assert.That(result.Chunks[8].Placement, Is.EqualTo(PngChunkPlacement.AfterImageData));
        Assert.That(result.Chunks, Has.All.Matches<PngChunk>(chunk => chunk.PreservationState == PngChunkPreservationState.Preserved));
        Assert.That(identify.Metadata.Chunks, Has.All.Matches<PngChunk>(chunk => chunk.PreservationState == PngChunkPreservationState.Preserved));
        Assert.That(identify.Metadata.Chunks[1].Placement, Is.EqualTo(PngChunkPlacement.BeforePalette));
        Assert.That(identify.Metadata.Chunks[5].Placement, Is.EqualTo(PngChunkPlacement.AfterImageData));
    }

    [Test]
    public void WriteChunksWithOptions_PreservePolicyRetainsCallerOrder()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 }),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 2 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };
        PngChunkWriteOptions options = new PngChunkWriteOptions();
        options.PlacementPolicy = PngChunkPlacementPolicy.Preserve;
        IPngChunkCodec codec = new PngChunkService();

        PngChunkListResult result = codec.ReadChunks(codec.WriteChunksWithOptions(chunks, options));

        Assert.That(GetTypes(result.Chunks), Is.EqualTo(new string[] { "IHDR", "IDAT", "tEXt", "IDAT", "IEND" }));
    }

    [Test]
    public void WriteChunksWithOptions_CanonicalizesAllPlacementRegionsAndIdatContiguity()
    {
        PngChunk after = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 });
        after.Placement = PngChunkPlacement.AfterImageData;
        PngChunk beforePalette = CreateChunk("vpAg", new byte[] { 9 });
        beforePalette.Placement = PngChunkPlacement.BeforePalette;
        PngChunk[] chunks = new PngChunk[]
        {
            after,
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 2 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 1 }),
            CreateChunk(PngConstants.ChunkPLTE, new byte[] { 1, 2, 3 }),
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 0, 1 }),
            beforePalette,
            CreateChunk(PngConstants.ChunkIHDR, CreateIhdrData()),
            CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1 }),
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };
        PngChunkWriteOptions options = new PngChunkWriteOptions();
        options.PlacementPolicy = PngChunkPlacementPolicy.Canonicalize;
        IPngChunkCodec codec = new PngChunkService();

        PngChunkListResult result = codec.ReadChunks(codec.WriteChunksWithOptions(chunks, options));

        Assert.That(GetTypes(result.Chunks), Is.EqualTo(new string[] { "IHDR", "gAMA", "vpAg", "PLTE", "pHYs", "IDAT", "IDAT", "tEXt", "IEND" }));
        Assert.That(result.Chunks[5].Data, Is.EqualTo(new byte[] { 2 }));
        Assert.That(result.Chunks[6].Data, Is.EqualTo(new byte[] { 1 }));
    }

    [Test]
    public void WriteChunksStreamWithOptions_UsesPolicyAndLimits()
    {
        PngChunk text = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 0, 66 });
        text.Placement = PngChunkPlacement.AfterImageData;
        PngChunk[] chunks = new PngChunk[]
        {
            text,
            CreateChunk(PngConstants.ChunkIEND, new byte[0])
        };
        PngChunkWriteOptions options = new PngChunkWriteOptions();
        options.PlacementPolicy = PngChunkPlacementPolicy.Canonicalize;
        options.Limits.MaxChunkCount = 2;
        IPngChunkCodec codec = new PngChunkService();
        MemoryStream stream = new MemoryStream();

        codec.WriteChunksStreamWithOptions(stream, chunks, options);

        Assert.That(GetTypes(codec.ReadChunks(stream.ToArray()).Chunks), Is.EqualTo(new string[] { "tEXt", "IEND" }));
        options.Limits.MaxChunkCount = 1;
        Assert.Throws<OverflowException>(() => codec.WriteChunksStreamWithOptions(new MemoryStream(), chunks, options));
    }

    [Test]
    public void WriteChunksWithOptions_RejectsInvalidPolicyInputs()
    {
        PngChunk iend = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        PngChunk invalidPlacement = CreateChunk(PngConstants.ChunkText, new byte[] { 1, 0, 2 });
        invalidPlacement.Placement = PngChunkPlacement.ImageData;
        PngChunkWriteOptions options = new PngChunkWriteOptions();
        options.PlacementPolicy = PngChunkPlacementPolicy.Canonicalize;

        Assert.Throws<ArgumentNullException>(() => Png.WriteChunksWithOptions(new PngChunk[] { iend }, null!));
        options.Limits = null!;
        Assert.Throws<ArgumentNullException>(() => Png.WriteChunksWithOptions(new PngChunk[] { iend }, options));
        options.Limits = new PngSecurityLimits();
        Assert.Throws<ArgumentException>(() => Png.WriteChunksWithOptions(new PngChunk[] { invalidPlacement, iend }, options));
        options.PlacementPolicy = (PngChunkPlacementPolicy)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.WriteChunksWithOptions(new PngChunk[] { iend }, options));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        return chunk;
    }

    static byte[] CreateIhdrData()
    {
        byte[] data = new byte[13];
        data[3] = 1;
        data[7] = 1;
        data[8] = PngConstants.BitDepth8;
        data[9] = PngConstants.ColorTypeIndexedColor;
        return data;
    }

    static string[] GetTypes(PngChunk[] chunks)
    {
        string[] types = new string[chunks.Length];
        int index = 0;
        while (index < chunks.Length)
        {
            types[index] = chunks[index].Type;
            index += 1;
        }

        return types;
    }
}
