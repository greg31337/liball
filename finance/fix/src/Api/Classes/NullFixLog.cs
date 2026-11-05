namespace fix;

/// <summary>Discards all structured FIX audit entries.</summary>
public class NullFixLog : IFixLog
{
    /// <summary>Creates a null FIX log.</summary>
    public NullFixLog()
    {
    }

    /// <summary>Discards one audit entry without inspecting or modifying it.</summary>
    /// <param name="entry">The structured entry.</param>
    public void WriteLog(LogEntry entry)
    {
    }
}
