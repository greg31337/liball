using System.IO;

namespace png;

/// <summary>Identifies PNG data without decoding pixel samples.</summary>
public interface IPngIdentifier
{
    /// <summary>Identifies PNG bytes using default options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The identification result.</returns>
    PngIdentifyResult IdentifyBytes(byte[] data);

    /// <summary>Identifies PNG bytes using explicit options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    PngIdentifyResult IdentifyBytesWithOptions(byte[] data, PngDecoderOptions options);

    /// <summary>Identifies PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    PngIdentifyResult IdentifyBytesAtOffset(byte[] data, int offset, PngDecoderOptions options);

    /// <summary>Identifies PNG data from a stream using default options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The identification result.</returns>
    PngIdentifyResult IdentifyStream(Stream stream);

    /// <summary>Identifies PNG data from a stream using explicit options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    PngIdentifyResult IdentifyStreamWithOptions(Stream stream, PngDecoderOptions options);
}

/// <summary>Validates and inspects PNG datastreams.</summary>
public interface IPngValidator
{
    /// <summary>Validates PNG bytes using strict defaults.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateBytes(byte[] data);

    /// <summary>Validates PNG bytes using explicit options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateBytesWithOptions(byte[] data, PngDecoderOptions options);

    /// <summary>Validates PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateBytesAtOffset(byte[] data, int offset, PngDecoderOptions options);

    /// <summary>Validates a PNG stream using strict defaults.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateStream(Stream stream);

    /// <summary>Validates a PNG stream using explicit options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateStreamWithOptions(Stream stream, PngDecoderOptions options);

    /// <summary>Inspects PNG bytes and reports as many issues as possible.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The inspection result.</returns>
    PngValidationResult InspectBestEffortBytes(byte[] data);

    /// <summary>Inspects PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The inspection result.</returns>
    PngValidationResult InspectBestEffortBytesAtOffset(byte[] data, int offset);

    /// <summary>Inspects a PNG stream and reports as many issues as possible.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The inspection result.</returns>
    PngValidationResult InspectBestEffortStream(Stream stream);

    /// <summary>Validates PNG bytes using tolerant repair policy.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateTolerantRepairBytes(byte[] data);

    /// <summary>Validates offset PNG bytes using tolerant repair policy.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateTolerantRepairBytesAtOffset(byte[] data, int offset);

    /// <summary>Validates a PNG stream using tolerant repair policy.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The validation result.</returns>
    PngValidationResult ValidateTolerantRepairStream(Stream stream);
}

/// <summary>Reads and writes complete low-level PNG chunk lists and individually framed chunks.</summary>
public interface IPngChunkCodec
{
    /// <summary>Reads one framed chunk from a bounded span using default limits.</summary>
    /// <param name="data">The chunk length, type, payload, and CRC bytes.</param>
    /// <returns>The parsed raw chunk.</returns>
    PngChunk ReadChunkSpan(ReadOnlySpan<byte> data);

    /// <summary>Reads one framed chunk from a bounded span using explicit limits.</summary>
    /// <param name="data">The chunk length, type, payload, and CRC bytes.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The parsed raw chunk.</returns>
    PngChunk ReadChunkSpanWithLimits(ReadOnlySpan<byte> data, PngSecurityLimits limits);

    /// <summary>Writes one framed chunk into a caller-provided span using default limits.</summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <returns>The number of bytes written.</returns>
    int WriteChunkSpan(Span<byte> destination, PngChunk chunk);

    /// <summary>Writes one framed chunk into a caller-provided span using explicit limits.</summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The number of bytes written.</returns>
    int WriteChunkSpanWithLimits(Span<byte> destination, PngChunk chunk, PngSecurityLimits limits);

    /// <summary>Reads chunks from PNG bytes using default limits.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunks(byte[] data);

    /// <summary>Reads chunks from PNG bytes using explicit limits.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunksWithLimits(byte[] data, PngSecurityLimits limits);

    /// <summary>Reads chunks from offset PNG bytes using default limits.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunksAtOffset(byte[] data, int offset);

    /// <summary>Reads chunks from offset PNG bytes using explicit limits.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunksAtOffsetWithLimits(byte[] data, int offset, PngSecurityLimits limits);

    /// <summary>Reads chunks from a stream using default limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunksStream(Stream stream);

    /// <summary>Reads chunks from a stream using explicit limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The ordered chunk list.</returns>
    PngChunkListResult ReadChunksStreamWithLimits(Stream stream, PngSecurityLimits limits);

    /// <summary>Writes chunks to a PNG byte array using default limits.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The complete PNG datastream.</returns>
    byte[] WriteChunks(PngChunk[] chunks);

    /// <summary>Writes chunks to a PNG byte array using explicit limits.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="limits">The security limits.</param>
    /// <returns>The complete PNG datastream.</returns>
    byte[] WriteChunksWithLimits(PngChunk[] chunks, PngSecurityLimits limits);

    /// <summary>Writes chunks to a PNG byte array using explicit placement policy and limits.</summary>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and security limits.</param>
    /// <returns>The complete PNG datastream.</returns>
    byte[] WriteChunksWithOptions(PngChunk[] chunks, PngChunkWriteOptions options);

    /// <summary>Writes chunks to a stream using default limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks in datastream order.</param>
    void WriteChunksStream(Stream stream, PngChunk[] chunks);

    /// <summary>Writes chunks to a stream using explicit limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="limits">The security limits.</param>
    void WriteChunksStreamWithLimits(Stream stream, PngChunk[] chunks, PngSecurityLimits limits);

