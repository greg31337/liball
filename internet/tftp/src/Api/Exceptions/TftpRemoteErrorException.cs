namespace tftp;

/// <summary>
/// Represents an ERROR packet received from a remote TFTP peer.
/// </summary>
public class TftpRemoteErrorException : System.Exception
{
    /// <summary>
    /// The protocol error code reported by the remote peer.
    /// </summary>
    public TftpErrorCode ErrorCode;

    /// <summary>
    /// The error message reported by the remote peer.
    /// </summary>
    public string RemoteMessage;

    /// <summary>
    /// Initializes a remote error exception from an ERROR packet.
    /// </summary>
    /// <param name="errorCode">The protocol error code reported by the peer.</param>
    /// <param name="remoteMessage">The error message reported by the peer.</param>
    public TftpRemoteErrorException(TftpErrorCode errorCode, string remoteMessage)
        : base(remoteMessage)
    {
        ErrorCode = errorCode;
        RemoteMessage = remoteMessage;
    }
}
