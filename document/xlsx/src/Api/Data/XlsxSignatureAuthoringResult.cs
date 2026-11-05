using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the signature produced by a package signature authoring operation.
/// </summary>
public class XlsxSignatureAuthoringResult
{
    /// <summary>
    /// Indicates whether authoring completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced by the operation.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the authored signature model when authoring succeeds.
    /// </summary>
    public XlsxPackageSignature? Signature;
    /// <summary>
    /// Initializes a new signature authoring result with its default state.
    /// </summary>
    public XlsxSignatureAuthoringResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Signature = null;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
