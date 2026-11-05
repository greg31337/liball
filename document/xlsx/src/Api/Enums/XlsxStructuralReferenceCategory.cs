using System;

namespace xlsx;

/// <summary>
/// Identifies the workbook feature changed by a structural edit.
/// </summary>
public enum XlsxStructuralReferenceCategory
{
    /// <summary>
    /// Selects a worksheet formula.
    /// </summary>
    Formula = 1,
    /// <summary>
    /// Selects a workbook defined name.
    /// </summary>
    Name = 2,
    /// <summary>
    /// Selects a worksheet table.
    /// </summary>
    Table = 3,
    /// <summary>
    /// Selects a pivot table.
    /// </summary>
    Pivot = 4,
    /// <summary>
    /// Selects a data validation.
    /// </summary>
    Validation = 5,
    /// <summary>
    /// Selects conditional formatting.
    /// </summary>
    ConditionalFormatting = 6,
    /// <summary>
    /// Selects a hyperlink.
    /// </summary>
    Hyperlink = 7,
    /// <summary>
    /// Selects a comment.
    /// </summary>
    Comment = 8,
    /// <summary>
    /// Selects a drawing anchor.
    /// </summary>
    Drawing = 9,
    /// <summary>
    /// Selects a chart series formula.
    /// </summary>
    Chart = 10,
    /// <summary>
    /// Selects a worksheet page break.
    /// </summary>
    PageBreak = 11,
    /// <summary>
    /// Selects a print area or print titles definition.
    /// </summary>
    PrintDefinition = 12,
    /// <summary>
    /// Selects a package relationship.
    /// </summary>
    Relationship = 13,
    /// <summary>
    /// Selects a merged cell range.
    /// </summary>
    Merge = 14,
    /// <summary>
    /// Selects an AutoFilter or sort reference.
    /// </summary>
    Filter = 15,
    /// <summary>
    /// Selects sheet collection metadata.
    /// </summary>
    Sheet = 16
}
