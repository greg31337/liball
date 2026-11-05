namespace iso20022;

/// <summary>Identifies the certificate trust outcome reported for an XML signature.</summary>
public enum CertificateTrustStatus
{
    /// <summary>The certificate trust outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>No certificate was supplied by the signature or caller.</summary>
    NotPresent = 1,
    /// <summary>The certificate was present but trust evaluation was not requested.</summary>
    NotEvaluated = 2,
    /// <summary>The certificate satisfies the selected caller trust policy.</summary>
    Trusted = 3,
    /// <summary>The certificate does not chain to a selected trust anchor.</summary>
    Untrusted = 4,
    /// <summary>The certificate is not yet valid at the selected validation time.</summary>
    NotYetValid = 5,
    /// <summary>The certificate is expired at the selected validation time.</summary>
    Expired = 6,
    /// <summary>The certificate is revoked under the selected revocation policy.</summary>
    Revoked = 7,
    /// <summary>The certificate cannot be processed or uses rejected parameters.</summary>
    Invalid = 8
}
