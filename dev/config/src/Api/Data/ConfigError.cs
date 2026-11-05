using System.Collections.Generic;
using System.Text;

namespace config;

/// <summary>Describes one error produced while loading, merging, resolving, or validating configuration.</summary>
public class ConfigError
{
    /// <summary>Names the source associated with the error, or is empty when no source is known.</summary>
    public string SourceName;
    /// <summary>Identifies the affected configuration path, or is empty for the root.</summary>
    public string Path;
    /// <summary>Explains the failure in plain text.</summary>
    public string Message;
    /// <summary>Contains a one-based source line, or zero when unavailable.</summary>
    public int LineNumber;
    /// <summary>Contains a one-based source column, or zero when unavailable.</summary>
    public int ColumnNumber;
    internal ConfigFailureKind FailureKind;

    internal ConfigError(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        SourceName = sourceName;
        Path = path;
        Message = message;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        FailureKind = ConfigFailureKind.None;
    }

    /// <summary>Formats the available source name, line, and column.</summary>
    /// <returns>A source location or <c>(unknown)</c>.</returns>
    internal string Location()
    {
        if (SourceName.Length == 0)
        {
            return "(unknown)";
        }

        if (LineNumber > 0 && ColumnNumber > 0)
        {
            return SourceName + ":" + LineNumber.ToString() + ":" + ColumnNumber.ToString();
        }

        return SourceName;
    }

    /// <summary>Formats the complete error for logs or developer output.</summary>
    /// <returns>A single-line error description.</returns>
    internal string ToDisplayString()
    {
        return "error " + Location() + " " + PathLabel() + ": " + Message;
    }

    /// <summary>Formats the error while masking its message when the path is considered secret.</summary>
    /// <param name="redactor">The secret-path matcher to apply.</param>
    /// <returns>A single-line redacted error description.</returns>
    internal string ToRedactedDisplayString(ConfigRedactor redactor)
    {
        return "error " + Location() + " " + PathLabel() + ": " + redactor.RedactDiagnosticMessage(Path, Message);
    }

    internal string PathLabel()
    {
        if (Path.Length == 0)
        {
            return "(root)";
        }

        return Path;
    }
}
