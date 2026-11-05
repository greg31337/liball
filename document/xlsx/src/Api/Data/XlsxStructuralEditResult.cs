using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains deterministic category counts and reference changes produced by a structural workbook edit.
/// </summary>
public class XlsxStructuralEditResult
{
    /// <summary>
    /// Indicates whether the edit completed without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains structural edit diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the structural edit kind.
    /// </summary>
    public XlsxStructuralEditKind Kind;
    /// <summary>
    /// Stores the edited worksheet name.
    /// </summary>
    public string SheetName;
    /// <summary>
    /// Stores the previous worksheet name for rename operations.
    /// </summary>
    public string PreviousSheetName;
    /// <summary>
    /// Stores the number of affected rows, columns, cells, ranges, or sheets.
    /// </summary>
    public int AffectedCount;
    /// <summary>
    /// Stores the number of formula updates.
    /// </summary>
    public int FormulaUpdateCount;
    /// <summary>
    /// Stores the number of defined-name updates.
    /// </summary>
    public int NameUpdateCount;
    /// <summary>
    /// Stores the number of table updates.
    /// </summary>
    public int TableUpdateCount;
    /// <summary>
    /// Stores the number of pivot updates.
    /// </summary>
    public int PivotUpdateCount;
    /// <summary>
    /// Stores the number of data-validation updates.
    /// </summary>
    public int ValidationUpdateCount;
    /// <summary>
    /// Stores the number of conditional-formatting updates.
    /// </summary>
    public int ConditionalFormattingUpdateCount;
    /// <summary>
    /// Stores the number of hyperlink updates.
    /// </summary>
    public int HyperlinkUpdateCount;
    /// <summary>
    /// Stores the number of comment updates.
    /// </summary>
    public int CommentUpdateCount;
    /// <summary>
    /// Stores the number of drawing-anchor updates.
    /// </summary>
    public int DrawingUpdateCount;
    /// <summary>
    /// Stores the number of chart-series updates.
    /// </summary>
    public int ChartUpdateCount;
    /// <summary>
    /// Stores the number of page-break updates.
    /// </summary>
    public int PageBreakUpdateCount;
    /// <summary>
    /// Stores the number of print-definition updates.
    /// </summary>
    public int PrintDefinitionUpdateCount;
    /// <summary>
    /// Stores the number of relationship updates.
    /// </summary>
    public int RelationshipUpdateCount;
    /// <summary>
    /// Stores the number of removed references or objects.
    /// </summary>
    public int RemovedReferenceCount;
    /// <summary>
    /// Contains deterministic reference changes in feature and collection order.
    /// </summary>
    public List<XlsxStructuralReferenceUpdate> Updates;

    /// <summary>
    /// Initializes an empty structural edit result.
    /// </summary>
    public XlsxStructuralEditResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Kind = XlsxStructuralEditKind.RowInsert;
        SheetName = "";
        PreviousSheetName = "";
        AffectedCount = 0;
        FormulaUpdateCount = 0;
        NameUpdateCount = 0;
        TableUpdateCount = 0;
        PivotUpdateCount = 0;
        ValidationUpdateCount = 0;
        ConditionalFormattingUpdateCount = 0;
        HyperlinkUpdateCount = 0;
        CommentUpdateCount = 0;
        DrawingUpdateCount = 0;
        ChartUpdateCount = 0;
        PageBreakUpdateCount = 0;
        PrintDefinitionUpdateCount = 0;
        RelationshipUpdateCount = 0;
        RemovedReferenceCount = 0;
        Updates = new List<XlsxStructuralReferenceUpdate>();
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
