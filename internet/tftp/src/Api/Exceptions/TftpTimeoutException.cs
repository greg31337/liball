namespace tftp;

/// <summary>
/// Represents exhaustion of a TFTP retry or total-duration limit.
/// </summary>
public class TftpTimeoutException : System.Exception
{
    /// <summary>
    /// Indicates that retry exhaustion, rather than total-duration exhaustion, ended the transfer.
    /// </summary>
    public bool RetryLimitReached;

    /// <summary>
    /// The number of retransmissions performed before the timeout failure.
    /// </summary>
    public int RetransmissionCount;

    /// <summary>
    /// The elapsed transfer duration in milliseconds before the timeout failure.
    /// </summary>
    public long ElapsedMilliseconds;

    /// <summary>
    /// Initializes a timeout exception with limit and transfer metrics.
    /// </summary>
    /// <param name="message">The timeout failure message.</param>
    /// <param name="retryLimitReached">Whether retry exhaustion ended the transfer.</param>
    /// <param name="retransmissionCount">The number of retransmissions performed.</param>
    /// <param name="elapsedMilliseconds">The elapsed transfer duration in milliseconds.</param>
    public TftpTimeoutException(
        string message,
        bool retryLimitReached,
        int retransmissionCount,
        long elapsedMilliseconds)
        : base(message)
    {
        RetryLimitReached = retryLimitReached;
        RetransmissionCount = retransmissionCount;
        ElapsedMilliseconds = elapsedMilliseconds;
    }
}
