using System;

namespace iso20022;

/// <summary>Requests bounded XML signature discovery and verification under explicit trust policy.</summary>
public class SignatureValidationRequest
{
    /// <summary>The exact raw XML bytes whose signatures are validated.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The caller-supplied certificate, algorithm, and reference trust policy.</summary>
    public SignatureTrustPolicy TrustPolicy = new SignatureTrustPolicy();
    /// <summary>The resource bounds applied while parsing and verifying signatures.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether absence of an XML signature is invalid.</summary>
    public bool RequireSignature;
    /// <summary>Whether every discovered signature is validated instead of the first applicable signature.</summary>
    public bool ValidateAllSignatures = true;
    /// <summary>Whether certificate subject and key identifiers are omitted from diagnostics.</summary>
    public bool RedactCertificateIdentifiers = true;
}
