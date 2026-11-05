using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet merge cells operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetMergeCellsResult
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
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the first row value used by this model.
    /// </summary>
    public int FirstRow;
    /// <summary>
    /// Stores the first column value used by this model.
    /// </summary>
    public int FirstColumn;
    /// <summary>
    /// Stores the last row value used by this model.
    /// </summary>
    public int LastRow;
    /// <summary>
    /// Stores the last column value used by this model.
    /// </summary>
    public int LastColumn;
    /// <summary>
    /// Stores the merged cell value used by this model.
    /// </summary>
    public XlsxWorksheetMergeCell? MergedCell;
    /// <summary>
    /// Indicates whether merged cell is present and should be written.
    /// </summary>
    public bool HasMergedCell;
    /// <summary>
    /// Stores the number of removed items processed by the operation.
    /// </summary>
    public int RemovedCount;
    /// <summary>
    /// Initializes a new worksheet merge cells result with its default state.
    /// </summary>
    public XlsxWorksheetMergeCellsResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Reference = "";
        FirstRow = 0;
        FirstColumn = 0;
        LastRow = 0;
        LastColumn = 0;
        MergedCell = null;
        HasMergedCell = false;
        RemovedCount = 0;
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

    /// <summary>
    /// Sets range and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "firstRow">The first row value used by the operation.</param>
    /// <param name = "firstColumn">The first column value used by the operation.</param>
    /// <param name = "lastRow">The last row value used by the operation.</param>
    /// <param name = "lastColumn">The last column value used by the operation.</param>
    internal void SetRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        FirstRow = firstRow;
        FirstColumn = firstColumn;
        LastRow = lastRow;
        LastColumn = lastColumn;
        Reference = XlsxWorksheetPart.FormatMergeReference(firstRow, firstColumn, lastRow, lastColumn);
    }
}
