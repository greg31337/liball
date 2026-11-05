using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME signer verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeSignerVerificationResult : IEmailSmimeSignerVerificationResultContract
{
    EmailSmimeSignerVerificationStatus status;
    string reason;
    EmailSmimeDigestAlgorithm digestAlgorithm;
    EmailSmimeSignatureAlgorithm signatureAlgorithm;
    EmailSmimeCertificateInfo? certificate;
    EmailSmimeChainResult? chainResult;
    bool contentDigestValid;
    bool signatureValid;
    /// <summary>
    /// Creates a new S/MIME signer verification result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <param name = "digestAlgorithm">The digest algorithm used by the operation.</param>
    /// <param name = "signatureAlgorithm">The signature algorithm used by the operation.</param>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <param name = "chainResult">The chain result used by the operation.</param>
    /// <param name = "contentDigestValid">Whether to enable the content digest valid behavior.</param>
    /// <param name = "signatureValid">Whether to enable the signature valid behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeSignerVerificationResult(EmailSmimeSignerVerificationStatus status, string reason, EmailSmimeDigestAlgorithm digestAlgorithm, EmailSmimeSignatureAlgorithm signatureAlgorithm, EmailSmimeCertificateInfo? certificate, EmailSmimeChainResult? chainResult, bool contentDigestValid, bool signatureValid)
    {
        if (status < EmailSmimeSignerVerificationStatus.Pass || status > EmailSmimeSignerVerificationStatus.PolicyRejected)
            throw new ArgumentOutOfRangeException("status");
        if (reason == null)
            throw new ArgumentNullException("reason");
        this.status = status;
        this.reason = reason;
        this.digestAlgorithm = digestAlgorithm;
        this.signatureAlgorithm = signatureAlgorithm;
        this.certificate = certificate;
        this.chainResult = chainResult;
        this.contentDigestValid = contentDigestValid;
        this.signatureValid = signatureValid;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailSmimeSignerVerificationStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailSmimeSignerVerificationStatus.Pass;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public string GetReason()
    {
        return reason;
    }

    /// <summary>
    /// Gets the digest algorithm.
    /// </summary>
    /// <returns>The current digest algorithm.</returns>
    public EmailSmimeDigestAlgorithm GetDigestAlgorithm()
    {
        return digestAlgorithm;
    }

    /// <summary>
    /// Gets the signature algorithm.
    /// </summary>
    /// <returns>The current signature algorithm.</returns>
    public EmailSmimeSignatureAlgorithm GetSignatureAlgorithm()
    {
        return signatureAlgorithm;
    }

    /// <summary>
    /// Determines whether the certificate is present or available.
    /// </summary>
    /// <returns><c>true</c> when the certificate is available; otherwise, <c>false</c>.</returns>
    public bool HasCertificate()
    {
        return certificate != null;
    }

    /// <summary>
    /// Gets the certificate.
    /// </summary>
    /// <returns>The current certificate.</returns>
    public EmailSmimeCertificateInfo? GetCertificate()
    {
        return certificate;
    }

    /// <summary>
    /// Determines whether the chain result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the chain result is available; otherwise, <c>false</c>.</returns>
    public bool HasChainResult()
    {
        return chainResult != null;
    }

    /// <summary>
    /// Gets the chain result.
    /// </summary>
    /// <returns>The current chain result.</returns>
    public EmailSmimeChainResult? GetChainResult()
    {
        return chainResult;
    }

    /// <summary>
    /// Determines whether the current value or state is content digest valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is content digest valid; otherwise, <c>false</c>.</returns>
    public bool IsContentDigestValid()
    {
        return contentDigestValid;
    }

    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    public bool IsSignatureValid()
    {
        return signatureValid;
    }
}
