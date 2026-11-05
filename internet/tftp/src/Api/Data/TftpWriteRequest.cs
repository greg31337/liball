namespace tftp;

/// <summary>
/// Holds the endpoint, filename, settings, and optional size for a TFTP write request.
/// </summary>
public class TftpWriteRequest
{
    /// <summary>
    /// The server endpoint to which to write.
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

    /// <summary>
    /// Indicates whether <see cref="KnownSourceSize"/> contains an explicit source size.
    /// </summary>
    public bool HasKnownSourceSize = false;

    /// <summary>
    /// The known source size in bytes when <see cref="HasKnownSourceSize"/> is true.
    /// </summary>
    public long KnownSourceSize = 0;
}
