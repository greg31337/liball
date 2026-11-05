using System;

namespace fix;

/// <summary>Represents one structured session audit-log entry.</summary>
public class LogEntry
{
    /// <summary>The entry timestamp in UTC.</summary>
    public DateTime TimestampUtc;
    /// <summary>The session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The data direction or local event category.</summary>
    public LogDirection Direction;
    /// <summary>The related session event kind.</summary>
    public SessionEventKind EventKind;
    /// <summary>The exact message bytes after configured redaction.</summary>
    public byte[] Bytes = [];
    /// <summary>A human-readable audit description.</summary>
    public string Text = "";
    /// <summary>Whether sensitive values were redacted.</summary>
    public bool IsRedacted;
}
