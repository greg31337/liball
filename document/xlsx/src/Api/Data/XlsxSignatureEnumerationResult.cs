using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains package signatures and diagnostics produced while enumerating them.
/// </summary>
public class XlsxSignatureEnumerationResult
{
    /// <summary>
    /// Indicates whether enumeration completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced by the operation.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains the enumerated package signatures.
    /// </summary>
    public List<XlsxPackageSignature> Signatures;
    /// <summary>
    /// Initializes a new signature enumeration result with its default state.
    /// </summary>
    public XlsxSignatureEnumerationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Signatures = new List<XlsxPackageSignature>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
