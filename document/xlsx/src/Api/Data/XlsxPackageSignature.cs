using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents one typed OPC package signature and its signed part references.
/// </summary>
public class XlsxPackageSignature
{
    /// <summary>
    /// Indicates whether the model was parsed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while parsing or validating the signature.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized signature part path.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the XML signature identifier.
    /// </summary>
    public string? Id;
    /// <summary>
    /// Stores the canonicalization method URI.
    /// </summary>
    public string CanonicalizationMethod;
    /// <summary>
    /// Stores the signature method URI.
    /// </summary>
    public string SignatureMethod;
    /// <summary>
    /// Stores the recognized signature algorithm.
    /// </summary>
    public XlsxSignatureAlgorithm Algorithm;
    /// <summary>
    /// Contains signed package part references.
    /// </summary>
    public List<XlsxSignatureReference> SignedParts;
    /// <summary>
    /// Contains the signature value bytes.
    /// </summary>
    public byte[] SignatureValue;
    /// <summary>
    /// Contains the embedded X.509 certificate bytes.
    /// </summary>
    public byte[] CertificateData;
    /// <summary>
    /// Stores the certificate subject name.
    /// </summary>
    public string CertificateSubject;
    /// <summary>
    /// Stores the certificate issuer name.
    /// </summary>
    public string CertificateIssuer;
    /// <summary>
    /// Stores the certificate thumbprint.
    /// </summary>
    public string CertificateThumbprint;
    /// <summary>
    /// Indicates whether the cryptographic signature over SignedInfo is valid.
    /// </summary>
    public bool CryptographicSignatureValid;
    /// <summary>
    /// Indicates whether every signed reference is present and valid.
    /// </summary>
    public bool ReferencesValid;
    /// <summary>
    /// Indicates whether an embedded certificate is present and valid for signing at the current time.
    /// </summary>
    public bool CertificateValid;
    /// <summary>
    /// Indicates whether certificate trust was evaluated.
    /// </summary>
    public bool TrustEvaluated;
    /// <summary>
    /// Indicates whether the certificate chain terminates in a trusted root.
    /// </summary>
    public bool CertificateTrusted;
    /// <summary>
    /// Initializes a new package signature with its default state.
    /// </summary>
    public XlsxPackageSignature()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "";
        Id = "";
        CanonicalizationMethod = "";
        SignatureMethod = "";
        Algorithm = XlsxSignatureAlgorithm.Unknown;
        SignedParts = new List<XlsxSignatureReference>();
        SignatureValue = new byte[0];
        CertificateData = new byte[0];
        CertificateSubject = "";
        CertificateIssuer = "";
        CertificateThumbprint = "";
        CryptographicSignatureValid = false;
        ReferencesValid = false;
        CertificateValid = false;
        TrustEvaluated = false;
        CertificateTrusted = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
