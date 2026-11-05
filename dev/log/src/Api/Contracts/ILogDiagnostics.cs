using System;

namespace log;

/// <summary>
/// Defines control and inspection of the logging library's self-diagnostic channel.
/// </summary>
public interface ILogDiagnostics
{
    /// <summary>Reports whether self-diagnostics are enabled.</summary>
    bool IsEnabled();
    /// <summary>Returns the number of written diagnostic entries.</summary>
    int GetWrittenCount();
    /// <summary>Returns the number of dropped reentrant entries.</summary>
    int GetDroppedReentrantCount();
    /// <summary>Enables callback-based self-diagnostics.</summary>
    void Enable(Action<string>? writeAction);
    /// <summary>Enables in-memory self-diagnostics.</summary>
    void EnableMemory();
    /// <summary>Disables self-diagnostics.</summary>
    void Disable();
    /// <summary>Clears retained diagnostics and counters.</summary>
    void Clear();
    /// <summary>Returns retained diagnostic entries.</summary>
    string[] GetEntries();
}
