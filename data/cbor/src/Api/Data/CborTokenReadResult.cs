namespace cbor;

/// <summary>
/// Holds the outcome of reading a low-level CBOR token.
/// </summary>
public class CborTokenReadResult
{
    /// <summary>The read outcome.</summary>
    public CborReadStatus Status = CborReadStatus.None;
    /// <summary>The token produced by a successful read.</summary>
    public CborToken? Token;
    /// <summary>The number of source bytes consumed.</summary>
    public int Consumed;
    /// <summary>The number of supplied source bytes not consumed by the token.</summary>
    public int Remaining;
    /// <summary>The stable failure code, or null when no failure occurred.</summary>
    public string? ErrorCode;
    /// <summary>The failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;
    /// <summary>The absolute failure offset, or negative one when it is unavailable.</summary>
    public long ErrorOffset = -1;
    /// <summary>The path to the failure, or null when it is unavailable.</summary>
    public string? ErrorPath;
}
