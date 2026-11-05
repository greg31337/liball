using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook recalculation operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookRecalculationResult
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
    /// Stores the number of preserved formula items processed by the operation.
    /// </summary>
    public int PreservedFormulaCount;
    /// <summary>
    /// Stores the number of recalculated formula items processed by the operation.
    /// </summary>
    public int RecalculatedFormulaCount;
    /// <summary>
    /// Stores the number of changed formula items processed by the operation.
    /// </summary>
    public int ChangedFormulaCount;
    /// <summary>
    /// Stores the number of spill cell items processed by the operation.
    /// </summary>
    public int SpillCellCount;
    /// <summary>
    /// Stores the marked for excel recalculation value used by this model.
    /// </summary>
    public bool MarkedForExcelRecalculation;
    /// <summary>
    /// Initializes a new workbook recalculation result with its default state.
    /// </summary>
    public XlsxWorkbookRecalculationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PreservedFormulaCount = 0;
        RecalculatedFormulaCount = 0;
        ChangedFormulaCount = 0;
        SpillCellCount = 0;
        MarkedForExcelRecalculation = false;
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
