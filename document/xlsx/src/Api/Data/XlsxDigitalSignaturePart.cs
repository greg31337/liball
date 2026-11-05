using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML digital signature part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxDigitalSignaturePart
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
    /// Stores the content type value used by this model.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the data value used by this model.
    /// </summary>
    public byte[] Data;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the root element name value used by this model.
    /// </summary>
    public string RootElementName;
    /// <summary>
    /// Stores the root namespace uri value used by this model.
    /// </summary>
    public string RootNamespaceUri;
    /// <summary>
    /// Indicates whether origin part.
    /// </summary>
    public bool IsOriginPart;
    /// <summary>
    /// Indicates whether signature xml part.
    /// </summary>
    public bool IsSignatureXmlPart;
    /// <summary>
    /// Initializes a new digital signature part with its default state.
    /// </summary>
    public XlsxDigitalSignaturePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "";
        ContentType = "";
        Data = new byte[0];
        RawXml = "";
        RootElementName = "";
        RootNamespaceUri = "";
        IsOriginPart = false;
        IsSignatureXmlPart = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
