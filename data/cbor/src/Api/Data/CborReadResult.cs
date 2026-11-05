namespace cbor;

/// <summary>
/// Holds the outcome of reading a CBOR value.
/// </summary>
public class CborReadResult
{
    /// <summary>The read outcome.</summary>
    public CborReadStatus Status = CborReadStatus.None;
    /// <summary>The value produced by a successful read.</summary>
    public CborValue? Value;
    /// <summary>The number of source bytes consumed.</summary>
    public int Consumed;
    /// <summary>The number of source bytes remaining after a completed first-item read.</summary>
    public int Remaining;
    /// <summary>The zero-based item index when the result belongs to a CBOR Sequence, or negative one otherwise.</summary>
    public int ItemIndex = -1;
    /// <summary>The absolute source offset where the item begins, or negative one when unavailable.</summary>
    public long SourceOffset = -1;
    /// <summary>The stable failure code, or null when no failure occurred.</summary>
    public string? ErrorCode;
    /// <summary>The failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;
    /// <summary>The absolute failure offset, or negative one when it is unavailable.</summary>
    public long ErrorOffset = -1;
    /// <summary>The path to the failure, or null when it is unavailable.</summary>
    public string? ErrorPath;
}
