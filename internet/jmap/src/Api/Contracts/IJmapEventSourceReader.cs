using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines stateful reading of a JMAP EventSource stream.
/// </summary>
public interface IJmapEventSourceReader
{
    /// <summary>Connects to the EventSource endpoint.</summary>
    /// <param name="options">The event filtering, close, ping, and resume options.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task representing connection establishment.</returns>
    Task ConnectEventSourceAsync(EventSourceOptions options, CancellationToken cancellationToken);

    /// <summary>Reads the next event or comment from the connected stream.</summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the next parsed event.</returns>
    Task<JmapEvent> ReadEventSourceAsync(CancellationToken cancellationToken);

    /// <summary>Closes the current EventSource connection.</summary>
    void CloseEventSource();
}
