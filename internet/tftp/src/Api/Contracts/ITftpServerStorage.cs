using System.Threading;

namespace tftp;

/// <summary>
/// Opens server content and completes pending writes using result-style expected failures.
/// </summary>
public interface ITftpServerStorage
{
    /// <summary>
    /// Opens content for a read request.
    /// </summary>
    /// <param name="request">The accepted read request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage open result.</returns>
    TftpStorageOpenResult OpenRead(
        TftpServerRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens a pending destination for a write request.
    /// </summary>
    /// <param name="request">The accepted write request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage open result.</returns>
    TftpStorageOpenResult OpenPendingWrite(
        TftpServerRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically publishes a completed pending write.
    /// </summary>
    /// <param name="completionToken">The opaque token returned when the pending write was opened.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage commit result.</returns>
    TftpStorageCommitResult CommitWrite(
        object completionToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Abandons a pending write and removes its unpublished content.
    /// </summary>
    /// <param name="completionToken">The opaque token returned when the pending write was opened.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The storage abort result.</returns>
    TftpStorageCommitResult AbortWrite(
        object completionToken,
        CancellationToken cancellationToken);
}
