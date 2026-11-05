using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace iso20022;

/// <summary>Requests deterministic XML signing with caller-owned key material and explicit algorithms.</summary>
public class MessageSigningRequest
{
    /// <summary>The exact raw XML bytes to sign.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The caller-owned asymmetric signing key.</summary>
    public AsymmetricAlgorithm? SigningKey;
    /// <summary>The signing certificate included or referenced by the signature when supplied.</summary>
    public X509Certificate2? Certificate;
    /// <summary>The deterministic path at which the signature is inserted.</summary>
    public XmlPathData InsertionPath = new XmlPathData();
    /// <summary>The contained message paths signed by references.</summary>
    public List<XmlPathData> ReferencePaths = new List<XmlPathData>();
    /// <summary>The canonicalization algorithm URI, or an empty value for a secure built-in default.</summary>
    public string CanonicalizationAlgorithm = string.Empty;
    /// <summary>The digest algorithm URI, or an empty value for a secure built-in default.</summary>
    public string DigestAlgorithm = string.Empty;
    /// <summary>The signature algorithm URI, or an empty value for a secure built-in default compatible with the key.</summary>
    public string SignatureAlgorithm = string.Empty;
    /// <summary>The optional key name written into signature key information.</summary>
    public string KeyName = string.Empty;
    /// <summary>Whether the supplied certificate is included in signature key information.</summary>
    public bool IncludeCertificate = true;
    /// <summary>Whether the generated signature is verified before returning output.</summary>
    public bool VerifyAfterSigning = true;
    /// <summary>The resource bounds applied while parsing, signing, and verifying.</summary>
    public ResourceLimits Limits = new ResourceLimits();
}
