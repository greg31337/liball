using System.Collections.Generic;
using System.Text;

namespace config;

/// <summary>Collects errors, warnings, and merge traces produced for a configuration operation.</summary>
public class ConfigDiagnostics : IConfigDiagnostics
{
    /// <summary>Contains fatal load, parse, merge, resolution, or validation problems.</summary>
    internal List<ConfigError> Errors;
    /// <summary>Contains nonfatal issues that did not prevent a snapshot.</summary>
    internal List<ConfigWarning> Warnings;
    /// <summary>Contains source and merge activity useful for explaining the final values.</summary>
    internal List<ConfigTrace> Traces;

    /// <summary>Creates an empty diagnostic collection.</summary>
    public ConfigDiagnostics()
    {
        Errors = new List<ConfigError>();
        Warnings = new List<ConfigWarning>();
        Traces = new List<ConfigTrace>();
    }

    /// <summary>Adds one fatal source or configuration error.</summary>
    /// <param name="sourceName">The source name, or an empty string when unknown.</param>
    /// <param name="path">The affected configuration path, or an empty string for the root.</param>
    /// <param name="message">The error description.</param>
    /// <param name="lineNumber">The one-based line number, or zero when unavailable.</param>
    /// <param name="columnNumber">The one-based column number, or zero when unavailable.</param>
    /// <returns>The added error.</returns>
    public ConfigError AddError(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        ConfigError error = new ConfigError(sourceName, path, message, lineNumber, columnNumber);
        Errors.Add(error);
        return error;
    }

    internal ConfigError AddLoadError(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        ConfigError error = AddError(sourceName, path, message, lineNumber, columnNumber);
        error.FailureKind = ConfigFailureKind.Load;
        return error;
    }

    internal ConfigError AddParseError(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        ConfigError error = AddError(sourceName, path, message, lineNumber, columnNumber);
        error.FailureKind = ConfigFailureKind.Parse;
        return error;
    }

    internal ConfigError AddMergeError(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        ConfigError error = AddError(sourceName, path, message, lineNumber, columnNumber);
        error.FailureKind = ConfigFailureKind.Merge;
        return error;
    }

    /// <summary>Adds one nonfatal source or configuration warning.</summary>
    /// <param name="sourceName">The source name, or an empty string when unknown.</param>
    /// <param name="path">The affected configuration path, or an empty string for the root.</param>
    /// <param name="message">The warning description.</param>
    /// <param name="lineNumber">The one-based line number, or zero when unavailable.</param>
    /// <param name="columnNumber">The one-based column number, or zero when unavailable.</param>
    /// <returns>The added warning.</returns>
    public ConfigWarning AddWarning(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        ConfigWarning warning = new ConfigWarning(sourceName, path, message, lineNumber, columnNumber);
        Warnings.Add(warning);
        return warning;
    }

    /// <summary>Adds one source or configuration processing trace.</summary>
    /// <param name="action">The processing action name.</param>
    /// <param name="sourceName">The source that performed the action.</param>
    /// <param name="path">The affected configuration path, or an empty string for the root.</param>
    /// <param name="message">Additional trace detail.</param>
    /// <returns>The added trace.</returns>
    public ConfigTrace AddTrace(string action, string sourceName, string path, string message)
    {
        ConfigTrace trace = new ConfigTrace(action, sourceName, path, message);
        Traces.Add(trace);
        return trace;
    }

    /// <summary>Gets the number of recorded errors.</summary>
    /// <returns>The error count.</returns>
    public int ErrorCount()
    {
        return Errors.Count;
    }

    /// <summary>Gets the number of recorded warnings.</summary>
    /// <returns>The warning count.</returns>
    public int WarningCount()
    {
        return Warnings.Count;
    }

    /// <summary>Gets the number of recorded processing traces.</summary>
    /// <returns>The trace count.</returns>
    public int TraceCount()
    {
        return Traces.Count;
    }

    /// <summary>Gets one error without throwing for an out-of-range index.</summary>
    /// <param name="index">The zero-based error index.</param>
    /// <returns>The requested error, or an empty error when the index is invalid.</returns>
    public ConfigError Error(int index)
    {
        if (index < 0 || index >= Errors.Count)
        {
            return new ConfigError("", "", "", 0, 0);
        }

        return Errors[index];
    }

