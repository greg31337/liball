namespace fix;

/// <summary>Identifies session boundaries that reset sequence state.</summary>
public enum SessionResetMode
{
    /// <summary>Sequence state is not reset automatically.</summary>
    None = 0,
    /// <summary>Reset sequence state on logon.</summary>
    OnLogon = 1,
    /// <summary>Reset sequence state on logout.</summary>
    OnLogout = 2,
    /// <summary>Reset sequence state on disconnect.</summary>
    OnDisconnect = 4,
    /// <summary>Reset sequence state at a configured session boundary.</summary>
    OnSessionBoundary = 8
}
