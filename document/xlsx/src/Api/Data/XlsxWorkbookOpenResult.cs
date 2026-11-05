using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook open operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookOpenResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the workbook value used by this model.
    /// </summary>
    public XlsxWorkbook? Workbook;
    /// <summary>
    /// Contains statistics.
    /// </summary>
    public XlsxPerformanceCounters Statistics;
    /// <summary>
    /// Initializes a new workbook open result with its default state.
    /// </summary>
    public XlsxWorkbookOpenResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Workbook = null;
        Statistics = new XlsxPerformanceCounters();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
