using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for the singleton JMAP vacation response.
/// </summary>
public interface IVacationResponseClient
{
    /// <summary>Gets the singleton vacation-response configuration.</summary>
    /// <param name="request">The VacationResponse/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the VacationResponse/get result.</returns>
    Task<VacationResponseGetResponse> GetVacationResponseAsync(VacationResponseGetRequest request, CancellationToken cancellationToken);

    /// <summary>Updates the singleton vacation-response configuration.</summary>
    /// <param name="request">The VacationResponse/set arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the VacationResponse/set result.</returns>
    Task<VacationResponseSetResponse> SetVacationResponseAsync(VacationResponseSetRequest request, CancellationToken cancellationToken);
}
