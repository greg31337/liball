using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Supplies authorization values for JMAP HTTP resources without prescribing an authentication scheme.
/// </summary>
public interface IJmapAuthorizationProvider
{
    /// <summary>Gets the complete Authorization header value for a resource.</summary>
    /// <param name="resourceUrl">The absolute resource URL.</param>
    /// <param name="cancellationToken">A token that can cancel credential acquisition.</param>
    /// <returns>A task containing the complete Authorization header value.</returns>
    Task<string> GetAuthorizationValueAsync(string resourceUrl, CancellationToken cancellationToken);
}
