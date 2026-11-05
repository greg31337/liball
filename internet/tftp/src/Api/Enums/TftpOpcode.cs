namespace tftp;

/// <summary>
/// Identifies a TFTP packet operation.
/// </summary>
public enum TftpOpcode
{
    /// <summary>
    /// Requests that a file be read from the server.
    /// </summary>
    ReadRequest = 1,

    /// <summary>
    /// Requests that a file be written to the server.
    /// </summary>
    WriteRequest = 2,

    /// <summary>
    /// Carries a numbered block of file data.
    /// </summary>
    Data = 3,

    /// <summary>
    /// Acknowledges a numbered data block or a write request.
    /// </summary>
    Acknowledgement = 4,

    /// <summary>
    /// Reports a transfer error.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Acknowledges negotiated transfer options.
    /// </summary>
    OptionAcknowledgement = 6
}
