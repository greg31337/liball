namespace tftp;

/// <summary>
/// Holds content, negotiation, retry, and size settings for a client transfer.
/// </summary>
public class TftpTransferOptions
{
    /// <summary>
    /// The content representation to transfer.
    /// </summary>
    public TftpTransferMode Mode = TftpTransferMode.Octet;

    /// <summary>
    /// Indicates whether the block-size option should be requested.
    /// </summary>
    public bool RequestBlockSize = false;

    /// <summary>
    /// The requested block size when <see cref="RequestBlockSize"/> is true.
    /// </summary>
    public int BlockSize = TftpProtocolValues.DefaultBlockSize;

    /// <summary>
    /// Indicates whether the timeout option should be requested.
    /// </summary>
    public bool RequestTimeout = false;

    /// <summary>
    /// The requested timeout in seconds when <see cref="RequestTimeout"/> is true.
    /// </summary>
    public int TimeoutSeconds = TftpProtocolValues.MinimumTimeoutSeconds;

    /// <summary>
    /// Indicates whether the transfer-size option should be requested.
    /// </summary>
    public bool RequestTransferSize = false;

    /// <summary>
    /// Indicates whether an option rejection should cause one optionless request retry.
    /// </summary>
    public bool RetryWithoutOptions = false;

    /// <summary>
    /// The initial retransmission interval in seconds.
    /// </summary>
    public int InitialTimeoutSeconds = 1;

    /// <summary>
    /// The maximum retransmission interval after backoff, in seconds.
    /// </summary>
    public int MaximumTimeoutSeconds = 16;

    /// <summary>
    /// The maximum number of retransmissions permitted without forward progress.
    /// </summary>
    public int MaximumRetries = 5;

    /// <summary>
    /// The maximum total transfer duration in seconds.
    /// </summary>
    public int MaximumTransferDurationSeconds = 300;

    /// <summary>
    /// The final-ACK dally duration in seconds.
    /// </summary>
    public int DallyDurationSeconds = 2;

    /// <summary>
    /// The maximum decoded content size accepted by the transfer.
    /// </summary>
    public long MaximumTransferSize = long.MaxValue;
}
