using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents extension list data used by the public workbook API.
/// </summary>
public class XlsxExtensionList
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
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the prefix value used by this model.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// Contains extensions.
    /// </summary>
    public List<XlsxExtension> Extensions;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Initializes a new extension list with its default state.
    /// </summary>
    public XlsxExtensionList()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        RawXml = "";
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        Prefix = "";
        Extensions = new List<XlsxExtension>();
        UnknownXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
