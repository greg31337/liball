using System.IO;

namespace png;

/// <summary>Default service for low-level PNG chunk reading and writing.</summary>
public class PngChunkService : IPngChunkCodec
{
    /// <summary>Creates an independent PNG chunk service.</summary>
    public PngChunkService()
    {
    }

    /// <summary>Reads one framed chunk from a bounded span using default limits.</summary>
    /// <param name="data">The chunk length, type, payload, and CRC bytes.</param>
    /// <returns>The parsed raw chunk.</returns>
    public PngChunk ReadChunkSpan(ReadOnlySpan<byte> data) { return Png.ReadChunkSpan(data); }

    /// <summary>Reads one framed chunk from a bounded span using explicit limits.</summary>
    /// <param name="data">The chunk length, type, payload, and CRC bytes.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The parsed raw chunk.</returns>
    public PngChunk ReadChunkSpanWithLimits(ReadOnlySpan<byte> data, PngSecurityLimits limits) { return Png.ReadChunkSpanWithLimits(data, limits); }

    /// <summary>Writes one framed chunk into a caller-provided span using default limits.</summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteChunkSpan(Span<byte> destination, PngChunk chunk) { return Png.WriteChunkSpan(destination, chunk); }

    /// <summary>Writes one framed chunk into a caller-provided span using explicit limits.</summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteChunkSpanWithLimits(Span<byte> destination, PngChunk chunk, PngSecurityLimits limits) { return Png.WriteChunkSpanWithLimits(destination, chunk, limits); }

    /// <summary>Reads chunks from PNG bytes using default limits.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunks(byte[] data) { return Png.ReadChunks(data); }

    /// <summary>Reads chunks from PNG bytes using explicit limits.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunksWithLimits(byte[] data, PngSecurityLimits limits) { return Png.ReadChunks(data, limits); }

    /// <summary>Reads chunks from offset PNG bytes using default limits.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunksAtOffset(byte[] data, int offset) { return Png.ReadChunks(data, offset); }

    /// <summary>Reads chunks from offset PNG bytes using explicit limits.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunksAtOffsetWithLimits(byte[] data, int offset, PngSecurityLimits limits) { return Png.ReadChunks(data, offset, limits); }

    /// <summary>Reads chunks from a stream using default limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunksStream(Stream stream) { return Png.ReadChunks(stream); }

    /// <summary>Reads chunks from a stream using explicit limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    public PngChunkListResult ReadChunksStreamWithLimits(Stream stream, PngSecurityLimits limits) { return Png.ReadChunks(stream, limits); }

    /// <summary>Writes chunks to a PNG byte array using default limits.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The complete PNG datastream.</returns>
    public byte[] WriteChunks(PngChunk[] chunks) { return Png.WriteChunks(chunks); }

    /// <summary>Writes chunks to a PNG byte array using explicit limits.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The complete PNG datastream.</returns>
    public byte[] WriteChunksWithLimits(PngChunk[] chunks, PngSecurityLimits limits) { return Png.WriteChunks(chunks, limits); }

    /// <summary>Writes chunks to a PNG byte array using explicit placement policy and limits.</summary>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and security limits.</param>
    /// <returns>The complete PNG datastream.</returns>
    public byte[] WriteChunksWithOptions(PngChunk[] chunks, PngChunkWriteOptions options) { return Png.WriteChunksWithOptions(chunks, options); }

    /// <summary>Writes chunks to a stream using default limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks in datastream order.</param>
    public void WriteChunksStream(Stream stream, PngChunk[] chunks) { Png.WriteChunks(stream, chunks); }

    /// <summary>Writes chunks to a stream using explicit limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="limits">The security limits.</param>
    public void WriteChunksStreamWithLimits(Stream stream, PngChunk[] chunks, PngSecurityLimits limits) { Png.WriteChunks(stream, chunks, limits); }

    /// <summary>Writes chunks to a stream using explicit placement policy and limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and security limits.</param>
    public void WriteChunksStreamWithOptions(Stream stream, PngChunk[] chunks, PngChunkWriteOptions options) { Png.WriteChunksStreamWithOptions(stream, chunks, options); }
}
