namespace config;

/// <summary>Defines collection and formatting operations for configuration diagnostics.</summary>
public interface IConfigDiagnostics
{
    /// <summary>Adds an error.</summary>
    ConfigError AddError(string sourceName, string path, string message, int lineNumber, int columnNumber);
    /// <summary>Adds a warning.</summary>
    ConfigWarning AddWarning(string sourceName, string path, string message, int lineNumber, int columnNumber);
    /// <summary>Adds a trace entry.</summary>
    ConfigTrace AddTrace(string action, string sourceName, string path, string message);
    /// <summary>Gets the error count.</summary>
    int ErrorCount();
    /// <summary>Gets the warning count.</summary>
    int WarningCount();
    /// <summary>Gets the trace count.</summary>
    int TraceCount();
    /// <summary>Gets one error or an empty error for an invalid index.</summary>
    ConfigError Error(int index);
    /// <summary>Gets one warning or an empty warning for an invalid index.</summary>
    ConfigWarning Warning(int index);
    /// <summary>Gets one trace entry or an empty entry for an invalid index.</summary>
    ConfigTrace Trace(int index);
    /// <summary>Checks whether errors are present.</summary>
    bool HasErrors();
    /// <summary>Checks whether warnings are present.</summary>
    bool HasWarnings();
    /// <summary>Checks whether trace entries are present.</summary>
    bool HasTraces();
    /// <summary>Formats all diagnostics.</summary>
    string ToDisplayString();
    /// <summary>Formats all diagnostics with secret values masked.</summary>
    string ToRedactedDisplayString(ConfigRedactor redactor);
}
