using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines host-neutral processing for every JMAP HTTP endpoint.
/// </summary>
public interface IJmapServer
{
    /// <summary>Gets the active server options.</summary>
    /// <returns>The server options.</returns>
    JmapServerOptions GetServerOptions();

    /// <summary>Gets the caller-supplied backend service bundle.</summary>
    /// <returns>The backend service bundle.</returns>
    JmapServerServices GetServerServices();

    /// <summary>Processes a Session resource request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    Task<JmapEndpointResponse> ProcessSessionEndpointAsync(JmapEndpointRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a JMAP API request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    Task<JmapEndpointResponse> ProcessApiEndpointAsync(JmapEndpointRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a streaming binary upload request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    Task<JmapEndpointResponse> ProcessUploadEndpointAsync(JmapEndpointRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a streaming binary download request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral endpoint response.</returns>
    Task<JmapEndpointResponse> ProcessDownloadEndpointAsync(JmapEndpointRequest request, CancellationToken cancellationToken);

    /// <summary>Processes a long-running EventSource request.</summary>
    /// <param name="request">The host-neutral endpoint request.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the host-neutral streaming response.</returns>
    Task<JmapEndpointResponse> ProcessEventSourceEndpointAsync(JmapEndpointRequest request, CancellationToken cancellationToken);
}
