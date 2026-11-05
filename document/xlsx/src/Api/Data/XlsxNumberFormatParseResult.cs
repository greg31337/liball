using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a number format parse operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxNumberFormatParseResult
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
    /// Stores the format code value used by this model.
    /// </summary>
    public string FormatCode;
    /// <summary>
    /// Stores the built in id value used by this model.
    /// </summary>
    public uint BuiltInId;
    /// <summary>
    /// Indicates whether built in.
    /// </summary>
    public bool IsBuiltIn;
    /// <summary>
    /// Contains sections.
    /// </summary>
    public List<XlsxNumberFormatSection> Sections;
    /// <summary>
    /// Initializes a new number format parse result with its default state.
    /// </summary>
    public XlsxNumberFormatParseResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        FormatCode = "";
        BuiltInId = 0;
        IsBuiltIn = false;
        Sections = new List<XlsxNumberFormatSection>();
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
