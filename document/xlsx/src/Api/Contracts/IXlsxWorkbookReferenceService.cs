using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines workbook-aware parsing and resolution operations for formula references and names.
/// </summary>
public interface IXlsxWorkbookReferenceService
{
    /// <summary>
    /// Parses a quoted, sheet-qualified, three-dimensional, or workbook-qualified A1 reference.
    /// </summary>
    /// <param name="text">The reference text to parse.</param>
    /// <returns>The parsed reference and validation diagnostics.</returns>
    XlsxReferenceResolutionResult ParseA1Reference(string text);
    /// <summary>
    /// Parses an optionally qualified R1C1 reference and resolves relative coordinates from a base cell.
    /// </summary>
    /// <param name="text">The reference text to parse.</param>
    /// <param name="baseRow">The one-based base row for relative coordinates.</param>
    /// <param name="baseColumn">The one-based base column for relative coordinates.</param>
    /// <returns>The parsed reference and resolved A1 coordinates.</returns>
    XlsxReferenceResolutionResult ParseR1C1Reference(string text, int baseRow, int baseColumn);
    /// <summary>
    /// Resolves a local or workbook-scoped defined name, with local scope taking precedence.
    /// </summary>
    /// <param name="workbook">The workbook containing the name.</param>
    /// <param name="name">The defined name, optionally qualified by a worksheet or workbook.</param>
    /// <param name="localSheetId">The zero-based local sheet scope used when the name is unqualified.</param>
    /// <returns>The resolved name and validation diagnostics.</returns>
    XlsxReferenceResolutionResult ResolveDefinedName(XlsxWorkbook workbook, string name, int localSheetId);
    /// <summary>
    /// Resolves the built-in print-area name for a worksheet scope.
    /// </summary>
    /// <param name="workbook">The workbook containing the print name.</param>
    /// <param name="localSheetId">The zero-based worksheet scope.</param>
    /// <returns>The resolved print name and validation diagnostics.</returns>
    XlsxReferenceResolutionResult ResolvePrintArea(XlsxWorkbook workbook, int localSheetId);
    /// <summary>
    /// Resolves the built-in print-titles name for a worksheet scope.
    /// </summary>
    /// <param name="workbook">The workbook containing the print name.</param>
    /// <param name="localSheetId">The zero-based worksheet scope.</param>
    /// <returns>The resolved print name and validation diagnostics.</returns>
    XlsxReferenceResolutionResult ResolvePrintTitles(XlsxWorkbook workbook, int localSheetId);
    /// <summary>
    /// Resolves a table structured reference across the workbook.
    /// </summary>
    /// <param name="workbook">The workbook containing the table.</param>
    /// <param name="text">The structured reference text.</param>
    /// <returns>The resolved table and A1 range.</returns>
    XlsxReferenceResolutionResult ResolveTableReference(XlsxWorkbook workbook, string text);
    /// <summary>
    /// Detects whether a name or reference contains an external-workbook qualifier.
    /// </summary>
    /// <param name="text">The name or reference text to inspect.</param>
    /// <returns><c>true</c> when an external-workbook qualifier is present; otherwise, <c>false</c>.</returns>
    bool IsExternalName(string text);
}
