namespace barcode;

/// <summary>
/// Holds checksum, error-correction, identifier, codeword, confidence, and GS1 decode metadata.
/// </summary>
public class BarcodeDecodeMetadata
{
    /// <summary>
    /// Indicates whether the decoded format carried a checksum.
    /// </summary>
    public bool HasChecksum;

    /// <summary>
    /// Indicates whether the carried checksum was valid.
    /// </summary>
    public bool ChecksumValid;

    /// <summary>
    /// The observed or calculated checksum value.
    /// </summary>
    public int ChecksumValue;

    /// <summary>
    /// Indicates whether error correction was applied.
    /// </summary>
    public bool ErrorCorrectionApplied;

    /// <summary>
    /// The number of corrected errors.
    /// </summary>
    public int CorrectedErrorCount;

    /// <summary>
    /// The number of supplied or inferred erasures.
    /// </summary>
    public int ErasureCount;

    /// <summary>
    /// The ISO/IEC 15424 symbology identifier.
    /// </summary>
    public string? SymbologyIdentifier;

    /// <summary>
    /// The recovered format-specific codewords when available.
    /// </summary>
    public int[]? Codewords;

    /// <summary>
    /// The heuristic decode confidence from zero through one.
    /// </summary>
    public double Confidence;

    /// <summary>
    /// Indicates whether the decoded payload uses GS1 semantics.
    /// </summary>
    public bool IsGs1;

    /// <summary>
    /// The normalized GS1 element string when semantic parsing was requested.
    /// </summary>
    public string? Gs1ElementString;

    /// <summary>
    /// The recovered structured-append metadata when present.
    /// </summary>
    public StructuredAppendInfo? StructuredAppend;
}
