namespace avif;

/// <summary>
/// Describes one deterministic finding produced while inspecting, validating, decoding, rendering,
/// encoding, or rewriting AVIF data.
/// </summary>
/// <remarks>
/// Context fields use the documented unknown sentinel when that context is unavailable. Empty paths
/// and references likewise mean that no value is available. Byte offsets are relative to the start
/// of the complete input source.
/// </remarks>
public class AvifDiagnostic
{
    /// <summary>The sentinel used when no byte offset is available.</summary>
    public const long UnknownByteOffset = -1;

    /// <summary>The sentinel used when no bit offset within the located byte is available.</summary>
    public const int UnknownBitOffset = -1;

    /// <summary>The sentinel used when no item or track identifier is available.</summary>
    public const uint UnknownEntityId = 0;

    /// <summary>The sentinel used when no frame or tile index is available.</summary>
    public const int UnknownIndex = -1;

    /// <summary>The stable result code associated with this finding.</summary>
    public AvifResultCode Code;

    /// <summary>The effect of this finding on the requested operation.</summary>
    public AvifDiagnosticSeverity Severity;

    /// <summary>The subsystem that emitted this finding.</summary>
    public AvifDiagnosticCategory Category;

    /// <summary>A caller-readable description that does not replace the stable result code.</summary>
    public string Message;

    /// <summary>The zero-based source byte offset, or <c>UnknownByteOffset</c>.</summary>
    public long ByteOffset;

    /// <summary>The zero-based bit position within <c>ByteOffset</c>, or <c>UnknownBitOffset</c>.</summary>
    public int BitOffset;

    /// <summary>The slash-separated ISO box path, or an empty string.</summary>
    public string BoxPath;

    /// <summary>The HEIF item identifier, or <c>UnknownEntityId</c>.</summary>
    public uint ItemId;

    /// <summary>The ISO media track identifier, or <c>UnknownEntityId</c>.</summary>
    public uint TrackId;

    /// <summary>The zero-based frame index, or <c>UnknownIndex</c>.</summary>
    public int FrameIndex;

    /// <summary>The zero-based tile index, or <c>UnknownIndex</c>.</summary>
    public int TileIndex;

    /// <summary>The AV1 syntax element path, or an empty string.</summary>
    public string Av1SyntaxPath;

    /// <summary>The standards clause associated with this finding, or an empty string.</summary>
    public string StandardReference;

    /// <summary>
    /// Creates a diagnostic without source-location or entity context.
    /// </summary>
    /// <param name="code">The stable result code associated with the finding.</param>
    /// <param name="severity">The effect of the finding on the requested operation.</param>
    /// <param name="category">The subsystem that emitted the finding.</param>
    /// <param name="message">The caller-readable description. A null value becomes an empty string.</param>
    /// <returns>The initialized diagnostic.</returns>
    internal static AvifDiagnostic Create(
        AvifResultCode code,
        AvifDiagnosticSeverity severity,
        AvifDiagnosticCategory category,
        string message)
    {
        return new AvifDiagnostic(
            code,
            severity,
            category,
            message,
            UnknownByteOffset,
            UnknownBitOffset,
            string.Empty,
            UnknownEntityId,
            UnknownEntityId,
            UnknownIndex,
            UnknownIndex,
            string.Empty,
            string.Empty);
    }

    /// <summary>
    /// Creates a diagnostic with complete source-location and entity context.
    /// </summary>
    /// <param name="code">The stable result code associated with the finding.</param>
    /// <param name="severity">The effect of the finding on the requested operation.</param>
    /// <param name="category">The subsystem that emitted the finding.</param>
    /// <param name="message">The caller-readable description. A null value becomes an empty string.</param>
    /// <param name="byteOffset">The source byte offset, or <c>UnknownByteOffset</c>.</param>
    /// <param name="bitOffset">The bit position within the byte, or <c>UnknownBitOffset</c>.</param>
    /// <param name="boxPath">The ISO box path. A null value becomes an empty string.</param>
    /// <param name="itemId">The item identifier, or <c>UnknownEntityId</c>.</param>
    /// <param name="trackId">The track identifier, or <c>UnknownEntityId</c>.</param>
    /// <param name="frameIndex">The frame index, or <c>UnknownIndex</c>.</param>
    /// <param name="tileIndex">The tile index, or <c>UnknownIndex</c>.</param>
    /// <param name="av1SyntaxPath">The AV1 syntax path. A null value becomes an empty string.</param>
    /// <param name="standardReference">The standards clause. A null value becomes an empty string.</param>
    public AvifDiagnostic(
        AvifResultCode code,
        AvifDiagnosticSeverity severity,
        AvifDiagnosticCategory category,
        string? message,
        long byteOffset,
        int bitOffset,
        string? boxPath,
        uint itemId,
        uint trackId,
        int frameIndex,
        int tileIndex,
        string? av1SyntaxPath,
        string? standardReference)
    {
        Code = code;
        Severity = severity;
        Category = category;
        Message = message == null ? string.Empty : message;
        ByteOffset = byteOffset;
        BitOffset = bitOffset;
        BoxPath = boxPath == null ? string.Empty : boxPath;
        ItemId = itemId;
        TrackId = trackId;
        FrameIndex = frameIndex;
        TileIndex = tileIndex;
        Av1SyntaxPath = av1SyntaxPath == null ? string.Empty : av1SyntaxPath;
        StandardReference = standardReference == null ? string.Empty : standardReference;
    }

    /// <summary>Creates a diagnostic with complete source and entity context.</summary>
    /// <returns>The initialized diagnostic.</returns>
    internal static AvifDiagnostic Create(
        AvifResultCode code,
        AvifDiagnosticSeverity severity,
        AvifDiagnosticCategory category,
        string? message,
        long byteOffset,
        int bitOffset,
        string? boxPath,
        uint itemId,
        uint trackId,
        int frameIndex,
        int tileIndex,
        string? av1SyntaxPath,
        string? standardReference)
    {
        return new AvifDiagnostic(
            code,
            severity,
            category,
            message,
            byteOffset,
            bitOffset,
            boxPath,
            itemId,
            trackId,
            frameIndex,
            tileIndex,
            av1SyntaxPath,
            standardReference);
    }
}
