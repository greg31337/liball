using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet used range operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetUsedRangeResult
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
    /// Indicates whether range is present and should be written.
    /// </summary>
    public bool HasRange;
    /// <summary>
    /// Stores the min row value used by this model.
    /// </summary>
    public int MinRow;
    /// <summary>
    /// Stores the min column value used by this model.
    /// </summary>
    public int MinColumn;
    /// <summary>
    /// Stores the max row value used by this model.
    /// </summary>
    public int MaxRow;
    /// <summary>
    /// Stores the max column value used by this model.
    /// </summary>
    public int MaxColumn;
    /// <summary>
    /// Stores the number of cell items processed by the operation.
    /// </summary>
    public int CellCount;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the range value used by this model.
    /// </summary>
    public XlsxA1RangeReferenceValue Range;
    /// <summary>
    /// Initializes a new worksheet used range result with its default state.
    /// </summary>
    public XlsxWorksheetUsedRangeResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        HasRange = false;
        MinRow = 0;
        MinColumn = 0;
        MaxRow = 0;
        MaxColumn = 0;
        CellCount = 0;
        Reference = "";
        Range = new XlsxA1RangeReferenceValue();
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
    /// Sets bounds and updates the associated presence or changed-state fields.
    /// </summary>
    /// <param name = "rowIndex">A one-based worksheet row index.</param>
    /// <param name = "columnIndex">A one-based worksheet column index.</param>
    internal void SetBounds(int rowIndex, int columnIndex)
    {
        if (!HasRange)
        {
            HasRange = true;
            MinRow = rowIndex;
            MinColumn = columnIndex;
            MaxRow = rowIndex;
            MaxColumn = columnIndex;
            CellCount = 1;
            return;
        }

        if (rowIndex < MinRow)
        {
            MinRow = rowIndex;
        }

        if (rowIndex > MaxRow)
        {
            MaxRow = rowIndex;
        }

        if (columnIndex < MinColumn)
        {
            MinColumn = columnIndex;
        }

        if (columnIndex > MaxColumn)
        {
            MaxColumn = columnIndex;
        }

        CellCount = CellCount + 1;
    }

    /// <summary>
    /// Performs the update range reference operation using the current model state.
    /// </summary>
    internal void UpdateRangeReference()
    {
        if (!HasRange)
        {
            Reference = "";
            Range = new XlsxA1RangeReferenceValue();
            return;
        }

        Range = new XlsxA1RangeReferenceValue();
        Range.First.Row = MinRow;
        Range.First.Column = MinColumn;
        Range.Last.Row = MaxRow;
        Range.Last.Column = MaxColumn;
        Range.HasLast = MinRow != MaxRow || MinColumn != MaxColumn;
        Reference = XlsxA1RangeReferenceConverter.FormatString(Range);
        Range.Text = Reference;
    }

    internal static void AddReferenceDiagnostics(XlsxWorksheetUsedRangeResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddRangeDiagnostics(XlsxWorksheetUsedRangeResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
