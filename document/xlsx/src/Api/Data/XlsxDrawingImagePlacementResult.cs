using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a drawing image placement operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxDrawingImagePlacementResult
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
    /// Stores the drawing part value used by this model.
    /// </summary>
    public XlsxDrawingPart? DrawingPart;
    /// <summary>
    /// Indicates whether drawing part is present and should be written.
    /// </summary>
    public bool HasDrawingPart;
    /// <summary>
    /// Stores the anchor value used by this model.
    /// </summary>
    public XlsxDrawingImageAnchor? Anchor;
    /// <summary>
    /// Indicates whether anchor is present and should be written.
    /// </summary>
    public bool HasAnchor;
    /// <summary>
    /// Stores the image part value used by this model.
    /// </summary>
    public XlsxImagePart? ImagePart;
    /// <summary>
    /// Indicates whether image part is present and should be written.
    /// </summary>
    public bool HasImagePart;
    /// <summary>
    /// Contains worksheet relationships.
    /// </summary>
    public XlsxRelationshipsPart? WorksheetRelationships;
    /// <summary>
    /// Indicates whether worksheet relationships is present and should be written.
    /// </summary>
    public bool HasWorksheetRelationships;
    /// <summary>
    /// Contains drawing relationships.
    /// </summary>
    public XlsxRelationshipsPart? DrawingRelationships;
    /// <summary>
    /// Indicates whether drawing relationships is present and should be written.
    /// </summary>
    public bool HasDrawingRelationships;
    /// <summary>
    /// Initializes a new drawing image placement result with its default state.
    /// </summary>
    public XlsxDrawingImagePlacementResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        DrawingPart = null;
        HasDrawingPart = false;
        Anchor = null;
        HasAnchor = false;
        ImagePart = null;
        HasImagePart = false;
        WorksheetRelationships = null;
        HasWorksheetRelationships = false;
        DrawingRelationships = null;
        HasDrawingRelationships = false;
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
