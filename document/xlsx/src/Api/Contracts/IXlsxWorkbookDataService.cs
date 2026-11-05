using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Defines workbook-level projection, materialization, template, inventory, and dependency queries.
/// </summary>
public interface IXlsxWorkbookDataService
{
    /// <summary>Projects a workbook range to typed row arrays.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="sheetName">The worksheet name.</param><param name="reference">The A1 range to project.</param>
    /// <returns>The materialized range.</returns>
    XlsxWorkbookRangeMaterialization ProjectRangeRows(XlsxWorkbook workbook, string sheetName, string reference);
    /// <summary>Detects whether the first populated row of a range is likely a header row.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="sheetName">The worksheet name.</param><param name="reference">The A1 range to inspect.</param>
    /// <returns>The header detection result.</returns>
    XlsxHeaderRowDetectionResult DetectHeaderRow(XlsxWorkbook workbook, string sheetName, string reference);
    /// <summary>Materializes sheet-qualified selected ranges in request order.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="selectedRanges">The sheet-qualified or first-sheet-relative A1 ranges.</param>
    /// <returns>The selected range materializations.</returns>
    XlsxSelectedRangesMaterializationResult MaterializeSelectedRanges(XlsxWorkbook workbook, List<string> selectedRanges);
    /// <summary>Replaces literal template tokens in text cells and optionally formulas.</summary>
    /// <param name="workbook">The workbook to update.</param><param name="replacements">The literal token replacements.</param><param name="includeFormulas">Whether formula text is also updated.</param>
    /// <returns>The replacement counts and diagnostics.</returns>
    XlsxTemplateReplacementResult ReplaceTemplateTokens(XlsxWorkbook workbook, List<XlsxTemplateReplacement> replacements, bool includeFormulas);
    /// <summary>Inventories external links, connections, query tables, relationships, formula references, and hyperlinks.</summary>
    /// <param name="workbook">The workbook to inspect.</param>
    /// <returns>The external-content inventory.</returns>
    XlsxExternalContentInventoryResult InventoryExternalContent(XlsxWorkbook workbook);
    /// <summary>Queries direct formula dependencies for one sheet-qualified cell.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="qualifiedReference">The sheet-qualified A1 cell reference.</param>
    /// <returns>The dependency details.</returns>
    XlsxFormulaDependencyQueryResult QueryFormulaDependencies(XlsxWorkbook workbook, string qualifiedReference);
    /// <summary>Queries direct and transitive formula impacts from changed cells.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="changedReferences">The sheet-qualified changed cells.</param>
    /// <returns>The impacted cells.</returns>
    XlsxImpactedCellsResult QueryImpactedCells(XlsxWorkbook workbook, List<string> changedReferences);
}
