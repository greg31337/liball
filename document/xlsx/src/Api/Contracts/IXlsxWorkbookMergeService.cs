using System;

namespace xlsx;

/// <summary>
/// Defines cross-workbook worksheet copy and complete workbook merge operations.
/// </summary>
public interface IXlsxWorkbookMergeService
{
    /// <summary>Copies one worksheet and its dependent content into another workbook.</summary>
    /// <param name="destination">The workbook receiving the worksheet.</param><param name="source">The source workbook.</param><param name="sheetName">The source worksheet name.</param><param name="options">Conflict and dependency options.</param>
    /// <returns>The complete merge mappings and diagnostics.</returns>
    XlsxWorkbookMergeResult CopyWorksheet(XlsxWorkbook destination, XlsxWorkbook source, string sheetName, XlsxWorkbookMergeOptions? options);
    /// <summary>Merges every source worksheet and applicable workbook-global content into another workbook.</summary>
    /// <param name="destination">The workbook receiving the content.</param><param name="source">The source workbook.</param><param name="options">Conflict and dependency options.</param>
    /// <returns>The complete merge mappings and diagnostics.</returns>
    XlsxWorkbookMergeResult MergeWorkbooks(XlsxWorkbook destination, XlsxWorkbook source, XlsxWorkbookMergeOptions? options);
}
