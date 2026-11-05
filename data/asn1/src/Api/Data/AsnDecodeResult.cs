namespace asn1;

/// <summary>
/// Contains the state and value of a decode operation.
/// </summary>
public class AsnDecodeResult
{
    /// <summary>
    /// Contains the decode status.
    /// </summary>
    public AsnDecodeStatus Status;

    /// <summary>
    /// Contains the decoded value when decoding completed.
    /// </summary>
    public AsnRuntimeValue? Value;

    /// <summary>
    /// Contains the exact number of encoded bytes consumed.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Contains stream-oriented progress when available.
    /// </summary>
    public AsnStreamProgress? StreamProgress;

    /// <summary>
    /// Contains decoding diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
