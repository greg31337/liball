namespace tftp;

/// <summary>
/// Holds the endpoint, filename, and settings for a TFTP read request.
/// </summary>
public class TftpReadRequest
{
    /// <summary>
    /// The server endpoint from which to read.
    /// </summary>
    public TftpEndpoint Endpoint = new TftpEndpoint();

    /// <summary>
    /// The remote filename to request.
    /// </summary>
    public string RemoteFilename = "";

    /// <summary>
    /// The transfer settings to use.
    /// </summary>
    public TftpTransferOptions Options = new TftpTransferOptions();
}
