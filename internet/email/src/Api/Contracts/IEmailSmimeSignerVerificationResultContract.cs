using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeSignerVerificationResult"/>.
/// </summary>
public interface IEmailSmimeSignerVerificationResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailSmimeSignerVerificationStatus GetStatus();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
    /// <summary>
    /// Gets the digest algorithm.
    /// </summary>
    /// <returns>The current digest algorithm.</returns>
    EmailSmimeDigestAlgorithm GetDigestAlgorithm();
    /// <summary>
    /// Gets the signature algorithm.
    /// </summary>
    /// <returns>The current signature algorithm.</returns>
    EmailSmimeSignatureAlgorithm GetSignatureAlgorithm();
    /// <summary>
    /// Determines whether the certificate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the certificate is available; otherwise, <c>false</c>.</returns>
    bool HasCertificate();
    /// <summary>
    /// Gets the certificate.
    /// </summary>
    /// <returns>The current certificate.</returns>
    EmailSmimeCertificateInfo? GetCertificate();
    /// <summary>
    /// Determines whether the chain result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the chain result is available; otherwise, <c>false</c>.</returns>
    bool HasChainResult();
    /// <summary>
    /// Gets the chain result.
    /// </summary>
    /// <returns>The current chain result.</returns>
    EmailSmimeChainResult? GetChainResult();
    /// <summary>
    /// Determines whether the current value or state is content digest valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is content digest valid; otherwise, <c>false</c>.</returns>
    bool IsContentDigestValid();
    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    bool IsSignatureValid();
}
