namespace png;

/// <summary>Default PNG CRC-32 calculator.</summary>
public class PngCrcCalculator : IPngCrcCalculator
{
    /// <summary>Creates an independent CRC calculator.</summary>
    public PngCrcCalculator()
    {
    }

    /// <summary>Calculates CRC-32 over a bounded span.</summary><param name="data">Input.</param><returns>CRC.</returns>
    public uint CalculateSpan(ReadOnlySpan<byte> data) { return PngCrc32.CalculateSpan(data); }
    /// <summary>Calculates CRC-32 over an array.</summary><param name="data">Input.</param><returns>CRC.</returns>
    public uint Calculate(byte[] data) { return PngCrc32.Calculate(data); }
    /// <summary>Calculates CRC-32 over a range.</summary><param name="data">Input.</param><param name="offset">Offset.</param><param name="count">Count.</param><returns>CRC.</returns>
    public uint CalculateRange(byte[] data, int offset, int count) { return PngCrc32.Calculate(data, offset, count); }
    /// <summary>Calculates a chunk CRC from a name.</summary><param name="chunkType">Type.</param><param name="data">Payload.</param><returns>CRC.</returns>
    public uint CalculateChunkFromName(string chunkType, byte[] data) { return PngCrc32.CalculateChunkCrc(chunkType, data); }
    /// <summary>Calculates a chunk CRC from type bytes.</summary><param name="chunkType">Type bytes.</param><param name="data">Payload.</param><returns>CRC.</returns>
    public uint CalculateChunkFromBytes(byte[] chunkType, byte[] data) { return PngCrc32.CalculateChunkCrc(chunkType, data); }
    /// <summary>Calculates a chunk CRC from a name and bounded payload.</summary><param name="chunkType">Type.</param><param name="data">Payload.</param><returns>CRC.</returns>
    public uint CalculateChunkFromNameSpan(string chunkType, ReadOnlySpan<byte> data) { return PngCrc32.CalculateChunkCrcSpan(chunkType, data); }
    /// <summary>Calculates a chunk CRC from bounded type and payload spans.</summary><param name="chunkType">Type bytes.</param><param name="data">Payload.</param><returns>CRC.</returns>
    public uint CalculateChunkFromSpans(ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> data) { return PngCrc32.CalculateChunkCrcSpans(chunkType, data); }
}
