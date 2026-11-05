using System.Collections.Generic;

namespace diameter;

/// <summary>Creates, updates, ends, and inspects authorization sessions.</summary>
public interface IDiameterSessionManager
{
    /// <summary>Creates one authorization session.</summary>
    /// <param name="configuration">The validated session configuration.</param>
    /// <returns>The created owned session snapshot.</returns>
    DiameterSessionSnapshot CreateDiameterSession(
        DiameterSessionConfiguration? configuration);

    /// <summary>Finds one session without changing it.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned snapshot, or <c>null</c> when the session is absent.</returns>
    DiameterSessionSnapshot? FindDiameterSession(string? sessionId);

    /// <summary>Applies one optimistic session update.</summary>
    /// <param name="update">The requested update and expected version.</param>
    /// <returns>The updated owned session snapshot.</returns>
    DiameterSessionSnapshot UpdateDiameterSession(
        DiameterSessionUpdate? update);

    /// <summary>Ends one session with an RFC 6733 termination cause.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="cause">The reason the session is ending.</param>
    /// <returns>The terminal owned session snapshot.</returns>
    DiameterSessionSnapshot EndDiameterSession(
        string? sessionId,
        DiameterTerminationCause cause);

    /// <summary>Reads owned snapshots of all retained sessions.</summary>
    /// <returns>The session snapshots in deterministic creation order.</returns>
    List<DiameterSessionSnapshot> ReadDiameterSessionSnapshots();
}

/// <summary>Persists authorization-session records with optimistic concurrency.</summary>
public interface IDiameterSessionStore
{
    /// <summary>Loads one owned persisted session record.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned record, or <c>null</c> when the session is absent.</returns>
    DiameterSessionRecord? LoadDiameterSession(string? sessionId);

    /// <summary>Atomically creates or replaces one session record.</summary>
    /// <param name="record">The complete owned record to persist.</param>
    /// <param name="expectedVersion">Zero for creation or the version being replaced.</param>
    /// <returns>The persisted owned record with its new version.</returns>
    DiameterSessionRecord SaveDiameterSession(
        DiameterSessionRecord? record,
        long expectedVersion);

    /// <summary>Atomically removes one session when its version still matches.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="expectedVersion">The version that must be removed.</param>
    /// <returns><c>true</c> when the matching record was removed.</returns>
    bool RemoveDiameterSession(string? sessionId, long expectedVersion);
}

/// <summary>Receives owned authorization-session lifecycle transitions.</summary>
public interface ISessionEventHandler
{
    /// <summary>Handles one session transition.</summary>
    /// <param name="eventData">The owned transition event.</param>
    void HandleDiameterSessionEvent(DiameterSessionEvent eventData);
}
