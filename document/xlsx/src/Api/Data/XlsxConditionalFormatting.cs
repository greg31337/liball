using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional formatting data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatting
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
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the sqref value used by this model.
    /// </summary>
    public string Sqref;
    /// <summary>
    /// Stores the pivot value used by this model.
    /// </summary>
    public bool Pivot;
    /// <summary>
    /// Indicates whether pivot is present and should be written.
    /// </summary>
    public bool HasPivot;
    /// <summary>
    /// Contains rules.
    /// </summary>
    public List<XlsxConditionalFormatRule> Rules;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new conditional formatting with its default state.
    /// </summary>
    public XlsxConditionalFormatting()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Sqref = "";
        Pivot = false;
        HasPivot = false;
        Rules = new List<XlsxConditionalFormatRule>();
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
