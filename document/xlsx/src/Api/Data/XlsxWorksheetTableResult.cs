using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet table operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetTableResult
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
    /// Stores the table value used by this model.
    /// </summary>
    public XlsxTablePart? Table;
    /// <summary>
    /// Indicates whether table is present and should be written.
    /// </summary>
    public bool HasTable;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public XlsxTableColumn? Column;
    /// <summary>
    /// Indicates whether column is present and should be written.
    /// </summary>
    public bool HasColumn;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public XlsxWorksheetTableReference? TableReference;
    /// <summary>
    /// Indicates whether table reference is present and should be written.
    /// </summary>
    public bool HasTableReference;
    /// <summary>
    /// Stores the number of created cell items processed by the operation.
    /// </summary>
    public int CreatedCellCount;
    /// <summary>
    /// Stores the number of updated cell items processed by the operation.
    /// </summary>
    public int UpdatedCellCount;
    /// <summary>
    /// Stores the number of added column items processed by the operation.
    /// </summary>
    public int AddedColumnCount;
    /// <summary>
    /// Stores the number of removed column items processed by the operation.
    /// </summary>
    public int RemovedColumnCount;
    /// <summary>
    /// Stores the number of formula cell items processed by the operation.
    /// </summary>
    public int FormulaCellCount;
    /// <summary>
    /// Initializes a new worksheet table result with its default state.
    /// </summary>
    public XlsxWorksheetTableResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Table = null;
        HasTable = false;
        Column = null;
        HasColumn = false;
        TableReference = null;
        HasTableReference = false;
        CreatedCellCount = 0;
        UpdatedCellCount = 0;
        AddedColumnCount = 0;
        RemovedColumnCount = 0;
        FormulaCellCount = 0;
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

    internal static void AddRangeDiagnostics(XlsxWorksheetTableResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddCellAccessDiagnostics(XlsxWorksheetTableResult result, XlsxWorksheetCellAccessResult cellResult)
    {
        foreach (XlsxDiagnostic diagnostic in cellResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
