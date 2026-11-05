using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME encryption operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeEncryptionResult : IEmailSmimeEncryptionResultContract
{
    byte[] cmsBytes;
    EmailIssueList issues;
    EmailSmimeContentEncryptionAlgorithm contentAlgorithm;
    EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm;
    int recipientCount;
    /// <summary>
    /// Creates a new S/MIME encryption result.
    /// </summary>
    /// <param name = "cmsBytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "contentAlgorithm">The content algorithm used by the operation.</param>
    /// <param name = "keyTransportAlgorithm">The key transport algorithm used by the operation.</param>
    /// <param name = "recipientCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeEncryptionResult(byte[] cmsBytes, EmailIssueList issues, EmailSmimeContentEncryptionAlgorithm contentAlgorithm, EmailSmimeKeyTransportAlgorithm keyTransportAlgorithm, int recipientCount)
    {
        if (cmsBytes == null || issues == null)
            throw new ArgumentNullException("cmsBytes");
        if (recipientCount < 0)
            throw new ArgumentOutOfRangeException("recipientCount");
        this.cmsBytes = Copy(cmsBytes);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.contentAlgorithm = contentAlgorithm;
        this.keyTransportAlgorithm = keyTransportAlgorithm;
        this.recipientCount = recipientCount;
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
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Gets the content algorithm.
    /// </summary>
    /// <returns>The current content algorithm.</returns>
    public EmailSmimeContentEncryptionAlgorithm GetContentAlgorithm()
    {
        return contentAlgorithm;
    }

    /// <summary>
    /// Gets the key transport algorithm.
    /// </summary>
    /// <returns>The current key transport algorithm.</returns>
    public EmailSmimeKeyTransportAlgorithm GetKeyTransportAlgorithm()
    {
        return keyTransportAlgorithm;
    }

    /// <summary>
    /// Gets the recipient count.
    /// </summary>
    /// <returns>The current recipient count.</returns>
    public int GetRecipientCount()
    {
        return recipientCount;
    }

    /// <summary>
    /// Determines whether the current value or state is authenticated.
    /// </summary>
    /// <returns><c>true</c> when the value or state is authenticated; otherwise, <c>false</c>.</returns>
    public bool IsAuthenticated()
    {
        return contentAlgorithm == EmailSmimeContentEncryptionAlgorithm.Aes128Gcm || contentAlgorithm == EmailSmimeContentEncryptionAlgorithm.Aes256Gcm;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
