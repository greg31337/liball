using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Collects validation diagnostics and reports whether any error-level diagnostic was produced.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxValidationResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics.
    /// </summary>
    public List<XlsxDiagnostic> Diagnostics;
    /// <summary>
    /// Initializes a new validation result with its default state.
    /// </summary>
    public XlsxValidationResult()
    {
        IsValid = true;
        Diagnostics = new List<XlsxDiagnostic>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        if (diagnostic.Severity == XlsxDiagnosticSeverity.Error)
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Adds info to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    /// <returns>The diagnostic produced by the operation.</returns>
    internal XlsxDiagnostic AddInfo(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Info, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }

    /// <summary>
    /// Adds warning to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    /// <returns>The diagnostic produced by the operation.</returns>
    internal XlsxDiagnostic AddWarning(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }

    /// <summary>
    /// Performs the count severity operation using the current model state.
    /// </summary>
    /// <param name = "severity">The severity value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int CountSeverity(XlsxDiagnosticSeverity severity)
    {
        int count = 0;
        foreach (XlsxDiagnostic diagnostic in Diagnostics)
        {
            if (diagnostic.Severity == severity)
            {
                count = count + 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Performs the has errors operation using the current model state.
    /// </summary>
    /// <returns><c>true</c> when the requested condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasErrors()
    {
        return CountSeverity(XlsxDiagnosticSeverity.Error) > 0;
    }
}
