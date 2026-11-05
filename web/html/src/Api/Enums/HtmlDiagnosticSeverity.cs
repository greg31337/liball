using System;
using System.Globalization;

namespace html;
/// <summary>Classifies a diagnostic as informational, recoverable, or fatal to the requested operation.</summary>
public enum HtmlDiagnosticSeverity
{
    /// <summary>Marks an informational diagnostic that does not indicate failure.</summary>
    Information = 0,
    /// <summary>Marks a recoverable condition that callers may need to inspect.</summary>
    Warning = 1,
    /// <summary>Marks an operation error that can prevent complete output.</summary>
    Error = 2,
    /// <summary>Marks an unrecoverable condition that prevents usable output.</summary>
    Fatal = 3
}
