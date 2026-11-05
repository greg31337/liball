namespace fix;

/// <summary>Identifies the lifecycle state of a FIX session.</summary>
public enum SessionState
{
    /// <summary>The session has not been started or has been stopped.</summary>
    Stopped = 0,
    /// <summary>The session is disconnected.</summary>
    Disconnected = 1,
    /// <summary>The session is establishing a transport connection.</summary>
    Connecting = 2,
    /// <summary>The transport is connected but logon is not complete.</summary>
    Connected = 3,
    /// <summary>A logon exchange is pending.</summary>
    LogonPending = 4,
    /// <summary>The session is logged on and active.</summary>
    Active = 5,
    /// <summary>A logout exchange is pending.</summary>
    LogoutPending = 6,
    /// <summary>The initiator is waiting to reconnect.</summary>
    Reconnecting = 7,
    /// <summary>The session stopped because of an unrecoverable failure.</summary>
    Faulted = 8,
    /// <summary>The session is recovering an inbound sequence gap.</summary>
    Recovering = 9
}
