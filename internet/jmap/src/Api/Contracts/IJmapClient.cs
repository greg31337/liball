using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines access to JMAP client configuration and its cached Session resource.
/// </summary>
public interface IJmapClient
{
    /// <summary>Gets the configuration used by this client.</summary>
    /// <returns>The client configuration.</returns>
    JmapClientOptions GetClientOptions();

    /// <summary>Gets the currently cached Session resource, if one has been loaded.</summary>
    /// <returns>The cached Session, or null when the Session has not been loaded.</returns>
    JmapSession? GetSession();

    /// <summary>Discovers and retrieves the JMAP Session resource.</summary>
    /// <param name="options">The direct or username-domain discovery options.</param>
    /// <param name="cancellationToken">A token that can cancel discovery.</param>
    /// <returns>A task containing the discovered Session.</returns>
    Task<JmapSession> DiscoverSessionAsync(JmapDiscoveryOptions options, CancellationToken cancellationToken);

    /// <summary>Refreshes the currently configured Session resource.</summary>
    /// <param name="cancellationToken">A token that can cancel the refresh.</param>
    /// <returns>A task containing the refreshed Session.</returns>
    Task<JmapSession> RefreshSessionAsync(CancellationToken cancellationToken);

    /// <summary>Gets the primary account for a capability.</summary>
    /// <param name="capability">The account capability URI.</param>
    /// <returns>The primary account id, or null when no matching account is available.</returns>
    string? GetPrimaryAccountId(string capability);

    /// <summary>Executes a low-level batch request.</summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the complete JMAP response.</returns>
    Task<JmapResponse> ExecuteBatchAsync(JmapRequest request, CancellationToken cancellationToken);

    /// <summary>Executes one arbitrary standard or vendor method call.</summary>
    /// <param name="call">The raw method call to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the selected call result.</returns>
    Task<JmapCallResult> ExecuteRawCallAsync(JmapRawMethodCall call, CancellationToken cancellationToken);

    /// <summary>Echoes an arbitrary JSON object through Core/echo.</summary>
    /// <param name="arguments">The arbitrary JSON arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the echoed JSON object.</returns>
    Task<JsonElement> ExecuteEchoAsync(JsonElement arguments, CancellationToken cancellationToken);

    /// <summary>Finds the first response for a call identifier in a batch response.</summary>
    /// <param name="response">The batch response to search.</param>
    /// <param name="callId">The originating call identifier.</param>
    /// <returns>The selected raw and classified call result.</returns>
    JmapCallResult FindCallResult(JmapResponse response, string callId);
}
