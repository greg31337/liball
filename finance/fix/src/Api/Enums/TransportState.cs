namespace fix;

/// <summary>Identifies the state of a session transport.</summary>
public enum TransportState
{
    /// <summary>The transport is disconnected.</summary>
    Disconnected = 0,
    /// <summary>The transport is connecting.</summary>
    Connecting = 1,
    /// <summary>The transport is listening for connections.</summary>
    Listening = 2,
    /// <summary>The transport is connected.</summary>
    Connected = 3,
    /// <summary>The transport has been closed.</summary>
    Closed = 4,
    /// <summary>The transport failed.</summary>
    Faulted = 5
}
