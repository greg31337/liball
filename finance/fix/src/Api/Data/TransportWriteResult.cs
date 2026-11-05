namespace fix;

/// <summary>Contains the explicit outcome of one transport state change or write.</summary>
public class TransportWriteResult
{
    /// <summary>The transport state after the operation.</summary>
    public TransportState State;
    /// <summary>The number of source bytes consumed.</summary>
    public int BytesWritten;
    /// <summary>Whether the operation would block.</summary>
    public bool WouldBlock;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
