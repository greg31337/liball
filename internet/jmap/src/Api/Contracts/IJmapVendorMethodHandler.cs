using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Processes an application-defined JMAP method without interpreting its arguments.
/// </summary>
public interface IJmapVendorMethodHandler
{
    /// <summary>Processes one opaque vendor method invocation.</summary>
    /// <param name="invocation">The invocation containing the untouched extension arguments.</param>
    /// <param name="context">The authenticated request context.</param>
    /// <param name="cancellationToken">A token that can cancel processing.</param>
    /// <returns>A task containing the vendor method response invocation.</returns>
    Task<JmapInvocation> ProcessVendorMethodAsync(
        JmapInvocation invocation,
        JmapRequestContext context,
        CancellationToken cancellationToken);
}
