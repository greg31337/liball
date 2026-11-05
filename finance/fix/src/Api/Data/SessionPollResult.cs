using System.Collections.Generic;

namespace fix;

/// <summary>Contains bounded work and events produced by one session poll.</summary>
public class SessionPollResult
{
    /// <summary>The state after polling.</summary>
    public SessionState State;
    /// <summary>Whether additional work remains immediately available.</summary>
    public bool WorkRemaining;
    /// <summary>The ordered events produced by the poll.</summary>
    public List<SessionEvent> Events = [];
    /// <summary>The latest session snapshot.</summary>
    public SessionSnapshot Snapshot = new SessionSnapshot();
}
