using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Locates a JMAP Session resource from explicit or username-domain discovery options.
/// </summary>
public interface IJmapServiceLocator
{
    /// <summary>Locates the absolute JMAP Session URL.</summary>
    /// <param name="options">The direct URL or username-domain discovery options.</param>
    /// <param name="cancellationToken">A token that can cancel discovery.</param>
    /// <returns>A task containing the absolute Session URL.</returns>
    Task<string> LocateSessionUrlAsync(JmapDiscoveryOptions options, CancellationToken cancellationToken);
}
