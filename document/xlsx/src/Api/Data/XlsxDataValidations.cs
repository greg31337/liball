using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents data validations data used by the public workbook API.
/// </summary>
public class XlsxDataValidations
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
    /// Stores the number of  items processed by the operation.
    /// </summary>
    public uint Count;
    /// <summary>
    /// Indicates whether count is present and should be written.
    /// </summary>
    public bool HasCount;
    /// <summary>
    /// Contains disable prompts.
    /// </summary>
    public bool DisablePrompts;
    /// <summary>
    /// Indicates whether disable prompts is present and should be written.
    /// </summary>
    public bool HasDisablePrompts;
    /// <summary>
    /// Stores the x window value used by this model.
    /// </summary>
    public uint XWindow;
    /// <summary>
    /// Indicates whether x window is present and should be written.
    /// </summary>
    public bool HasXWindow;
    /// <summary>
    /// Stores the y window value used by this model.
    /// </summary>
    public uint YWindow;
    /// <summary>
    /// Indicates whether y window is present and should be written.
    /// </summary>
    public bool HasYWindow;
    /// <summary>
    /// Contains items.
    /// </summary>
    public List<XlsxDataValidation> Items;
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
    /// Initializes a new data validations with its default state.
    /// </summary>
    public XlsxDataValidations()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Count = 0;
        HasCount = false;
        DisablePrompts = false;
        HasDisablePrompts = false;
        XWindow = 0;
        HasXWindow = false;
        YWindow = 0;
        HasYWindow = false;
        Items = new List<XlsxDataValidation>();
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
