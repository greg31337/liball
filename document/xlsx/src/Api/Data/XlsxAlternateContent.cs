using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents alternate content data used by the public workbook API.
/// </summary>
public class XlsxAlternateContent
{
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the number of choice items processed by the operation.
    /// </summary>
    public int ChoiceCount;
    /// <summary>
    /// Stores the number of fallback items processed by the operation.
    /// </summary>
    public int FallbackCount;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Initializes a new alternate content with its default state.
    /// </summary>
    public XlsxAlternateContent()
    {
        RawXml = "";
        ChoiceCount = 0;
        FallbackCount = 0;
        Validation = new XlsxValidationResult();
        IsValid = true;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
