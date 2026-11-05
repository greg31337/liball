using System;

namespace config;

/// <summary>Represents a failure while converting or binding a configuration value.</summary>
public class ConfigConversionException : System.Exception
{
    /// <summary>Names the source associated with the failure, or is empty when unknown.</summary>
    public string SourceName;
    /// <summary>Identifies the affected configuration path, or is empty for the root.</summary>
    public string Path;
    /// <summary>Contains a one-based source line, or zero when unavailable.</summary>
    public int LineNumber;
    /// <summary>Contains a one-based source column, or zero when unavailable.</summary>
    public int ColumnNumber;

    /// <summary>Creates a conversion exception with available source context.</summary>
    /// <param name="message">The failure description.</param>
    /// <param name="sourceName">The source name, or an empty string when unknown.</param>
    /// <param name="path">The affected configuration path, or an empty string for the root.</param>
    /// <param name="lineNumber">The one-based line number, or zero when unavailable.</param>
    /// <param name="columnNumber">The one-based column number, or zero when unavailable.</param>
    public ConfigConversionException(string message, string sourceName, string path, int lineNumber, int columnNumber) : base(message)
    {
        SourceName = sourceName;
        Path = path;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }
}
