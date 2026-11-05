using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM signing attempt operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimSigningAttemptResult : IEmailDkimSigningAttemptResultContract
{
    EmailDkimSigningAttemptStatus status;
    EmailDkimAlgorithmKind algorithmKind;
    EmailDkimSigningResult? signingResult;
    string reason;
    /// <summary>
    /// Creates a new DKIM signing attempt result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "algorithmKind">The algorithm kind used by the operation.</param>
    /// <param name = "signingResult">The signing result used by the operation.</param>
    /// <param name = "reason">The reason used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimSigningAttemptResult(EmailDkimSigningAttemptStatus status, EmailDkimAlgorithmKind algorithmKind, EmailDkimSigningResult? signingResult, string reason)
    {
        if (status < EmailDkimSigningAttemptStatus.Success || status > EmailDkimSigningAttemptStatus.ProhibitedAlgorithm)
            throw new ArgumentOutOfRangeException("status");
        if (algorithmKind != EmailDkimAlgorithmKind.RsaSha1 && algorithmKind != EmailDkimAlgorithmKind.RsaSha256 && algorithmKind != EmailDkimAlgorithmKind.Ed25519Sha256)
            throw new ArgumentOutOfRangeException("algorithmKind");
        if (algorithmKind == EmailDkimAlgorithmKind.RsaSha1 && status != EmailDkimSigningAttemptStatus.ProhibitedAlgorithm || status == EmailDkimSigningAttemptStatus.ProhibitedAlgorithm && algorithmKind != EmailDkimAlgorithmKind.RsaSha1)
            throw new ArgumentException("RSA-SHA1 signing attempts must be represented as prohibited.", "status");
        if (reason == null)
            throw new ArgumentNullException("reason");
        if (status == EmailDkimSigningAttemptStatus.Success && signingResult == null)
            throw new ArgumentException("A successful signing attempt requires a signing result.", "signingResult");
        if (status != EmailDkimSigningAttemptStatus.Success && signingResult != null)
            throw new ArgumentException("An unsuccessful signing attempt cannot contain a signing result.", "signingResult");
        if (status != EmailDkimSigningAttemptStatus.Success && reason.Length == 0)
            throw new ArgumentException("An unsuccessful signing attempt requires an explanatory reason.", "reason");
        this.status = status;
        this.algorithmKind = algorithmKind;
        this.signingResult = signingResult;
        this.reason = reason;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailDkimSigningAttemptStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    public EmailDkimAlgorithmKind GetAlgorithmKind()
    {
        return algorithmKind;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailDkimSigningAttemptStatus.Success;
    }

    /// <summary>
    /// Determines whether the current value or state is unsupported algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unsupported algorithm; otherwise, <c>false</c>.</returns>
    public bool IsUnsupportedAlgorithm()
    {
        return status == EmailDkimSigningAttemptStatus.UnsupportedAlgorithm;
    }

    /// <summary>
    /// Determines whether the current value or state is prohibited algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is prohibited algorithm; otherwise, <c>false</c>.</returns>
    public bool IsProhibitedAlgorithm()
    {
        return status == EmailDkimSigningAttemptStatus.ProhibitedAlgorithm;
    }

    /// <summary>
    /// Determines whether the signing result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signing result is available; otherwise, <c>false</c>.</returns>
    public bool HasSigningResult()
    {
        return signingResult != null;
    }

    /// <summary>
    /// Gets the signing result.
    /// </summary>
    /// <returns>The current signing result.</returns>
    public EmailDkimSigningResult? GetSigningResult()
    {
        return signingResult;
    }

    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    public string GetReason()
    {
        return reason;
    }
}
