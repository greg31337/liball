using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SpanChunkAndCrcContractTests
{
    [Test]
    public void ChunkCodec_SpanOperationsRoundTripWithinCallerOwnedStorage()
    {
        IPngChunkCodec codec = new PngChunkService();
        PngChunk source = CreateChunk(PngConstants.ChunkText, new byte[] { 65, 66, 67 });
        Span<byte> storage = stackalloc byte[19];
        storage.Fill(0xAA);

        int written = codec.WriteChunkSpan(storage.Slice(2, 15), source);
        PngChunk result = codec.ReadChunkSpan(storage.Slice(2, written));

        Assert.That(written, Is.EqualTo(15));
        Assert.That(storage[0], Is.EqualTo(0xAA));
        Assert.That(storage[1], Is.EqualTo(0xAA));
        Assert.That(storage[17], Is.EqualTo(0xAA));
        Assert.That(storage[18], Is.EqualTo(0xAA));
        Assert.That(result.Type, Is.EqualTo(PngConstants.ChunkText));
        Assert.That(result.Data, Is.EqualTo(source.Data));
        Assert.That(result.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(source.Type, source.Data)));
        Assert.That(result.PreservationState, Is.EqualTo(PngChunkPreservationState.Preserved));
    }

    [Test]
    public void ChunkCodec_SpanOperationsHonorLimitsAndBounds()
    {
        IPngChunkCodec codec = new PngChunkService();
        PngChunk chunk = CreateChunk(PngConstants.ChunkText, new byte[] { 1, 2, 3 });
        byte[] framed = PngRawChunkWriter.WriteToBytes(chunk);
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxMetadataBytes = 2;

        Assert.Throws<OverflowException>(() => ReadChunkSpanWithLimits(codec, framed, limits));
        Assert.Throws<OverflowException>(() => WriteChunkSpanWithLimits(codec, chunk, new byte[15], limits));
        Assert.Throws<ArgumentException>(() => WriteChunkSpan(codec, chunk, new byte[14]));
        Assert.Throws<EndOfStreamException>(() => ReadChunkSpan(codec, framed.AsSpan(0, framed.Length - 1).ToArray()));
    }

    [Test]
    public void CrcCalculator_ReadOnlySpanOperationsMatchKnownVectorsAndArrayMethods()
    {
        IPngCrcCalculator calculator = new PngCrcCalculator();
        byte[] bounded = new byte[] { 0xAA, 49, 50, 51, 52, 53, 54, 55, 56, 57, 0xBB };
        ReadOnlySpan<byte> digits = bounded.AsSpan(1, 9);
        ReadOnlySpan<byte> type = new byte[] { 73, 72, 68, 82 };
        ReadOnlySpan<byte> payload = new byte[]
        {
            0, 0, 0, 1,
            0, 0, 0, 1,
            8, 2, 0, 0, 0
        };

        Assert.That(calculator.CalculateSpan(digits), Is.EqualTo(0xCBF43926u));
        Assert.That(calculator.CalculateChunkFromNameSpan(PngConstants.ChunkIHDR, payload), Is.EqualTo(0x907753DEu));
        Assert.That(calculator.CalculateChunkFromSpans(type, payload), Is.EqualTo(0x907753DEu));
        Assert.That(calculator.CalculateSpan(ReadOnlySpan<byte>.Empty), Is.EqualTo(calculator.Calculate(new byte[0])));
    }

    [Test]
    public void CrcCalculator_SpanChunkTypeValidationMatchesArrayOperations()
    {
        IPngCrcCalculator calculator = new PngCrcCalculator();

        Assert.Throws<ArgumentException>(() => CalculateInvalidTypeSpan(calculator, new byte[] { 73, 72, 68 }));
        Assert.Throws<ArgumentException>(() => CalculateInvalidNameSpan(calculator, "ABC"));
        Assert.Throws<ArgumentException>(() => CalculateInvalidNameSpan(calculator, "IH\u0100R"));
    }

    static PngChunk CreateChunk(string chunkType, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        chunk.Data = data;
        return chunk;
    }

    static PngChunk ReadChunkSpan(IPngChunkCodec codec, byte[] data)
    {
        return codec.ReadChunkSpan(data);
    }

    static PngChunk ReadChunkSpanWithLimits(IPngChunkCodec codec, byte[] data, PngSecurityLimits limits)
    {
        return codec.ReadChunkSpanWithLimits(data, limits);
    }

    static int WriteChunkSpan(IPngChunkCodec codec, PngChunk chunk, byte[] destination)
    {
        return codec.WriteChunkSpan(destination, chunk);
    }

    static int WriteChunkSpanWithLimits(IPngChunkCodec codec, PngChunk chunk, byte[] destination, PngSecurityLimits limits)
    {
        return codec.WriteChunkSpanWithLimits(destination, chunk, limits);
    }

    static uint CalculateInvalidTypeSpan(IPngCrcCalculator calculator, byte[] chunkType)
    {
        return calculator.CalculateChunkFromSpans(chunkType, ReadOnlySpan<byte>.Empty);
    }

    static uint CalculateInvalidNameSpan(IPngCrcCalculator calculator, string chunkType)
    {
        return calculator.CalculateChunkFromNameSpan(chunkType, ReadOnlySpan<byte>.Empty);
    }
}
