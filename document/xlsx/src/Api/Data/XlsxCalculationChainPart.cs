using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML calculation chain part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxCalculationChainPart
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
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains cells.
    /// </summary>
    public List<XlsxCalculationChainCell> Cells;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new calculation chain part with its default state.
    /// </summary>
    public XlsxCalculationChainPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.CalculationChain;
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        Cells = new List<XlsxCalculationChainCell>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
