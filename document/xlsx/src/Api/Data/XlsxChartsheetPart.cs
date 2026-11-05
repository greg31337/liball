using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML chartsheet part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxChartsheetPart
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
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the drawing value used by this model.
    /// </summary>
    public XlsxSheetDrawingReference Drawing;
    /// <summary>
    /// Indicates whether drawing is present and should be written.
    /// </summary>
    public bool HasDrawing;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new chartsheet part with its default state.
    /// </summary>
    public XlsxChartsheetPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/chartsheets/sheet1.xml";
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Drawing = new XlsxSheetDrawingReference();
        HasDrawing = false;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Performs the resolve drawing relationship operation using the current model state.
    /// </summary>
    /// <param name = "relationships">The relationships value used by the operation.</param>
    /// <returns>A result containing the produced data, validity flag, and diagnostics.</returns>
    internal XlsxSheetDrawingRelationshipResult ResolveDrawingRelationship(XlsxRelationshipsPart relationships)
    {
        XlsxSheetDrawingRelationshipResult result = new XlsxSheetDrawingRelationshipResult();
        if (!HasDrawing)
        {
            return result;
        }

        if (Drawing == null)
        {
            result.AddError("chartsheet.drawingNull", "Chartsheet drawing reference must not be null when HasDrawing is true.");
            return result;
        }

        result.Drawing = Drawing;
        result.HasDrawing = true;
        XlsxWorksheetPart.ResolveDrawingRelationship(result, Drawing, relationships, "chartsheet");
        return result;
    }
}
