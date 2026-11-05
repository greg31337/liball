using System.IO;

namespace png;

/// <summary>
/// Provides the main entry points for identifying, decoding, encoding, validating, repairing, and creating PNG images.
/// </summary>
internal static class Png
{
    static readonly PngCodec codec = new PngCodec();

    /// <summary>
    /// Reads PNG structure and metadata from a byte array without inflating image data.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The identification result.</returns>
    public static PngIdentifyResult IdentifyBytes(byte[] data)
    {
        return PngIdentifier.Identify(data);
    }

    /// <summary>
    /// Reads PNG structure and metadata from a byte array without inflating image data.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The identification result.</returns>
    public static PngIdentifyResult IdentifyBytes(byte[] data, PngDecoderOptions options)
    {
        return PngIdentifier.Identify(data, 0, options);
    }

    /// <summary>
    /// Reads PNG structure and metadata from a byte array without inflating image data.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The identification result.</returns>
    public static PngIdentifyResult IdentifyBytes(byte[] data, int offset, PngDecoderOptions options)
    {
        return PngIdentifier.Identify(data, offset, options);
    }

    /// <summary>
    /// Reads PNG structure and metadata from the current stream position without inflating image data.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The identification result.</returns>
    public static PngIdentifyResult IdentifyStream(Stream stream)
    {
        return PngIdentifier.Identify(stream);
    }

    /// <summary>
    /// Reads PNG structure and metadata from the current stream position without inflating image data.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The identification result.</returns>
    public static PngIdentifyResult IdentifyStream(Stream stream, PngDecoderOptions options)
    {
        return PngIdentifier.Identify(stream, options);
    }

    /// <summary>
    /// Validates and decompresses a PNG byte array into pixels, metadata, and optional animation frames.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeBytes(byte[] data)
    {
        return codec.DecodeBytesWithDefaultOptions(data);
    }

    /// <summary>
    /// Validates and decompresses a PNG byte array into pixels, metadata, and optional animation frames.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeBytes(byte[] data, PngDecoderOptions options)
    {
        return codec.DecodeBytes(data, options);
    }

    /// <summary>
    /// Validates and decompresses a PNG byte array into pixels, metadata, and optional animation frames.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeBytes(byte[] data, int offset, PngDecoderOptions options)
    {
        return codec.DecodeBytesAtOffset(data, offset, options);
    }

    /// <summary>
    /// Validates and decompresses PNG data beginning at an offset in a byte array.
    /// </summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based offset of the PNG signature.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeBytesAtOffset(byte[] data, int offset, PngDecoderOptions options)
    {
        return codec.DecodeBytesAtOffset(data, offset, options);
    }

    /// <summary>
    /// Validates and decompresses a PNG datastream bounded by a read-only byte span.
    /// </summary>
    /// <param name="data">The bounded PNG datastream.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeSpan(ReadOnlySpan<byte> data, PngDecoderOptions options)
    {
        return codec.DecodeSpan(data, options);
    }

    /// <summary>
    /// Validates and decompresses PNG data from the current stream position.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeStream(Stream stream)
    {
        return codec.DecodeStreamWithDefaultOptions(stream);
    }

    /// <summary>
    /// Validates and decompresses PNG data from the current stream position.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage DecodeStream(Stream stream, PngDecoderOptions options)
    {
        return codec.DecodeStream(stream, options);
    }

    /// <summary>
    /// Encodes an image as a complete PNG or APNG datastream in a new byte array.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] EncodeToBytes(PngImage image)
    {
        return codec.EncodeToBytesWithDefaultOptions(image);
    }

    /// <summary>
    /// Encodes an image as a complete PNG or APNG datastream in a new byte array.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
    {
        return codec.EncodeToBytes(image, options);
    }

    /// <summary>
    /// Encodes an image as PNG or APNG and writes it to the supplied stream.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="image">The image to process.</param>
    public static void EncodeToStream(Stream stream, PngImage image)
    {
        codec.EncodeToStreamWithDefaultOptions(stream, image);
    }

