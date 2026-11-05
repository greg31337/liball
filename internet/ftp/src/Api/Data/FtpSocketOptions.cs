namespace ftp;

/// <summary>
/// Configures portable TCP socket behavior for an FTP control or data connection.
/// </summary>
public class FtpSocketOptions
{
    /// <summary>
    /// Indicates that TCP Nagle buffering is disabled.
    /// </summary>
    public bool NoDelay;

    /// <summary>
    /// Indicates that TCP keep-alive probes are enabled using operating-system timing defaults.
    /// </summary>
    public bool KeepAlive;

    /// <summary>
    /// The requested receive-buffer size in bytes, or zero to retain the operating-system default.
    /// </summary>
    public int ReceiveBufferBytes;

    /// <summary>
    /// The requested send-buffer size in bytes, or zero to retain the operating-system default.
    /// </summary>
    public int SendBufferBytes;

    /// <summary>
    /// The synchronous receive timeout in milliseconds, or zero for the operating-system default.
    /// </summary>
    public int ReceiveTimeoutMilliseconds;

    /// <summary>
    /// The synchronous send timeout in milliseconds, or zero for the operating-system default.
    /// </summary>
    public int SendTimeoutMilliseconds;

    /// <summary>
    /// Indicates that TCP close uses the configured linger duration.
    /// </summary>
    public bool LingerEnabled;

    /// <summary>
    /// The TCP linger duration in seconds, or zero when linger is disabled.
    /// </summary>
    public int LingerSeconds;

    /// <summary>
    /// Creates portable socket settings that retain operating-system defaults.
    /// </summary>
    public FtpSocketOptions()
    {
        NoDelay = false;
        KeepAlive = false;
        ReceiveBufferBytes = 0;
        SendBufferBytes = 0;
        ReceiveTimeoutMilliseconds = 0;
        SendTimeoutMilliseconds = 0;
        LingerEnabled = false;
        LingerSeconds = 0;
    }
}
