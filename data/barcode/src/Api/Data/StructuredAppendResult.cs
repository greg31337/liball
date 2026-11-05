namespace barcode;

/// <summary>
/// Holds complete or partial structured-append assembly output and consistency details.
/// </summary>
public class StructuredAppendResult
{
    /// <summary>
    /// Indicates whether every required segment was assembled without conflict.
    /// </summary>
    public bool Complete;

    /// <summary>
    /// The assembled payload when assembly produced output.
    /// </summary>
    public BarcodePayload? Payload;

    /// <summary>
    /// The normalized segments in sequence order.
    /// </summary>
    public BarcodeDecodeResult[]? Segments;

    /// <summary>
    /// The zero-based indices of missing segments.
    /// </summary>
    public int[]? MissingIndices;

    /// <summary>
    /// The stable descriptions of duplicate or conflicting segment metadata.
    /// </summary>
    public string[]? Conflicts;

    /// <summary>
    /// The assembled file or sequence identifier.
    /// </summary>
    public string? FileIdentifier;

    /// <summary>
    /// The issues encountered while assembling the sequence.
    /// </summary>
    public BarcodeValidationIssue[]? Issues;
}
