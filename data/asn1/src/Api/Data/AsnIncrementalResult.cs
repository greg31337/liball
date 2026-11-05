namespace asn1;

/// <summary>
/// Contains the state, progress, and optional value produced from one incremental chunk.
/// </summary>
public class AsnIncrementalResult
{
    /// <summary>
    /// Contains the incremental decode status.
    /// </summary>
    public AsnDecodeStatus Status;

    /// <summary>
    /// Contains the number of bytes consumed from the supplied chunk.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Contains the decoded value when the status is Done.
    /// </summary>
    public AsnRuntimeValue? Value;

    /// <summary>
    /// Contains incremental decoding diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
