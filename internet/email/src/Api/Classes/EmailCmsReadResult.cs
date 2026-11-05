using System;

namespace email;
/// <summary>
/// Describes the outcome of a CMS read operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCmsReadResult : IEmailCmsReadResultContract
{
    EmailCmsContentInfo? contentInfo;
    EmailIssueList issues;
    bool complete;
    int asn1ElementCount;
    int maximumAsn1Depth;
    /// <summary>
    /// Creates a new CMS read result.
    /// </summary>
    /// <param name = "contentInfo">The content info used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <param name = "asn1ElementCount">The number of items or bytes to process.</param>
    /// <param name = "maximumAsn1Depth">The maximum ASN.1 depth used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCmsReadResult(EmailCmsContentInfo? contentInfo, EmailIssueList issues, bool complete, int asn1ElementCount, int maximumAsn1Depth)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (asn1ElementCount < 0 || maximumAsn1Depth < 0)
            throw new ArgumentOutOfRangeException("asn1ElementCount");
        this.contentInfo = contentInfo;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.complete = complete;
        this.asn1ElementCount = asn1ElementCount;
        this.maximumAsn1Depth = maximumAsn1Depth;
    }

    /// <summary>
    /// Determines whether the content info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content info is available; otherwise, <c>false</c>.</returns>
    public bool HasContentInfo()
    {
        return contentInfo != null;
    }

    /// <summary>
    /// Gets the content info.
    /// </summary>
    /// <returns>The current content info.</returns>
    public EmailCmsContentInfo? GetContentInfo()
    {
        return contentInfo;
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
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return contentInfo != null && complete && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Gets the ASN.1 element count.
    /// </summary>
    /// <returns>The current ASN.1 element count.</returns>
    public int GetAsn1ElementCount()
    {
        return asn1ElementCount;
    }

    /// <summary>
    /// Gets the maximum ASN.1 depth.
    /// </summary>
    /// <returns>The current maximum ASN.1 depth.</returns>
    public int GetMaximumAsn1Depth()
    {
        return maximumAsn1Depth;
    }
}
