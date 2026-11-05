using System.Collections.Generic;

namespace tftp;

/// <summary>
/// Holds an immutable-by-convention snapshot of server lifecycle and transfer counts.
/// </summary>
public class TftpServerStatus
{
    /// <summary>
    /// Indicates whether the server is accepting requests or draining transfers.
    /// </summary>
    public bool IsRunning = false;

    /// <summary>
    /// Indicates whether the server is draining active transfers.
    /// </summary>
    public bool IsDraining = false;

    /// <summary>
    /// The endpoints to which the server is currently bound.
    /// </summary>
    public List<TftpServerBinding> BoundEndpoints = [];

    /// <summary>
    /// The number of currently active transfers.
    /// </summary>
    public int ActiveTransferCount = 0;

    /// <summary>
    /// The cumulative number of accepted transfers.
    /// </summary>
    public long AcceptedTransferCount = 0;

    /// <summary>
    /// The cumulative number of successfully completed transfers.
    /// </summary>
    public long CompletedTransferCount = 0;

    /// <summary>
    /// The cumulative number of failed or cancelled transfers.
    /// </summary>
    public long FailedTransferCount = 0;
}
