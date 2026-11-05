using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a openPGP verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailOpenPgpVerificationResult : IEmailOpenPgpVerificationResultContract
{
    EmailOpenPgpVerificationStatus status;
    EmailOpenPgpSignedFormat format;
    byte[] content;
    byte[] signerIdentifier;
    int hashAlgorithm;
    int publicKeyAlgorithm;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new openPGP verification result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "format">The format used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "signerIdentifier">The signer identifier used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpVerificationResult(EmailOpenPgpVerificationStatus status, EmailOpenPgpSignedFormat format, byte[] content, byte[] signerIdentifier, int hashAlgorithm, int publicKeyAlgorithm, EmailIssueList issues)
    {
        if (content == null || signerIdentifier == null || issues == null)
            throw new ArgumentNullException("content");
        this.status = status;
        this.format = format;
        this.content = new EmailOpenPgpSigningCredentialOperations().Copy(content);
        this.signerIdentifier = new EmailOpenPgpSigningCredentialOperations().Copy(signerIdentifier);
        this.hashAlgorithm = hashAlgorithm;
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.issues = new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailOpenPgpVerificationStatus.Pass && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailOpenPgpVerificationStatus GetStatus()
    {
        return status;
    }

    /// <summary>
    /// Gets the format.
    /// </summary>
    /// <returns>The current format.</returns>
    public EmailOpenPgpSignedFormat GetFormat()
    {
        return format;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    public byte[] GetContent()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(content);
    }

    /// <summary>
    /// Gets the signer identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signer identifier.</returns>
    public byte[] GetSignerIdentifier()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(signerIdentifier);
    }

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    public int GetHashAlgorithm()
    {
        return hashAlgorithm;
    }

    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    public int GetPublicKeyAlgorithm()
    {
        return publicKeyAlgorithm;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return new EmailOpenPgpSigningResultOperations().CopyIssues(issues);
    }
}
