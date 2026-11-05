using System.Collections.Generic;

namespace iso20022;

/// <summary>Defines certificate, reference-containment, revocation, and algorithm trust policy for XML signatures.</summary>
public class SignatureTrustPolicy
{
    /// <summary>Caller-supplied DER certificate trust anchors.</summary>
    public List<byte[]> TrustedCertificates = new List<byte[]>();
    /// <summary>Allowed canonicalization algorithm URIs, or an empty list for secure built-in defaults.</summary>
    public List<string> AllowedCanonicalizationAlgorithms = new List<string>();
    /// <summary>Allowed digest algorithm URIs, or an empty list for secure built-in defaults.</summary>
    public List<string> AllowedDigestAlgorithms = new List<string>();
    /// <summary>Allowed signature algorithm URIs, or an empty list for secure built-in defaults.</summary>
    public List<string> AllowedSignatureAlgorithms = new List<string>();
    /// <summary>Whether platform trust anchors may be used in addition to caller anchors.</summary>
    public bool UseSystemTrustStore = true;
    /// <summary>Whether the signing certificate must satisfy the selected trust anchors.</summary>
    public bool RequireTrustedCertificate = true;
    /// <summary>Whether embedded certificates may supply signing identity material.</summary>
    public bool AllowEmbeddedCertificates = true;
    /// <summary>Whether certificate revocation is checked when supported.</summary>
    public bool CheckCertificateRevocation = true;
    /// <summary>Whether references outside the containing message are permitted.</summary>
    public bool AllowExternalReferences;
    /// <summary>The validation time used for certificate validity when supplied.</summary>
    public IsoDateTimeValue? ValidationTime;
}
