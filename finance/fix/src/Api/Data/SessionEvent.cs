using System;

namespace fix;

/// <summary>Describes one ordered event emitted while polling a session.</summary>
public class SessionEvent
{
    /// <summary>The session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The event kind.</summary>
    public SessionEventKind Kind;
    /// <summary>The event timestamp in UTC.</summary>
    public DateTime TimestampUtc;
    /// <summary>The related message when one is available.</summary>
    public FixMessage? Message;
    /// <summary>The related sequence number, or zero when not applicable.</summary>
    public int SequenceNumber;
    /// <summary>A stable machine-readable event code.</summary>
    public string Code = "";
    /// <summary>A human-readable event description.</summary>
    public string Text = "";
}
