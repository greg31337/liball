using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Specifies the supported DKIM key source values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailDkimKeySourceKind
{
    /// <summary>
    /// Represents the dns record choice or state.
    /// </summary>
    DnsRecord = 0,
    /// <summary>
    /// Represents the subject public key info choice or state.
    /// </summary>
    SubjectPublicKeyInfo = 1,
    /// <summary>
    /// Represents the pem subject public key info choice or state.
    /// </summary>
    PemSubjectPublicKeyInfo = 2,
    /// <summary>
    /// Represents the RSA public key choice or state.
    /// </summary>
    RsaPublicKey = 3,
    /// <summary>
    /// Represents the pem RSA public key choice or state.
    /// </summary>
    PemRsaPublicKey = 4
}