    /// <summary>
    /// Encodes an image as PNG or APNG and writes it to the supplied stream.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    public static void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
    {
        codec.EncodeToStream(stream, image, options);
    }

    /// <summary>
    /// Checks a PNG byte array using strict validation unless the options select another error policy.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateBytes(byte[] data)
    {
        return PngValidator.Validate(data);
    }

    /// <summary>
    /// Checks a PNG byte array using strict validation unless the options select another error policy.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateBytes(byte[] data, PngDecoderOptions options)
    {
        return PngValidator.Validate(data, 0, options);
    }

    /// <summary>
    /// Checks a PNG byte array using strict validation unless the options select another error policy.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateBytes(byte[] data, int offset, PngDecoderOptions options)
    {
        return PngValidator.Validate(data, offset, options);
    }

    /// <summary>
    /// Checks PNG data from the current stream position using strict validation unless configured otherwise.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateStream(Stream stream)
    {
        return PngValidator.Validate(stream);
    }

    /// <summary>
    /// Checks PNG data from the current stream position using strict validation unless configured otherwise.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateStream(Stream stream, PngDecoderOptions options)
    {
        return PngValidator.Validate(stream, options);
    }

    /// <summary>
    /// Inspects as much PNG structure as possible and reports problems instead of requiring a valid image.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult InspectBestEffortBytes(byte[] data)
    {
        return PngValidator.InspectBestEffort(data);
    }

    /// <summary>
    /// Inspects as much PNG structure as possible and reports problems instead of requiring a valid image.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult InspectBestEffortBytes(byte[] data, int offset)
    {
        return PngValidator.InspectBestEffort(data, offset);
    }

    /// <summary>
    /// Inspects as much PNG structure as possible from the current stream position.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult InspectBestEffortStream(Stream stream)
    {
        return PngValidator.InspectBestEffort(stream);
    }

    /// <summary>
    /// Validates PNG bytes while tolerating the ancillary issues that the repair workflow can correct.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateTolerantRepairBytes(byte[] data)
    {
        return PngValidator.ValidateTolerantRepair(data);
    }

    /// <summary>
    /// Validates PNG bytes while tolerating the ancillary issues that the repair workflow can correct.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateTolerantRepairBytes(byte[] data, int offset)
    {
        return PngValidator.ValidateTolerantRepair(data, offset);
    }

    /// <summary>
    /// Validates streamed PNG data while tolerating repairable ancillary issues.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The validation result.</returns>
    public static PngValidationResult ValidateTolerantRepairStream(Stream stream)
    {
        return PngValidator.ValidateTolerantRepair(stream);
    }

    /// <summary>
    /// Reads one length, type, payload, and CRC-framed PNG chunk from a bounded span using default limits.
    /// </summary>
    /// <param name="data">The bounded raw chunk bytes.</param>
    /// <returns>The parsed raw chunk.</returns>
    public static PngChunk ReadChunkSpan(ReadOnlySpan<byte> data)
    {
        return ReadChunkSpanWithLimits(data, new PngSecurityLimits());
    }

    /// <summary>
    /// Reads one length, type, payload, and CRC-framed PNG chunk from a bounded span using explicit limits.
    /// </summary>
    /// <param name="data">The bounded raw chunk bytes.</param>
    /// <param name="limits">The limits used to bound allocations.</param>
    /// <returns>The parsed raw chunk.</returns>
    public static PngChunk ReadChunkSpanWithLimits(ReadOnlySpan<byte> data, PngSecurityLimits limits)
    {
        PngChunk chunk = PngRawChunkReader.ReadSpan(data, 0, limits);
        chunk.PreservationState = PngChunkPreservationState.Preserved;
        return chunk;
    }

    /// <summary>
    /// Writes one framed PNG chunk into a caller-provided span using default limits.
    /// </summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <returns>The number of bytes written.</returns>
    public static int WriteChunkSpan(Span<byte> destination, PngChunk chunk)
    {
        return WriteChunkSpanWithLimits(destination, chunk, new PngSecurityLimits());
    }

