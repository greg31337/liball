namespace asn1;

/// <summary>
/// Identifies the state of a one-shot or incremental decode operation.
/// </summary>
public enum AsnDecodeStatus
{
    /// <summary>
    /// Represents need more data.
    /// </summary>
    NeedMoreData = 0,

    /// <summary>
    /// Represents done.
    /// </summary>
    Done = 1,

    /// <summary>
    /// Represents error.
    /// </summary>
    Error = 2
}
