using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML workbook connections part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxWorkbookConnectionsPart
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
    /// Stores the number of  items processed by the operation.
    /// </summary>
    public uint Count;
    /// <summary>
    /// Indicates whether count is present and should be written.
    /// </summary>
    public bool HasCount;
    /// <summary>
    /// Contains connections.
    /// </summary>
    public List<XlsxWorkbookConnectionInfo> Connections;
    /// <summary>
    /// Initializes a new workbook connections part with its default state.
    /// </summary>
    public XlsxWorkbookConnectionsPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.Connections;
        RawXml = "";
        Count = 0;
        HasCount = false;
        Connections = new List<XlsxWorkbookConnectionInfo>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
