using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet range operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetRangeResult
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
    /// Stores the number of affected cell items processed by the operation.
    /// </summary>
    public int AffectedCellCount;
    /// <summary>
    /// Stores the number of created cell items processed by the operation.
    /// </summary>
    public int CreatedCellCount;
    /// <summary>
    /// Stores the number of cleared cell items processed by the operation.
    /// </summary>
    public int ClearedCellCount;
    /// <summary>
    /// Stores the number of copied cell items processed by the operation.
    /// </summary>
    public int CopiedCellCount;
    /// <summary>
    /// Stores the number of moved cell items processed by the operation.
    /// </summary>
    public int MovedCellCount;
    /// <summary>
    /// Stores the number of styled cell items processed by the operation.
    /// </summary>
    public int StyledCellCount;
    /// <summary>
    /// Stores the number of formula cell items processed by the operation.
    /// </summary>
    public int FormulaCellCount;
    /// <summary>
    /// Stores the number of sorted row items processed by the operation.
    /// </summary>
    public int SortedRowCount;
    /// <summary>
    /// Stores the number of filled cell items processed by the operation.
    /// </summary>
    public int FilledCellCount;
    /// <summary>
    /// Stores the number of series cell items processed by the operation.
    /// </summary>
    public int SeriesCellCount;
    /// <summary>
    /// Stores the number of transposed cell items processed by the operation.
    /// </summary>
    public int TransposedCellCount;
    /// <summary>
    /// Initializes a new worksheet range result with its default state.
    /// </summary>
    public XlsxWorksheetRangeResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        AffectedCellCount = 0;
        CreatedCellCount = 0;
        ClearedCellCount = 0;
        CopiedCellCount = 0;
        MovedCellCount = 0;
        StyledCellCount = 0;
        FormulaCellCount = 0;
        SortedRowCount = 0;
        FilledCellCount = 0;
        SeriesCellCount = 0;
        TransposedCellCount = 0;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}
