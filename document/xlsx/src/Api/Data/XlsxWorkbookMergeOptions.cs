using System;

namespace xlsx;

/// <summary>
/// Controls name conflict handling and dependent-content behavior for workbook merges.
/// </summary>
public class XlsxWorkbookMergeOptions
{
    /// <summary>Controls worksheet-name conflicts.</summary>
    public XlsxWorkbookMergeConflictPolicy SheetNameConflictPolicy;
    /// <summary>Controls defined-name conflicts.</summary>
    public XlsxWorkbookMergeConflictPolicy DefinedNameConflictPolicy;
    /// <summary>Controls table-name conflicts.</summary>
    public XlsxWorkbookMergeConflictPolicy TableNameConflictPolicy;
    /// <summary>Indicates whether applicable defined names are copied.</summary>
    public bool CopyDefinedNames;
    /// <summary>Indicates whether relationships and their dependent package parts are copied.</summary>
    public bool CopyDependentParts;
    /// <summary>Indicates whether formulas are updated for renamed incoming sheets and tables.</summary>
    public bool UpdateFormulaReferences;

    /// <summary>Initializes merge options with safe deterministic renaming and complete dependency copying.</summary>
    public XlsxWorkbookMergeOptions()
    {
        SheetNameConflictPolicy = XlsxWorkbookMergeConflictPolicy.RenameIncoming;
        DefinedNameConflictPolicy = XlsxWorkbookMergeConflictPolicy.RenameIncoming;
        TableNameConflictPolicy = XlsxWorkbookMergeConflictPolicy.RenameIncoming;
        CopyDefinedNames = true;
        CopyDependentParts = true;
        UpdateFormulaReferences = true;
    }
}
