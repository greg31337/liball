namespace sip;

/// <summary>Identifies a unified SIP stack event.</summary>
public enum SipStackEventKind
{
    /// <summary>No event is available.</summary>
    None = 0,
    /// <summary>The stack started.</summary>
    Started = 1,
    /// <summary>The stack stopped.</summary>
    Stopped = 2,
    /// <summary>An incoming request is available.</summary>
    IncomingRequest = 3,
    /// <summary>An outgoing response was sent.</summary>
    OutgoingResponse = 4,
    /// <summary>A transport event is available.</summary>
    Transport = 5,
    /// <summary>A transaction event is available.</summary>
    Transaction = 6,
    /// <summary>A dialog event is available.</summary>
    Dialog = 7,
    /// <summary>A registration event is available.</summary>
    Registration = 8,
    /// <summary>An authentication event is available.</summary>
    Authentication = 9,
    /// <summary>An operation failed.</summary>
    Error = 10,
    /// <summary>An application extension dispatch result is available.</summary>
    Extension = 11
}
