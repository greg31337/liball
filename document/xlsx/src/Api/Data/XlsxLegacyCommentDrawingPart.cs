using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML legacy comment drawing part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxLegacyCommentDrawingPart
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
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains drawings.
    /// </summary>
    public List<XlsxLegacyCommentDrawing> Drawings;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new legacy comment drawing part with its default state.
    /// </summary>
    public XlsxLegacyCommentDrawingPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/drawings/vmlDrawing1.vml";
        RawXml = "";
        Drawings = new List<XlsxLegacyCommentDrawing>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
