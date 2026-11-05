namespace pop3;

/// <summary>Receives redacted POP3 protocol log entries.</summary>
public interface IPop3ProtocolLogger
{
    /// <summary>Records one protocol log entry.</summary>
    /// <param name="entry">The redacted entry to record.</param>
    void Log(Pop3LogEntry entry);
}
