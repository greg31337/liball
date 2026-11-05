using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Defines worksheet-bound parsing and resolution operations for cell and table references.
/// </summary>
public interface IXlsxWorksheetReferenceService
{
    /// <summary>
    /// Parses an A1 reference and binds it to the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet represented by the reference.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The reference text to parse.</param>
    /// <returns>The parsed and worksheet-bound reference.</returns>
    XlsxReferenceResolutionResult ParseA1Reference(XlsxWorksheetPart worksheet, string sheetName, string text);
    /// <summary>
    /// Parses an R1C1 reference, resolves relative coordinates, and binds it to the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet represented by the reference.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The reference text to parse.</param>
    /// <param name="baseRow">The one-based base row for relative coordinates.</param>
    /// <param name="baseColumn">The one-based base column for relative coordinates.</param>
    /// <returns>The parsed and worksheet-bound reference.</returns>
    XlsxReferenceResolutionResult ParseR1C1Reference(XlsxWorksheetPart worksheet, string sheetName, string text, int baseRow, int baseColumn);
    /// <summary>
    /// Resolves a structured reference against tables on the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The structured reference text.</param>
    /// <returns>The resolved table and A1 range.</returns>
    XlsxReferenceResolutionResult ResolveTableReference(XlsxWorksheetPart worksheet, string sheetName, string text);
}
