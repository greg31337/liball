using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet cell access operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetCellAccessResult
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
    /// Stores the cell value used by this model.
    /// </summary>
    public XlsxWorksheetCell Cell;
    /// <summary>
    /// Indicates whether cell is present and should be written.
    /// </summary>
    public bool HasCell;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public XlsxCellValue Value;
    /// <summary>
    /// Indicates whether value is present and should be written.
    /// </summary>
    public bool HasValue;
    /// <summary>
    /// Stores the created row value used by this model.
    /// </summary>
    public bool CreatedRow;
    /// <summary>
    /// Stores the created cell value used by this model.
    /// </summary>
    public bool CreatedCell;
    /// <summary>
    /// Initializes a new worksheet cell access result with its default state.
    /// </summary>
    public XlsxWorksheetCellAccessResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Cell = new XlsxWorksheetCell();
        HasCell = false;
        Value = new XlsxCellValue();
        HasValue = false;
        CreatedRow = false;
        CreatedCell = false;
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

    internal static void AddReferenceDiagnostics(XlsxWorksheetCellAccessResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddRangeDiagnostics(XlsxWorksheetCellAccessResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddValueDiagnostics(XlsxWorksheetCellAccessResult result, XlsxCellValueResult valueResult)
    {
        foreach (XlsxDiagnostic diagnostic in valueResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddCellAccessDiagnostics(XlsxWorksheetCellAccessResult result, XlsxWorksheetCellAccessResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
