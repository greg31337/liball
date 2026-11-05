using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains independent package signature, reference, certificate, and trust validation outcomes.
/// </summary>
public class XlsxSignatureValidationResult
{
    /// <summary>
    /// Indicates whether validation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced by the operation.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains the validated package signatures.
    /// </summary>
    public List<XlsxPackageSignature> Signatures;
    /// <summary>
    /// Indicates whether at least one package signature was found.
    /// </summary>
    public bool HasSignatures;
    /// <summary>
    /// Indicates whether every cryptographic signature value is valid.
    /// </summary>
    public bool AllCryptographicSignaturesValid;
    /// <summary>
    /// Indicates whether every signed part reference is valid.
    /// </summary>
    public bool AllReferencesValid;
    /// <summary>
    /// Indicates whether every signing certificate is currently valid for digital signing.
    /// </summary>
    public bool AllCertificatesValid;
    /// <summary>
    /// Indicates whether certificate trust was evaluated.
    /// </summary>
    public bool TrustEvaluated;
    /// <summary>
    /// Indicates whether every certificate chain terminates in a trusted root.
    /// </summary>
    public bool AllCertificatesTrusted;
    /// <summary>
    /// Initializes a new signature validation result with its default state.
    /// </summary>
    public XlsxSignatureValidationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Signatures = new List<XlsxPackageSignature>();
        HasSignatures = false;
        AllCryptographicSignaturesValid = false;
        AllReferencesValid = false;
        AllCertificatesValid = false;
        TrustEvaluated = false;
        AllCertificatesTrusted = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
