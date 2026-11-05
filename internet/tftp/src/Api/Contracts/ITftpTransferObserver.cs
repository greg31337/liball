namespace tftp;

/// <summary>
/// Receives immutable-by-convention snapshots of transfer activity.
/// </summary>
public interface ITftpTransferObserver
{
    /// <summary>
    /// Observes a transfer event.
    /// </summary>
    /// <param name="transferEvent">The event snapshot to observe.</param>
    void Observe(TftpTransferEvent transferEvent);
}
