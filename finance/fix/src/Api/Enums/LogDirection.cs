namespace fix;

/// <summary>Identifies the direction or category of an audit-log entry.</summary>
public enum LogDirection
{
    /// <summary>The entry describes inbound data.</summary>
    Incoming = 0,
    /// <summary>The entry describes outbound data.</summary>
    Outgoing = 1,
    /// <summary>The entry describes a local event.</summary>
    Event = 2
}
