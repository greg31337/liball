using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP sending Identities.
/// </summary>
public interface IIdentityClient
{
    /// <summary>Gets sending Identities.</summary>
    /// <param name="request">The Identity/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/get result.</returns>
    Task<IdentityGetResponse> GetIdentitiesAsync(IdentityGetRequest request, CancellationToken cancellationToken);

    /// <summary>Gets Identity changes since a previous collection state.</summary>
    /// <param name="request">The Identity/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/changes result.</returns>
    Task<IdentityChangesResponse> GetIdentityChangesAsync(IdentityChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Gets and combines every Identity changes page needed to reach the current state.</summary>
    /// <param name="request">The initial Identity/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing one deduplicated aggregate changes result.</returns>
    Task<IdentityChangesResponse> GetAllIdentityChangesAsync(IdentityChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Creates, updates, or destroys sending Identities.</summary>
    /// <param name="request">The Identity/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Identity/set result.</returns>
    Task<IdentitySetResponse> SetIdentitiesAsync(IdentitySetRequest request, CancellationToken cancellationToken);
}