    /// <summary>Gets one warning without throwing for an out-of-range index.</summary>
    /// <param name="index">The zero-based warning index.</param>
    /// <returns>The requested warning, or an empty warning when the index is invalid.</returns>
    public ConfigWarning Warning(int index)
    {
        if (index < 0 || index >= Warnings.Count)
        {
            return new ConfigWarning("", "", "", 0, 0);
        }

        return Warnings[index];
    }

    /// <summary>Gets one trace without throwing for an out-of-range index.</summary>
    /// <param name="index">The zero-based trace index.</param>
    /// <returns>The requested trace, or an empty trace when the index is invalid.</returns>
    public ConfigTrace Trace(int index)
    {
        if (index < 0 || index >= Traces.Count)
        {
            return new ConfigTrace("", "", "", "");
        }

        return Traces[index];
    }

    /// <summary>Checks whether any errors were recorded.</summary>
    /// <returns>True when at least one error exists.</returns>
    public bool HasErrors()
    {
        return Errors.Count > 0;
    }

    /// <summary>Checks whether any warnings were recorded.</summary>
    /// <returns>True when at least one warning exists.</returns>
    public bool HasWarnings()
    {
        return Warnings.Count > 0;
    }

    /// <summary>Checks whether any processing traces were recorded.</summary>
    /// <returns>True when at least one trace exists.</returns>
    public bool HasTraces()
    {
        return Traces.Count > 0;
    }

    internal ConfigDiagnostics Copy()
    {
        ConfigDiagnostics copy = new ConfigDiagnostics();

        int errorIndex = 0;
        while (errorIndex < Errors.Count)
        {
            ConfigError error = Errors[errorIndex];
            ConfigError copiedError = new ConfigError(error.SourceName, error.Path, error.Message, error.LineNumber, error.ColumnNumber);
            copiedError.FailureKind = error.FailureKind;
            copy.Errors.Add(copiedError);
            errorIndex = errorIndex + 1;
        }

        int warningIndex = 0;
        while (warningIndex < Warnings.Count)
        {
            ConfigWarning warning = Warnings[warningIndex];
            copy.Warnings.Add(new ConfigWarning(warning.SourceName, warning.Path, warning.Message, warning.LineNumber, warning.ColumnNumber));
            warningIndex = warningIndex + 1;
        }

        int traceIndex = 0;
        while (traceIndex < Traces.Count)
        {
            ConfigTrace trace = Traces[traceIndex];
            ConfigTrace copiedTrace = new ConfigTrace(trace.Action, trace.SourceName, trace.Path, trace.Message);
            copiedTrace.SourceGroup = trace.SourceGroup;
            copiedTrace.PrecedenceRank = trace.PrecedenceRank;
            copiedTrace.Decision = trace.Decision;
            copy.Traces.Add(copiedTrace);
            traceIndex = traceIndex + 1;
        }

        return copy;
    }

    /// <summary>Formats all diagnostics with errors first, then warnings, then traces.</summary>
    /// <returns>Newline-separated diagnostic text.</returns>
    public string ToDisplayString()
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < Errors.Count)
        {
            AppendLine(builder, Errors[index].ToDisplayString());
            index = index + 1;
        }

        index = 0;
        while (index < Warnings.Count)
        {
            AppendLine(builder, Warnings[index].ToDisplayString());
            index = index + 1;
        }

        index = 0;
        while (index < Traces.Count)
        {
            AppendLine(builder, Traces[index].ToDisplayString());
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>Formats all diagnostics while masking messages associated with secret-like paths.</summary>
    /// <param name="redactor">The secret-path matcher to apply.</param>
    /// <returns>Newline-separated redacted diagnostic text.</returns>
    public string ToRedactedDisplayString(ConfigRedactor redactor)
    {
        StringBuilder builder = new StringBuilder();

        int index = 0;
        while (index < Errors.Count)
        {
            AppendLine(builder, Errors[index].ToRedactedDisplayString(redactor));
            index = index + 1;
        }

        index = 0;
        while (index < Warnings.Count)
        {
            AppendLine(builder, Warnings[index].ToRedactedDisplayString(redactor));
            index = index + 1;
        }

        index = 0;
        while (index < Traces.Count)
        {
            AppendLine(builder, Traces[index].ToRedactedDisplayString(redactor));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void AppendLine(StringBuilder builder, string line)
    {
        if (builder.Length > 0)
        {
            builder.Append("\n");
        }

        builder.Append(line);
    }
}
