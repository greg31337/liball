using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCmsReadResult"/>.
/// </summary>
public interface IEmailCmsReadResultContract
{
    /// <summary>
    /// Determines whether the content info is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content info is available; otherwise, <c>false</c>.</returns>
    bool HasContentInfo();
    /// <summary>
    /// Gets the content info.
    /// </summary>
    /// <returns>The current content info.</returns>
    EmailCmsContentInfo? GetContentInfo();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the ASN.1 element count.
    /// </summary>
    /// <returns>The current ASN.1 element count.</returns>
    int GetAsn1ElementCount();
    /// <summary>
    /// Gets the maximum ASN.1 depth.
    /// </summary>
    /// <returns>The current maximum ASN.1 depth.</returns>
    int GetMaximumAsn1Depth();
}
