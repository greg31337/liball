using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides worksheet-bound parsing and resolution for cell and table references.
/// </summary>
public class XlsxWorksheetReferenceService : IXlsxWorksheetReferenceService
{
    /// <summary>
    /// Initializes a worksheet reference service.
    /// </summary>
    public XlsxWorksheetReferenceService()
    {
    }

    /// <summary>
    /// Parses an A1 reference and binds it to the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet represented by the reference.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The reference text to parse.</param>
    /// <returns>The parsed and worksheet-bound reference.</returns>
    public XlsxReferenceResolutionResult ParseA1Reference(XlsxWorksheetPart worksheet, string sheetName, string text)
    {
        XlsxReferenceResolutionResult result = XlsxWorkbookReferenceService.ParseA1Core(text);
        BindWorksheet(result, worksheet, sheetName);
        return result;
    }

    /// <summary>
    /// Parses an R1C1 reference, resolves relative coordinates, and binds it to the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet represented by the reference.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The reference text to parse.</param>
    /// <param name="baseRow">The one-based base row for relative coordinates.</param>
    /// <param name="baseColumn">The one-based base column for relative coordinates.</param>
    /// <returns>The parsed and worksheet-bound reference.</returns>
    public XlsxReferenceResolutionResult ParseR1C1Reference(XlsxWorksheetPart worksheet, string sheetName, string text, int baseRow, int baseColumn)
    {
        XlsxReferenceResolutionResult result = new XlsxWorkbookReferenceService().ParseR1C1Reference(text, baseRow, baseColumn);
        BindWorksheet(result, worksheet, sheetName);
        return result;
    }

    /// <summary>
    /// Resolves a structured reference against tables on the supplied worksheet.
    /// </summary>
    /// <param name="worksheet">The worksheet containing the table.</param>
    /// <param name="sheetName">The worksheet display name.</param>
    /// <param name="text">The structured reference text.</param>
    /// <returns>The resolved table and A1 range.</returns>
    public XlsxReferenceResolutionResult ResolveTableReference(XlsxWorksheetPart worksheet, string sheetName, string text)
    {
        XlsxReferenceResolutionResult result = XlsxWorkbookReferenceService.ParseStructuredReference(text);
        if (result.IsValid && !result.IsExternal)
        {
            if (result.HasSheetName && !string.Equals(result.FirstSheetName, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("reference.sheetMismatch", "Reference worksheet qualifier does not match the supplied worksheet.");
            }
            else
            {
                XlsxWorkbookReferenceService.ResolveStructuredReference(result, worksheet, sheetName);
            }
        }
        return result;
    }

    internal static void BindWorksheet(XlsxReferenceResolutionResult result, XlsxWorksheetPart worksheet, string sheetName)
    {
        if (!result.IsValid)
        {
            return;
        }
        if (worksheet == null)
        {
            result.AddError("reference.worksheetMissing", "Worksheet is required for worksheet reference resolution.");
            return;
        }
        if (sheetName == null || sheetName.Length == 0)
        {
            result.AddError("reference.sheetNameMissing", "Worksheet display name is required for worksheet reference resolution.");
            return;
        }
        if (result.HasSheetName && !result.IsThreeDimensional && !string.Equals(result.FirstSheetName, sheetName, StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("reference.sheetMismatch", "Reference worksheet qualifier does not match the supplied worksheet.");
            return;
        }
        if (!result.HasSheetName)
        {
            result.FirstSheetName = sheetName;
            result.LastSheetName = sheetName;
            result.HasSheetName = true;
            if (result.HasA1Range)
            {
                result.NormalizedText = XlsxWorkbookReferenceService.BuildQualifier(result) + result.A1Range.Text;
            }
        }
        result.Worksheet = worksheet;
        result.HasWorksheet = true;
    }
}
