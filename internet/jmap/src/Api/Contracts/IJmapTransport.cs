using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines transport operations for JMAP Session, API, binary, and EventSource endpoints.
/// </summary>
public interface IJmapTransport
{
    /// <summary>Retrieves and decodes a Session resource.</summary>
    /// <param name="sessionUrl">The absolute Session resource URL.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Session resource.</returns>
    Task<JmapSession> GetTransportSessionAsync(string sessionUrl, CancellationToken cancellationToken);

    /// <summary>Sends a JMAP API request and decodes its response.</summary>
    /// <param name="apiUrl">The absolute API endpoint URL.</param>
    /// <param name="request">The ordered JMAP request.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the complete JMAP response.</returns>
    Task<JmapResponse> SendTransportApiRequestAsync(string apiUrl, JmapRequest request, CancellationToken cancellationToken);

    /// <summary>Uploads a raw binary stream.</summary>
    /// <param name="uploadUrl">The expanded absolute upload URL.</param>
    /// <param name="request">The upload stream and metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the uploaded blob metadata.</returns>
    Task<BlobUploadResponse> SendTransportUploadAsync(string uploadUrl, BlobUploadRequest request, CancellationToken cancellationToken);

    /// <summary>Downloads a raw binary stream.</summary>
    /// <param name="downloadUrl">The expanded absolute download URL.</param>
    /// <param name="request">The requested blob and response metadata.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the streaming download response.</returns>
    Task<BlobDownloadResponse> SendTransportDownloadAsync(string downloadUrl, BlobDownloadRequest request, CancellationToken cancellationToken);

    /// <summary>Opens an authenticated EventSource response stream.</summary>
    /// <param name="eventSourceUrl">The expanded absolute EventSource URL.</param>
    /// <param name="lastEventId">The last received event id, or null for a fresh stream.</param>
    /// <param name="cancellationToken">A token that can cancel connection establishment.</param>
    /// <returns>A task containing the readable event stream.</returns>
    Task<Stream> OpenTransportEventSourceAsync(string eventSourceUrl, string? lastEventId, CancellationToken cancellationToken);
}
