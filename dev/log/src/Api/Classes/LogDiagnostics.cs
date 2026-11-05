using System;

namespace log;

/// <summary>
/// Controls and inspects the logging library's self-diagnostic channel.
/// </summary>
public class LogDiagnostics : ILogDiagnostics
{
    /// <summary>Creates a self-diagnostic controller.</summary>
    public LogDiagnostics()
    {
    }

    /// <summary>Reports whether self-diagnostics are enabled.</summary>
    public bool IsEnabled()
    {
        return LogSelfLog.IsEnabled();
    }

    /// <summary>Returns the number of written diagnostic entries.</summary>
    public int GetWrittenCount()
    {
        return LogSelfLog.WrittenCount();
    }

    /// <summary>Returns the number of dropped reentrant entries.</summary>
    public int GetDroppedReentrantCount()
    {
        return LogSelfLog.DroppedReentrantCount();
    }

    /// <summary>Enables callback-based self-diagnostics.</summary>
    public void Enable(Action<string>? writeAction)
    {
        LogSelfLog.Enable(writeAction);
    }

    /// <summary>Enables in-memory self-diagnostics.</summary>
    public void EnableMemory()
    {
        LogSelfLog.EnableMemory();
    }

    /// <summary>Disables self-diagnostics.</summary>
    public void Disable()
    {
        LogSelfLog.Disable();
    }

    /// <summary>Clears retained diagnostics and counters.</summary>
    public void Clear()
    {
        LogSelfLog.Clear();
    }

    /// <summary>Returns retained diagnostic entries.</summary>
    public string[] GetEntries()
    {
        return LogSelfLog.Entries();
    }
}
