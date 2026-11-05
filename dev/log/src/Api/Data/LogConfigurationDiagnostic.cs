namespace log;

/// <summary>
/// Describes one validation problem found in a logger configuration.
/// </summary>
public class LogConfigurationDiagnostic
{
    internal const string ErrorSeverity = "error";
    internal const string WarningSeverity = "warning";

    /// <summary>
    /// The diagnostic severity text.
    /// </summary>
    public string Severity = "";
    /// <summary>
    /// The stable machine-readable outcome or diagnostic code.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// The human-readable message.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// The configuration path associated with the diagnostic.
    /// </summary>
    public string Path = "";
    /// <summary>
    /// Whether the diagnostic is an error that makes the configuration invalid.
    /// </summary>
    public bool IsError;
    /// <summary>
    /// Whether the diagnostic is advisory.
    /// </summary>
    public bool IsWarning;

    internal LogConfigurationDiagnostic(string severity, string code, string message, string path)
    {
        Severity = LogResult.NormalizeText(severity);
        Code = LogResult.NormalizeText(code);
        Message = LogResult.NormalizeText(message);
        Path = LogResult.NormalizeText(path);
        IsError = Severity == LogConfigurationDiagnostic.ErrorSeverity;
        IsWarning = Severity == LogConfigurationDiagnostic.WarningSeverity;
    }

    internal static LogConfigurationDiagnostic Error(string code, string message, string path)
    {
        return new LogConfigurationDiagnostic(LogConfigurationDiagnostic.ErrorSeverity, code, message, path);
    }

    internal static LogConfigurationDiagnostic Warning(string code, string message, string path)
    {
        return new LogConfigurationDiagnostic(LogConfigurationDiagnostic.WarningSeverity, code, message, path);
    }

    internal static LogConfigurationDiagnostic[] AppendDiagnostic(LogConfigurationDiagnostic[] diagnostics, LogConfigurationDiagnostic diagnostic)
    {
        LogConfigurationDiagnostic[] source = diagnostics;
        if (source == null)
        {
            source = new LogConfigurationDiagnostic[0];
        }

        if (diagnostic == null)
        {
            return LogConfigurationDiagnostic.CopyDiagnostics(source);
        }

        LogConfigurationDiagnostic[] copy = new LogConfigurationDiagnostic[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        copy[source.Length] = diagnostic;
        return copy;
    }

    internal static LogConfigurationDiagnostic[] CopyDiagnostics(LogConfigurationDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            return new LogConfigurationDiagnostic[0];
        }

        LogConfigurationDiagnostic[] copy = new LogConfigurationDiagnostic[diagnostics.Length];
        int index = 0;
        while (index < diagnostics.Length)
        {
            copy[index] = diagnostics[index];
            index = index + 1;
        }

        return copy;
    }

    internal static int CountErrors(LogConfigurationDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.IsError)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int CountWarnings(LogConfigurationDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.IsWarning)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static LogConfigurationDiagnostic? FirstDiagnostic(LogConfigurationDiagnostic[]? diagnostics)
    {
        if (diagnostics == null)
        {
            return null;
        }

        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null)
            {
                return diagnostic;
            }
        }

        return null;
    }

    internal static LogConfigurationDiagnostic? FirstError(LogConfigurationDiagnostic[]? diagnostics)
    {
        if (diagnostics == null)
        {
            return null;
        }

        foreach (LogConfigurationDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.IsError)
            {
                return diagnostic;
            }
        }

        return null;
    }
}
