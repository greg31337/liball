namespace ftp;

/// <summary>
/// Receives transfer lifecycle and progress snapshots.
/// </summary>
public interface IFtpTransferObserver
{
    /// <summary>
    /// Observes the initial transfer snapshot.
    /// </summary>
    /// <param name="progress">The initial transfer progress.</param>
    void ObserveTransferStarted(FtpTransferProgress progress);

    /// <summary>
    /// Observes an intermediate transfer snapshot.
    /// </summary>
    /// <param name="progress">The current transfer progress.</param>
    void ObserveTransferProgress(FtpTransferProgress progress);

    /// <summary>
    /// Observes the terminal transfer result.
    /// </summary>
    /// <param name="result">The completed transfer result.</param>
    void ObserveTransferCompleted(FtpTransferResult result);
}
