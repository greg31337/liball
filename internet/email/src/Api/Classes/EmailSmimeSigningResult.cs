using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME signing operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeSigningResult : IEmailSmimeSigningResultContract
{
    byte[] cmsBytes;
    EmailIssueList issues;
    bool detached;
    int signerCount;
    /// <summary>
    /// Creates a new S/MIME signing result.
    /// </summary>
    /// <param name = "cmsBytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "detached">Whether to enable the detached behavior.</param>
    /// <param name = "signerCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeSigningResult(byte[] cmsBytes, EmailIssueList issues, bool detached, int signerCount)
    {
        if (cmsBytes == null || issues == null)
            throw new ArgumentNullException("cmsBytes");
        if (signerCount < 0)
            throw new ArgumentOutOfRangeException("signerCount");
        this.cmsBytes = Copy(cmsBytes);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.detached = detached;
        this.signerCount = signerCount;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return cmsBytes.Length > 0 && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the CMS bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current CMS bytes.</returns>
    public byte[] GetCmsBytes()
    {
        return Copy(cmsBytes);
    }

    /// <summary>
    /// Determines whether the current value or state is detached.
    /// </summary>
    /// <returns><c>true</c> when the value or state is detached; otherwise, <c>false</c>.</returns>
    public bool IsDetached()
    {
        return detached;
    }

    /// <summary>
    /// Determines whether the current value or state is opaque.
    /// </summary>
    /// <returns><c>true</c> when the value or state is opaque; otherwise, <c>false</c>.</returns>
    public bool IsOpaque()
    {
        return !detached;
    }

    /// <summary>
    /// Gets the signer count.
    /// </summary>
    /// <returns>The current signer count.</returns>
    public int GetSignerCount()
    {
        return signerCount;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
