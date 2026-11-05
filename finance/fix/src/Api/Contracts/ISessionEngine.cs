namespace fix;

/// <summary>Owns multiple independently configured caller-driven sessions.</summary>
public interface ISessionEngine
{
    /// <summary>Adds and starts an independently configured session.</summary>
    /// <param name="settings">The session settings.</param>
    /// <returns>The initial session state and events.</returns>
    SessionPollResult AddSession(SessionSettings settings);
    /// <summary>Removes and stops one session.</summary>
    /// <param name="id">The session identity.</param>
    /// <returns>The final session state and events.</returns>
    SessionPollResult RemoveSession(SessionId id);
    /// <summary>Polls all sessions once in deterministic order.</summary>
    /// <returns>The aggregated events and latest processed snapshot.</returns>
    SessionPollResult PollSessions();
    /// <summary>Gets the latest snapshot for one session.</summary>
    /// <param name="id">The session identity.</param>
    /// <returns>The session snapshot.</returns>
    SessionSnapshot ResolveSessionSnapshot(SessionId id);
}
