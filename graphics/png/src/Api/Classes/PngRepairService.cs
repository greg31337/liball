using System.IO;

namespace png;

/// <summary>Default service for recoverable PNG ancillary repairs.</summary>
public class PngRepairService : IPngRepairer
{
    /// <summary>Creates an independent PNG repair service.</summary>
    public PngRepairService()
    {
    }

    /// <summary>Performs explicitly selected recoverable repairs on PNG bytes and returns a report.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    public PngRepairResult RepairBytes(byte[] data, PngRepairOptions options) { return PngRepairer.Repair(data, 0, options); }

    /// <summary>Performs explicitly selected recoverable repairs on offset PNG bytes and returns a report.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    public PngRepairResult RepairBytesAtOffset(byte[] data, int offset, PngRepairOptions options) { return PngRepairer.Repair(data, offset, options); }

    /// <summary>Performs explicitly selected recoverable repairs on a PNG stream and returns a report.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    public PngRepairResult RepairStream(Stream stream, PngRepairOptions options) { return PngRepairer.Repair(stream, options); }

    /// <summary>Performs explicitly selected recoverable repairs on a chunk list and returns a report.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    public PngRepairResult RepairChunks(PngChunk[] chunks, PngRepairOptions options) { return PngRepairer.Repair(chunks, options); }

    /// <summary>Repairs ancillary chunk ordering in PNG bytes.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryOrderingBytes(byte[] data) { return Png.RepairAncillaryOrderingBytes(data); }

    /// <summary>Repairs ancillary chunk ordering in offset PNG bytes.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryOrderingBytesAtOffset(byte[] data, int offset) { return Png.RepairAncillaryOrderingBytes(data, offset); }

    /// <summary>Repairs ancillary chunk ordering in a PNG stream.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryOrderingStream(Stream stream) { return Png.RepairAncillaryOrderingStream(stream); }

    /// <summary>Repairs ancillary chunk ordering in a chunk list.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The repaired chunks.</returns>
    public PngChunk[] RepairAncillaryOrderingChunks(PngChunk[] chunks) { return Png.RepairAncillaryOrderingChunks(chunks); }

    /// <summary>Repairs ancillary CRC values in PNG bytes.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryCrcBytes(byte[] data) { return Png.RepairAncillaryCrcBytes(data); }

    /// <summary>Repairs ancillary CRC values in offset PNG bytes.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryCrcBytesAtOffset(byte[] data, int offset) { return Png.RepairAncillaryCrcBytes(data, offset); }

    /// <summary>Repairs ancillary CRC values in a PNG stream.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    public byte[] RepairAncillaryCrcStream(Stream stream) { return Png.RepairAncillaryCrcStream(stream); }

    /// <summary>Repairs ancillary CRC values in a chunk list.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The repaired chunks.</returns>
    public PngChunk[] RepairAncillaryCrcChunks(PngChunk[] chunks) { return Png.RepairAncillaryCrcChunks(chunks); }
}
