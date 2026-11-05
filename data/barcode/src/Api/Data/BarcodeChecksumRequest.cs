namespace barcode;

/// <summary>
/// Holds a checksum algorithm or barcode format and the source data to process.
/// </summary>
public class BarcodeChecksumRequest
{
    /// <summary>
    /// The barcode format whose checksum rules apply.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The explicit checksum algorithm name when format selection alone is insufficient.
    /// </summary>
    public string? Algorithm;

    /// <summary>
    /// The source text when calculating a character-oriented checksum.
    /// </summary>
    public string? Text;

    /// <summary>
    /// The source bytes when calculating a byte- or codeword-oriented checksum.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// The source codewords when calculating a symbol checksum.
    /// </summary>
    public int[]? Codewords;

    /// <summary>
    /// Indicates whether an observed check value was supplied for verification.
    /// </summary>
    public bool HasObservedValue;

    /// <summary>
    /// The supplied check value when verification is requested.
    /// </summary>
    public int ObservedValue;
}
