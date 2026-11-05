using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML query table part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxQueryTablePart
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
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the connection id value used by this model.
    /// </summary>
    public uint ConnectionId;
    /// <summary>
    /// Indicates whether connection id is present and should be written.
    /// </summary>
    public bool HasConnectionId;
    /// <summary>
    /// Stores the number of query table field items processed by the operation.
    /// </summary>
    public uint QueryTableFieldCount;
    /// <summary>
    /// Indicates whether query table field count is present and should be written.
    /// </summary>
    public bool HasQueryTableFieldCount;
    /// <summary>
    /// Contains fields.
    /// </summary>
    public List<XlsxQueryTableField> Fields;
    /// <summary>
    /// Initializes a new query table part with its default state.
    /// </summary>
    public XlsxQueryTablePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.QueryTablesDirectory + "queryTable1.xml";
        RawXml = "";
        Name = "";
        ConnectionId = 0;
        HasConnectionId = false;
        QueryTableFieldCount = 0;
        HasQueryTableFieldCount = false;
        Fields = new List<XlsxQueryTableField>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
