using System.Collections.Generic;

namespace iso20022;

/// <summary>Reports one located XML signature, its algorithms, references, and certificate trust.</summary>
public class XmlSignatureResult
{
    /// <summary>The signature identifier when supplied.</summary>
    public string Identifier = string.Empty;
    /// <summary>The located signature path.</summary>
    public XmlPathData Path = new XmlPathData();
    /// <summary>The canonicalization algorithm URI.</summary>
    public string CanonicalizationAlgorithm = string.Empty;
    /// <summary>The signature algorithm URI.</summary>
    public string SignatureAlgorithm = string.Empty;
    /// <summary>The aggregate signature validation outcome.</summary>
    public SignatureValidationStatus Status;
    /// <summary>The certificate trust outcome.</summary>
    public CertificateTrustStatus CertificateStatus;
    /// <summary>The certificate subject or stable key identifier when reporting is permitted.</summary>
    public string CertificateIdentifier = string.Empty;
    /// <summary>Whether the certificate identifier was intentionally redacted.</summary>
    public bool CertificateIdentifierRedacted;
    /// <summary>The signature references in source order.</summary>
    public List<SignatureReferenceResult> References = new List<SignatureReferenceResult>();
}
