namespace asn1;

/// <summary>
/// Contains encoded bytes, progress, and diagnostics from one encode operation.
/// </summary>
public class AsnEncodeResult
{
    /// <summary>
    /// Indicates whether encoding completed successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains the encoded bytes written by the operation.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the exact number of encoded bytes written.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Contains stream-oriented progress when available.
    /// </summary>
    public AsnStreamProgress? StreamProgress;

    /// <summary>
    /// Contains encoding diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
