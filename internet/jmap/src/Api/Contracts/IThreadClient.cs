using System.Threading;
using System.Threading.Tasks;

namespace jmap;

/// <summary>
/// Defines typed operations for JMAP Threads.
/// </summary>
public interface IThreadClient
{
    /// <summary>Gets Threads by identifier or retrieves all available Threads.</summary>
    /// <param name="request">The Thread/get arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Thread/get result.</returns>
    Task<ThreadGetResponse> GetThreadsAsync(ThreadGetRequest request, CancellationToken cancellationToken);

    /// <summary>Gets Thread changes since a previous collection state.</summary>
    /// <param name="request">The Thread/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the Thread/changes result.</returns>
    Task<ThreadChangesResponse> GetThreadChangesAsync(ThreadChangesRequest request, CancellationToken cancellationToken);

    /// <summary>Gets and combines every Thread changes page needed to reach the current state.</summary>
    /// <param name="request">The initial Thread/changes arguments.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing one deduplicated aggregate changes result.</returns>
    Task<ThreadChangesResponse> GetAllThreadChangesAsync(ThreadChangesRequest request, CancellationToken cancellationToken);
}
