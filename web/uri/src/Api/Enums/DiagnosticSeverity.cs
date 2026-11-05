namespace uri;

/// <summary>
/// Identifies whether a diagnostic prevents an operation from succeeding.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>The operation cannot produce its requested result.</summary>
    Error = 0,

    /// <summary>The operation produced a result after ignoring or recovering from input.</summary>
    Warning = 1
}
