using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds listener, negotiation, resource, timeout, and stop settings for a TFTP server.
/// </summary>
public class TftpServerOptions
{
    /// <summary>
    /// The local endpoints on which the server listens.
    /// </summary>
    public List<TftpServerBinding> Bindings = [];

    /// <summary>
    /// Indicates whether block-size negotiation is enabled.
    /// </summary>
    public bool EnableBlockSize = true;

    /// <summary>
    /// The largest block size the server will negotiate.
    /// </summary>
    public int MaximumBlockSize = TftpProtocolValues.MaximumBlockSize;

    /// <summary>
    /// Indicates whether timeout negotiation is enabled.
    /// </summary>
    public bool EnableTimeout = true;

    /// <summary>
    /// Indicates whether transfer-size negotiation is enabled.
    /// </summary>
    public bool EnableTransferSize = true;

    /// <summary>
    /// The maximum number of concurrent transfer sessions.
    /// </summary>
    public int MaximumSessions = 64;

    /// <summary>
    /// The maximum accepted filename length in bytes.
    /// </summary>
    public int MaximumFilenameLength = 255;

    /// <summary>
    /// The maximum accepted request packet length in bytes.
    /// </summary>
    public int MaximumRequestPacketLength = TftpProtocolValues.MaximumRequestPacketLength;

    /// <summary>
    /// The maximum content size accepted for one transfer.
    /// </summary>
    public long MaximumTransferSize = long.MaxValue;

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
    /// The maximum total duration of one session in seconds.
    /// </summary>
    public int MaximumSessionDurationSeconds = 300;

    /// <summary>
    /// The final-ACK dally duration in seconds.
    /// </summary>
    public int DallyDurationSeconds = 2;

    /// <summary>
    /// The stop behavior used when server execution is cancelled.
    /// </summary>
    public TftpServerStopMode StopMode = TftpServerStopMode.Graceful;

    /// <summary>
    /// Optional settings for the built-in directory-backed storage provider.
    /// </summary>
    public TftpDirectoryStorageOptions? DirectoryStorage = null;
}
