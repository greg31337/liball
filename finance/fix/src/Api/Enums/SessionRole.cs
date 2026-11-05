namespace fix;

/// <summary>Identifies a FIX session endpoint's role.</summary>
public enum SessionRole
{
    /// <summary>The endpoint initiates connections.</summary>
    Initiator = 0,
    /// <summary>The endpoint accepts connections.</summary>
    Acceptor = 1
}
