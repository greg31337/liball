namespace diameter;

/// <summary>
/// Provides volatile, thread-safe authorization-session persistence with
/// optimistic version checks.
/// </summary>
public class DiameterInMemorySessionStore : IDiameterSessionStore
{
    internal SessionTable Table = new SessionTable();

    /// <summary>Initializes an empty in-memory session store.</summary>
    public DiameterInMemorySessionStore()
    {
    }

    /// <summary>Loads one owned in-memory session record.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned record, or <c>null</c> when absent.</returns>
    public DiameterSessionRecord? LoadDiameterSession(string? sessionId)
    {
        return Table.ReadSessionRecord(sessionId);
    }

    /// <summary>Atomically creates or replaces one owned session record.</summary>
    /// <param name="record">The complete record to persist.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The persisted owned record with its new version.</returns>
    public DiameterSessionRecord SaveDiameterSession(
        DiameterSessionRecord? record,
        long expectedVersion)
    {
        return Table.SaveSessionRecord(record, expectedVersion);
    }

    /// <summary>Atomically removes a session whose version still matches.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns><c>true</c> when the matching session was removed.</returns>
    public bool RemoveDiameterSession(
        string? sessionId,
        long expectedVersion)
    {
        return Table.RemoveSessionRecord(sessionId, expectedVersion);
    }
}
