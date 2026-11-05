namespace htmlgui;

/// <summary>Creates deterministic DOM snapshots and coordinates revisioned incremental updates.</summary>
public interface IUiUpdateEngine
{
    /// <summary>Creates a canonical detached DOM snapshot.</summary>
    /// <param name="roots">The renderer-neutral roots to annotate and copy.</param>
    /// <param name="sessionId">The stable rendering or bridge session identifier.</param>
    /// <param name="rootSeed">The deterministic seed shared by all roots.</param>
    /// <param name="revision">The nonnegative snapshot revision.</param>
    /// <returns>The canonical detached snapshot.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot identity or render tree is invalid.</exception>
    DomSnapshot CreateDomSnapshot(DomNode[]? roots, string? sessionId, string? rootSeed, int revision);

    /// <summary>Computes one revision transaction from the currently accepted snapshot.</summary>
    /// <param name="before">The accepted base snapshot.</param>
    /// <param name="after">The desired next snapshot.</param>
    /// <param name="priority">The scheduling priority carried by the batch.</param>
    /// <param name="currentPreservation">The optional browser state captured before applying the batch.</param>
    /// <returns>The detached ordered mutation batch.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a snapshot, priority, or accepted base is invalid.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when another batch is pending or the revision cannot advance.</exception>
    UiUpdateBatch ComputeDomUpdates(DomSnapshot? before, DomSnapshot? after, UiUpdatePriority priority, UiPreservationState? currentPreservation);

    /// <summary>Accepts a pending update batch and commits its resulting snapshot.</summary>
    /// <param name="batchId">The stable update batch identifier.</param>
    /// <param name="revision">The resulting batch revision.</param>
    /// <returns>The exceptionless acknowledgement or recovery result.</returns>
    UiUpdateResult AcceptDomUpdates(string? batchId, int revision);

    /// <summary>Rejects a pending update batch and retains its accepted base snapshot.</summary>
    /// <param name="batchId">The stable update batch identifier.</param>
    /// <param name="revision">The rejected batch revision.</param>
    /// <returns>The exceptionless rejection and recovery result.</returns>
    UiUpdateResult RejectDomUpdates(string? batchId, int revision);
}
