namespace fix;

/// <summary>Contains the explicit outcome of a message encode operation.</summary>
public class EncodeResult
{
    /// <summary>The operation status.</summary>
    public EncodeStatus Status = EncodeStatus.Invalid;
    /// <summary>The number of destination bytes written.</summary>
    public int BytesWritten;
    /// <summary>The destination size required for the complete encoded message.</summary>
    public int RequiredSize;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
