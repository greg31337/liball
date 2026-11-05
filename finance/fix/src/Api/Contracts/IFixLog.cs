namespace fix;

/// <summary>Receives structured FIX session audit entries.</summary>
public interface IFixLog
{
    /// <summary>Writes one audit entry.</summary>
    /// <param name="entry">The structured entry.</param>
    void WriteLog(LogEntry entry);
}