    /// <summary>
    /// Writes one framed PNG chunk into a caller-provided span using explicit limits.
    /// </summary>
    /// <param name="destination">The destination, which must hold at least payload length plus 12 bytes.</param>
    /// <param name="chunk">The raw chunk to write.</param>
    /// <param name="limits">The limits used to bound the chunk payload.</param>
    /// <returns>The number of bytes written.</returns>
    public static int WriteChunkSpanWithLimits(Span<byte> destination, PngChunk chunk, PngSecurityLimits limits)
    {
        return PngRawChunkWriter.WriteSpan(destination, chunk, limits);
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(byte[] data)
    {
        return ReadChunks(data, 0, new PngSecurityLimits());
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="limits">The limits used to bound allocations and work.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(byte[] data, PngSecurityLimits limits)
    {
        return ReadChunks(data, 0, limits);
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(byte[] data, int offset)
    {
        return ReadChunks(data, offset, new PngSecurityLimits());
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <param name="limits">The limits used to bound allocations and work.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(byte[] data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        ValidateChunkDatastreamSignature(data, offset);
        return PngChunkListReader.Read(data, offset + PngConstants.SignatureLength, limits);
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(Stream stream)
    {
        return ReadChunks(stream, new PngSecurityLimits());
    }

    /// <summary>
    /// Reads the PNG signature and ordered raw chunks without decoding pixels.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="limits">The limits used to bound allocations and work.</param>
    /// <returns>The parsed chunks and datastream status.</returns>
    public static PngChunkListResult ReadChunks(Stream stream, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        byte[] signature = ReadChunkDatastreamSignature(stream);
        ValidateChunkDatastreamSignature(signature, 0);
        return PngChunkListReader.Read(stream, limits);
    }

    /// <summary>
    /// Writes a PNG signature followed by the supplied ordered chunks, recalculating serialized chunk framing.
    /// </summary>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] WriteChunks(PngChunk[] chunks)
    {
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    /// <summary>
    /// Writes a PNG signature followed by the supplied ordered chunks, recalculating serialized chunk framing.
    /// </summary>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    /// <param name="limits">The limits used to bound allocations and work.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] WriteChunks(PngChunk[] chunks, PngSecurityLimits limits)
    {
        return PngChunkDatastreamWriter.WriteToBytes(chunks, limits);
    }

    /// <summary>
    /// Writes a PNG signature followed by chunks placed according to the supplied policy.
    /// </summary>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and limits used for output.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] WriteChunksWithOptions(PngChunk[] chunks, PngChunkWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        PngChunk[] outputChunks = PngChunkPlacementProcessor.ApplyPolicy(chunks, options.PlacementPolicy);
        return PngChunkDatastreamWriter.WriteToBytes(outputChunks, options.Limits);
    }

    /// <summary>
    /// Writes a PNG signature followed by the supplied ordered chunks, recalculating serialized chunk framing.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    public static void WriteChunks(Stream stream, PngChunk[] chunks)
    {
        PngChunkDatastreamWriter.Write(stream, chunks);
    }

    /// <summary>
    /// Writes a PNG signature followed by the supplied ordered chunks, recalculating serialized chunk framing.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    /// <param name="limits">The limits used to bound allocations and work.</param>
    public static void WriteChunks(Stream stream, PngChunk[] chunks, PngSecurityLimits limits)
    {
        PngChunkDatastreamWriter.Write(stream, chunks, limits);
    }

    /// <summary>
    /// Writes a PNG signature followed by chunks placed according to the supplied policy.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <param name="chunks">The chunks to write.</param>
    /// <param name="options">The placement policy and limits used for output.</param>
    public static void WriteChunksStreamWithOptions(Stream stream, PngChunk[] chunks, PngChunkWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        PngChunk[] outputChunks = PngChunkPlacementProcessor.ApplyPolicy(chunks, options.PlacementPolicy);
        PngChunkDatastreamWriter.Write(stream, outputChunks, options.Limits);
    }

    /// <summary>
    /// Returns PNG bytes with repairable ancillary chunks moved to valid positions.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryOrderingBytes(byte[] data)
    {
        return PngRepairer.RepairAncillaryOrdering(data);
    }

    /// <summary>
    /// Returns PNG bytes with repairable ancillary chunks moved to valid positions.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryOrderingBytes(byte[] data, int offset)
    {
        return PngRepairer.RepairAncillaryOrdering(data, offset);
    }

    /// <summary>
    /// Reads PNG data and returns bytes with repairable ancillary chunks moved to valid positions.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryOrderingStream(Stream stream)
    {
        return PngRepairer.RepairAncillaryOrdering(stream);
    }

    /// <summary>
    /// Returns a reordered chunk array with repairable ancillary chunks in valid positions.
    /// </summary>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    /// <returns>The resulting metadata, model, or calculated value.</returns>
    public static PngChunk[] RepairAncillaryOrderingChunks(PngChunk[] chunks)
    {
        return PngRepairer.RepairAncillaryOrdering(chunks);
    }

    /// <summary>
    /// Returns PNG bytes whose invalid ancillary chunk CRC values have been recalculated.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryCrcBytes(byte[] data)
    {
        return PngRepairer.RepairAncillaryCrc(data);
    }

    /// <summary>
    /// Returns PNG bytes whose invalid ancillary chunk CRC values have been recalculated.
    /// </summary>
    /// <param name="data">The source or destination byte array.</param>
    /// <param name="offset">The zero-based byte offset at which processing begins.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryCrcBytes(byte[] data, int offset)
    {
        return PngRepairer.RepairAncillaryCrc(data, offset);
    }

    /// <summary>
    /// Reads PNG data and returns bytes whose invalid ancillary chunk CRC values have been recalculated.
    /// </summary>
    /// <param name="stream">The readable or writable stream at its current position.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] RepairAncillaryCrcStream(Stream stream)
    {
        return PngRepairer.RepairAncillaryCrc(stream);
    }

    /// <summary>
    /// Returns chunk copies with CRC values recalculated for ancillary chunks.
    /// </summary>
    /// <param name="chunks">The chunks in PNG datastream order.</param>
    /// <returns>The resulting metadata, model, or calculated value.</returns>
    public static PngChunk[] RepairAncillaryCrcChunks(PngChunk[] chunks)
    {
        return PngRepairer.RepairAncillaryCrc(chunks);
    }

    /// <summary>
    /// Creates an image model with dimensions, format, metadata containers, and either blank or supplied pixel bytes.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="pixelFormat">The in-memory pixel format.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage CreateImage(int width, int height, PngPixelFormat pixelFormat)
    {
        return PngImageFactoryCore.CreateBlank(width, height, pixelFormat);
    }

    /// <summary>
    /// Creates an image model with dimensions, format, metadata containers, and either blank or supplied pixel bytes.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="pixelFormat">The in-memory pixel format.</param>
    /// <param name="data">The source or destination byte array.</param>
    /// <returns>The decoded image.</returns>
    public static PngImage CreateImage(int width, int height, PngPixelFormat pixelFormat, byte[] data)
    {
        return PngImageFactoryCore.CreateFromBytes(width, height, pixelFormat, data);
    }

    internal static void ValidateChunkDatastreamSignature(byte[] data, int offset)
    {
        PngValidationResult signatureResult = PngSignature.ValidateSignature(data, offset, PngErrorPolicy.Strict);
        if (!signatureResult.IsValid)
        {
            throw new InvalidDataException("PNG signature is invalid.");
        }
    }

    internal static byte[] ReadChunkDatastreamSignature(Stream stream)
    {
        byte[] signature = new byte[PngConstants.SignatureLength];
        int totalRead = 0;
        while (totalRead < signature.Length)
        {
            int read = stream.Read(signature, totalRead, signature.Length - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return signature;
    }
}
