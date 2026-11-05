using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML slicers part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxSlicersPart
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
    /// Contains slicers.
    /// </summary>
    public List<XlsxSlicerDefinition> Slicers;
    /// <summary>Stores preserved extension-list XML.</summary>
    public string ExtensionListRawXml;
    /// <summary>Contains unknown root attributes.</summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>Contains preserved child XML not represented by typed fields.</summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new slicers part with its default state.
    /// </summary>
    public XlsxSlicersPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/slicers/slicer1.xml";
        RawXml = "";
        Slicers = new List<XlsxSlicerDefinition>();
        ExtensionListRawXml = "";
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
