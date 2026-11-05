namespace fix;

/// <summary>Contains the explicit outcome of one transport read.</summary>
public class TransportReadResult
{
    /// <summary>The transport state after reading.</summary>
    public TransportState State;
    /// <summary>The number of destination bytes written.</summary>
    public int BytesRead;
    /// <summary>Whether the peer ended the stream.</summary>
    public bool EndOfStream;
    /// <summary>Whether the operation would block.</summary>
    public bool WouldBlock;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
