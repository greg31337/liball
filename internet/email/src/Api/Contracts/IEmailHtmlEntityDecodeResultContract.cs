using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlEntityDecodeResult"/>.
/// </summary>
public interface IEmailHtmlEntityDecodeResultContract
{
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>
    /// Gets the decoded reference count.
    /// </summary>
    /// <returns>The current decoded reference count.</returns>
    int GetDecodedReferenceCount();
    /// <summary>
    /// Gets the malformed reference count.
    /// </summary>
    /// <returns>The current malformed reference count.</returns>
    int GetMalformedReferenceCount();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
