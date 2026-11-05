using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Describes the outcome of a S/MIME decompression operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSmimeDecompressionResult : IEmailSmimeDecompressionResultContract
{
    EmailSmimeDecompressionStatus status;
    byte[]? content;
    byte[] compressedContent;
    byte[] originalCmsBytes;
    string algorithmObjectIdentifier;
    string contentTypeObjectIdentifier;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new S/MIME decompression result.
    /// </summary>
    /// <param name = "status">The status used by the operation.</param>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "compressedContent">The compressed content used by the operation.</param>
    /// <param name = "originalCmsBytes">The source bytes or data to process.</param>
    /// <param name = "algorithmObjectIdentifier">The algorithm object identifier used by the operation.</param>
    /// <param name = "contentTypeObjectIdentifier">The content type object identifier used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeDecompressionResult(EmailSmimeDecompressionStatus status, byte[]? content, byte[] compressedContent, byte[] originalCmsBytes, string algorithmObjectIdentifier, string contentTypeObjectIdentifier, EmailIssueList issues)
    {
        if (status < EmailSmimeDecompressionStatus.Success || status > EmailSmimeDecompressionStatus.DecompressionFailed)
            throw new ArgumentOutOfRangeException("status");
        if (compressedContent == null || originalCmsBytes == null || issues == null)
            throw new ArgumentNullException("compressedContent");
        if (algorithmObjectIdentifier == null || contentTypeObjectIdentifier == null)
            throw new ArgumentNullException("algorithmObjectIdentifier");
        if ((status == EmailSmimeDecompressionStatus.Success && content == null) || (status != EmailSmimeDecompressionStatus.Success && content != null))
            throw new ArgumentException("Only successful decompression results can contain decoded content.", "content");
        this.status = status;
        this.content = content == null ? null : Copy(content);
        this.compressedContent = Copy(compressedContent);
        this.originalCmsBytes = Copy(originalCmsBytes);
        this.algorithmObjectIdentifier = algorithmObjectIdentifier;
        this.contentTypeObjectIdentifier = contentTypeObjectIdentifier;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return status == EmailSmimeDecompressionStatus.Success && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the current value or state is unsupported algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unsupported algorithm; otherwise, <c>false</c>.</returns>
    public bool IsUnsupportedAlgorithm()
    {
        return status == EmailSmimeDecompressionStatus.UnsupportedAlgorithm;
    }

    /// <summary>
    /// Determines whether the current value or state is losslessly preserved.
    /// </summary>
    /// <returns><c>true</c> when the value or state is losslessly preserved; otherwise, <c>false</c>.</returns>
    public bool IsLosslesslyPreserved()
    {
        return originalCmsBytes.Length > 0;
    }

    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public EmailSmimeDecompressionStatus GetStatus()
    {
        return status;
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
    /// Gets the compressed content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current compressed content.</returns>
    public byte[] GetCompressedContent()
    {
        return Copy(compressedContent);
    }

    /// <summary>
    /// Gets the original CMS bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original CMS bytes.</returns>
    public byte[] GetOriginalCmsBytes()
    {
        return Copy(originalCmsBytes);
    }

    /// <summary>
    /// Gets the algorithm object identifier.
    /// </summary>
    /// <returns>The current algorithm object identifier.</returns>
    public string GetAlgorithmObjectIdentifier()
    {
        return algorithmObjectIdentifier;
    }

    /// <summary>
    /// Gets the content type object identifier.
    /// </summary>
    /// <returns>The current content type object identifier.</returns>
    public string GetContentTypeObjectIdentifier()
    {
        return contentTypeObjectIdentifier;
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