    /// <summary>Writes chunks to a stream using explicit placement policy and limits.</summary>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and security limits.</param>
    void WriteChunksStreamWithOptions(Stream stream, PngChunk[] chunks, PngChunkWriteOptions options);
}

/// <summary>Repairs explicitly selected recoverable PNG issues and reports every performed change.</summary>
public interface IPngRepairer
{
    /// <summary>Performs explicitly selected recoverable repairs on PNG bytes and returns a report.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    PngRepairResult RepairBytes(byte[] data, PngRepairOptions options);

    /// <summary>Performs explicitly selected recoverable repairs on offset PNG bytes and returns a report.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    PngRepairResult RepairBytesAtOffset(byte[] data, int offset, PngRepairOptions options);

    /// <summary>Performs explicitly selected recoverable repairs on a PNG stream and returns a report.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    PngRepairResult RepairStream(Stream stream, PngRepairOptions options);

    /// <summary>Performs explicitly selected recoverable repairs on a chunk list and returns a report.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <param name="options">The repairs and limits to apply.</param>
    /// <returns>The repaired data, chunks, and repair report.</returns>
    PngRepairResult RepairChunks(PngChunk[] chunks, PngRepairOptions options);

    /// <summary>Repairs ancillary chunk ordering in PNG bytes.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryOrderingBytes(byte[] data);

    /// <summary>Repairs ancillary chunk ordering in offset PNG bytes.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryOrderingBytesAtOffset(byte[] data, int offset);

    /// <summary>Repairs ancillary chunk ordering in a PNG stream.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryOrderingStream(Stream stream);

    /// <summary>Repairs ancillary chunk ordering in a chunk list.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The repaired chunks.</returns>
    PngChunk[] RepairAncillaryOrderingChunks(PngChunk[] chunks);

    /// <summary>Repairs ancillary CRC values in PNG bytes.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryCrcBytes(byte[] data);

    /// <summary>Repairs ancillary CRC values in offset PNG bytes.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryCrcBytesAtOffset(byte[] data, int offset);

    /// <summary>Repairs ancillary CRC values in a PNG stream.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The repaired PNG datastream.</returns>
    byte[] RepairAncillaryCrcStream(Stream stream);

    /// <summary>Repairs ancillary CRC values in a chunk list.</summary>
    /// <param name="chunks">The chunks in datastream order.</param>
    /// <returns>The repaired chunks.</returns>
    PngChunk[] RepairAncillaryCrcChunks(PngChunk[] chunks);
}

/// <summary>Creates PNG images and pixel buffers.</summary>
public interface IPngImageFactory
{
    /// <summary>Creates a blank image.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <returns>The created image.</returns>
    PngImage CreateImage(int width, int height, PngPixelFormat pixelFormat);

    /// <summary>Creates an image from caller-supplied pixel bytes.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="data">The pixel bytes.</param>
    /// <returns>The created image.</returns>
    PngImage CreateImageWithData(int width, int height, PngPixelFormat pixelFormat, byte[] data);

    /// <summary>Creates a tightly packed image from a bounded span, copying the supplied pixels.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="data">The tightly packed pixel bytes.</param>
    /// <returns>The created image.</returns>
    PngImage CreateImageFromSpan(int width, int height, PngPixelFormat pixelFormat, ReadOnlySpan<byte> data);

    /// <summary>Creates an image with a caller-supplied row stride, copying the supplied pixels.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The pixel bytes including any row padding.</param>
    /// <returns>The created image.</returns>
    PngImage CreateImageWithStride(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data);

    /// <summary>Creates an indexed image with its palette, copying both indices and palette entries.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The indexed pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The packed or unpacked palette indices.</param>
    /// <param name="palette">The palette referenced by the indices.</param>
    /// <returns>The created indexed image.</returns>
    PngImage CreateIndexedImage(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngPalette palette);

    /// <summary>Creates an image with caller-supplied metadata and copied pixel bytes.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The pixel bytes including any row padding.</param>
    /// <param name="metadata">The metadata model to attach to the image.</param>
    /// <returns>The created image.</returns>
    PngImage CreateImageWithMetadata(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngMetadata metadata);

    /// <summary>Creates an APNG frame rectangle with copied pixel bytes.</summary>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    /// <param name="xOffset">The horizontal canvas offset.</param>
    /// <param name="yOffset">The vertical canvas offset.</param>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <param name="stride">The byte distance between consecutive rows.</param>
    /// <param name="data">The frame pixel bytes.</param>
    /// <param name="delayNumerator">The frame delay numerator.</param>
    /// <param name="delayDenominator">The frame delay denominator.</param>
    /// <param name="disposeOperation">The post-display disposal operation.</param>
    /// <param name="blendOperation">The canvas blend operation.</param>
    /// <returns>The created frame.</returns>
    PngFrame CreateFrameWithData(int width, int height, int xOffset, int yOffset, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation);

    /// <summary>Creates an APNG image from a static or default image and a bounded frame collection.</summary>
    /// <param name="image">The static image or default-frame pixels and metadata.</param>
    /// <param name="playCount">The animation play count; zero repeats indefinitely.</param>
    /// <param name="defaultImageIsFirstFrame">Whether the image pixels are also the first animation frame.</param>
    /// <param name="frames">The animation frames in display order.</param>
    /// <returns>The created animation image.</returns>
    PngImage CreateAnimationImage(PngImage image, int playCount, bool defaultImageIsFirstFrame, ReadOnlySpan<PngFrame> frames);
}
