using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME compression operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeCompressionResult : IEmailSmimeCompressionResultContract
{
    byte[] cmsBytes;
    EmailIssueList issues;
    long uncompressedBytes;
    long compressedBytes;
    /// <summary>
    /// Creates a new S/MIME compression result.
    /// </summary>
    /// <param name = "cmsBytes">The source bytes or data to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "uncompressedBytes">The source bytes or data to process.</param>
    /// <param name = "compressedBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeCompressionResult(byte[] cmsBytes, EmailIssueList issues, long uncompressedBytes, long compressedBytes)
    {
        if (cmsBytes == null || issues == null)
            throw new ArgumentNullException("cmsBytes");
        if (uncompressedBytes < 0 || compressedBytes < 0)
            throw new ArgumentOutOfRangeException("uncompressedBytes");
        this.cmsBytes = Copy(cmsBytes);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.uncompressedBytes = uncompressedBytes;
        this.compressedBytes = compressedBytes;
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
    /// Gets the uncompressed bytes.
    /// </summary>
    /// <returns>The current uncompressed bytes.</returns>
    public long GetUncompressedBytes()
    {
        return uncompressedBytes;
    }

    /// <summary>
    /// Gets the compressed bytes.
    /// </summary>
    /// <returns>The current compressed bytes.</returns>
    public long GetCompressedBytes()
    {
        return compressedBytes;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
