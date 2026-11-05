using System.IO;
using System.Threading;

namespace tftp;

/// <summary>
/// Downloads and uploads content using TFTP.
/// </summary>
public interface ITftpClient
{
    /// <summary>
    /// Downloads remote content into a caller-owned stream.
    /// </summary>
    /// <param name="request">The read request.</param>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    TftpTransferResult DownloadToStream(
        TftpReadRequest request,
        Stream destination,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads content from a caller-owned stream.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="source">The readable source stream.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    TftpTransferResult UploadFromStream(
        TftpWriteRequest request,
        Stream source,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads remote content and publishes it at a local file path.
    /// </summary>
    /// <param name="request">The read request.</param>
    /// <param name="localPath">The local destination file path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    TftpTransferResult DownloadToFile(
        TftpReadRequest request,
        string localPath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads content from a local file path.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="localPath">The local source file path.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The completed transfer result.</returns>
    TftpTransferResult UploadFromFile(
        TftpWriteRequest request,
        string localPath,
        CancellationToken cancellationToken);
}
