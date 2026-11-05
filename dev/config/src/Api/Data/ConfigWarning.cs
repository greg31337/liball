namespace config;

/// <summary>Describes one nonfatal issue produced while processing configuration.</summary>
public class ConfigWarning
{
    /// <summary>Names the source associated with the warning, or is empty when no source is known.</summary>
    public string SourceName;
    /// <summary>Identifies the affected configuration path, or is empty for the root.</summary>
    public string Path;
    /// <summary>Explains the issue in plain text.</summary>
    public string Message;
    /// <summary>Contains a one-based source line, or zero when unavailable.</summary>
    public int LineNumber;
    /// <summary>Contains a one-based source column, or zero when unavailable.</summary>
    public int ColumnNumber;

    internal ConfigWarning(string sourceName, string path, string message, int lineNumber, int columnNumber)
    {
        SourceName = sourceName;
        Path = path;
        Message = message;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
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

    /// <summary>Formats the complete warning for logs or developer output.</summary>
    /// <returns>A single-line warning description.</returns>
    internal string ToDisplayString()
    {
        return "warning " + Location() + " " + PathLabel() + ": " + Message;
    }

    /// <summary>Formats the warning while masking its message when the path is considered secret.</summary>
    /// <param name="redactor">The secret-path matcher to apply.</param>
    /// <returns>A single-line redacted warning description.</returns>
    internal string ToRedactedDisplayString(ConfigRedactor redactor)
    {
        return "warning " + Location() + " " + PathLabel() + ": " + redactor.RedactDiagnosticMessage(Path, Message);
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
