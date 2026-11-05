namespace pop3;

/// <summary>Describes one redacted POP3 protocol log entry.</summary>
public class Pop3LogEntry
{
    /// <summary>The source and destination of the logged information.</summary>
    public Pop3LogDirection Direction;

    /// <summary>The safe command, response, or state text.</summary>
    public string Text = "";

    /// <summary>The number of wire bytes represented by the entry.</summary>
    public long ByteCount;

    /// <summary>The session state associated with the entry.</summary>
    public Pop3SessionState SessionState;

    /// <summary>Indicates whether sensitive wire content was replaced with redacted metadata.</summary>
    public bool IsRedacted;
}
