using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeVerificationResult : IEmailSmimeVerificationResultContract
{
    List<EmailSmimeSignerVerificationResult> signers;
    EmailIssueList issues;
    byte[]? content;
    bool detached;
    /// <summary>
    /// Creates a new S/MIME verification result.
    /// </summary>
    /// <param name = "signers">The signers used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "detached">Whether to enable the detached behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeVerificationResult(List<EmailSmimeSignerVerificationResult> signers, EmailIssueList issues, byte[]? content, bool detached)
    {
        if (signers == null || issues == null)
            throw new ArgumentNullException("signers");
        this.signers = new List<EmailSmimeSignerVerificationResult>(signers);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.content = content == null ? null : Copy(content);
        this.detached = detached;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        if (signers.Count == 0 || issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
            return false;
        int index = 0;
        while (index < signers.Count)
        {
            if (!signers[index].IsSuccess())
                return false;
            index++;
        }

        return true;
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
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    public bool HasContent()
    {
        return content != null;
    }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    public byte[]? GetContent()
    {
        return content == null ? null : Copy(content);
    }

    /// <summary>
    /// Gets the signer count.
    /// </summary>
    /// <returns>The current signer count.</returns>
    public int GetSignerCount()
    {
        return signers.Count;
    }

    /// <summary>
    /// Gets the signer at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested signer.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeSignerVerificationResult GetSignerAt(int index)
    {
        if (index < 0 || index >= signers.Count)
            throw new ArgumentOutOfRangeException("index");
        return signers[index];
    }

    /// <summary>
    /// Gets the signers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signers.</returns>
    public List<EmailSmimeSignerVerificationResult> GetSigners()
    {
        return new List<EmailSmimeSignerVerificationResult>(signers);
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
