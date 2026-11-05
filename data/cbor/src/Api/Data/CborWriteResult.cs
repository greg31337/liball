namespace cbor;

/// <summary>
/// Holds the outcome of writing CBOR data.
/// </summary>
public class CborWriteResult
{
    /// <summary>The write outcome.</summary>
    public CborWriteStatus Status = CborWriteStatus.None;
    /// <summary>The number of destination bytes written.</summary>
    public int Written;
    /// <summary>The total destination size required by the operation.</summary>
    public int Required;
    /// <summary>The stable failure code, or null when no failure occurred.</summary>
    public string? ErrorCode;
    /// <summary>The failure message, or null when no failure occurred.</summary>
    public string? ErrorMessage;
    /// <summary>The path to the value that failed, or null when it is unavailable.</summary>
    public string? ErrorPath;
}
