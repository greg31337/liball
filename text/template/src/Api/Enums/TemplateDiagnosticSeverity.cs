using System;
using System.Text;

namespace template;

/// <summary>
/// Defines the supported diagnostic severity values used by the template API.
/// </summary>
public enum TemplateDiagnosticSeverity
{
    /// <summary>
    /// Marks a diagnostic as information.
    /// </summary>
    Information = 0,
    /// <summary>
    /// Marks a diagnostic as warning.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Marks a diagnostic as error.
    /// </summary>
    Error = 2
}

