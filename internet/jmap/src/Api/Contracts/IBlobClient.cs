using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines host-neutral streaming blob operations.
/// </summary>
public interface IBlobClient
{
    /// <summary>Uploads raw binary data to a JMAP account.</summary>
    /// <param name="request">The upload stream and metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the uploaded blob metadata.</returns>
    Task<BlobUploadResponse> UploadBlobAsync(BlobUploadRequest request, CancellationToken cancellationToken);

    /// <summary>Downloads raw binary data from a JMAP account.</summary>
    /// <param name="request">The requested blob and response metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the streaming download response.</returns>
    Task<BlobDownloadResponse> DownloadBlobAsync(BlobDownloadRequest request, CancellationToken cancellationToken);

    /// <summary>Copies binary data between JMAP accounts.</summary>
    /// <param name="request">The Blob/copy arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Blob/copy result.</returns>
    Task<BlobCopyResponse> CopyBlobAsync(BlobCopyRequest request, CancellationToken cancellationToken);
}
