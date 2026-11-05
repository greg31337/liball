using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Reports copied sheets and all workbook-global and relationship remappings produced by a merge.
/// </summary>
public class XlsxWorkbookMergeResult
{
    /// <summary>Indicates whether the operation completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains merge diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the number of worksheets copied.</summary>
    public int CopiedSheetCount;
    /// <summary>Contains worksheet-name and part-path mappings.</summary>
    public List<XlsxWorkbookMergeMapping> SheetNameMappings;
    /// <summary>Contains defined-name mappings.</summary>
    public List<XlsxWorkbookMergeMapping> DefinedNameMappings;
    /// <summary>Contains cell-format index mappings.</summary>
    public List<XlsxWorkbookMergeMapping> StyleMappings;
    /// <summary>Contains shared-string index mappings.</summary>
    public List<XlsxWorkbookMergeMapping> SharedStringMappings;
    /// <summary>Contains source-to-destination relationship mappings.</summary>
    public List<XlsxWorkbookMergeMapping> RelationshipMappings;
    /// <summary>Contains table name and part mappings.</summary>
    public List<XlsxWorkbookMergeMapping> TableMappings;
    /// <summary>Contains pivot table and cache part mappings.</summary>
    public List<XlsxWorkbookMergeMapping> PivotMappings;
    /// <summary>Contains drawing part mappings.</summary>
    public List<XlsxWorkbookMergeMapping> DrawingMappings;
    /// <summary>Contains chart part mappings.</summary>
    public List<XlsxWorkbookMergeMapping> ChartMappings;
    /// <summary>Contains comments part mappings.</summary>
    public List<XlsxWorkbookMergeMapping> CommentMappings;
    /// <summary>Contains changed formula mappings.</summary>
    public List<XlsxWorkbookMergeMapping> FormulaMappings;
    /// <summary>Contains every copied package part mapping.</summary>
    public List<XlsxWorkbookMergeMapping> PartMappings;

    /// <summary>Initializes an empty successful merge result.</summary>
    public XlsxWorkbookMergeResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        CopiedSheetCount = 0;
        SheetNameMappings = new List<XlsxWorkbookMergeMapping>();
        DefinedNameMappings = new List<XlsxWorkbookMergeMapping>();
        StyleMappings = new List<XlsxWorkbookMergeMapping>();
        SharedStringMappings = new List<XlsxWorkbookMergeMapping>();
        RelationshipMappings = new List<XlsxWorkbookMergeMapping>();
        TableMappings = new List<XlsxWorkbookMergeMapping>();
        PivotMappings = new List<XlsxWorkbookMergeMapping>();
        DrawingMappings = new List<XlsxWorkbookMergeMapping>();
        ChartMappings = new List<XlsxWorkbookMergeMapping>();
        CommentMappings = new List<XlsxWorkbookMergeMapping>();
        FormulaMappings = new List<XlsxWorkbookMergeMapping>();
        PartMappings = new List<XlsxWorkbookMergeMapping>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal void AddError(string code, string message)
    {
        AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
    }
}
