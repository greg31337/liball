using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlTextResult"/>.
/// </summary>
public interface IEmailHtmlTextResultContract
{
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    EmailHtmlTokenizationResult GetTokenizationResult();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the skipped hidden element count.
    /// </summary>
    /// <returns>The current skipped hidden element count.</returns>
    int GetSkippedHiddenElementCount();
    /// <summary>
    /// Gets the appended link target count.
    /// </summary>
    /// <returns>The current appended link target count.</returns>
    int GetAppendedLinkTargetCount();
    /// <summary>
    /// Gets the appended image alternative count.
    /// </summary>
    /// <returns>The current appended image alternative count.</returns>
    int GetAppendedImageAlternativeCount();
    /// <summary>
    /// Determines whether the source text decode result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source text decode result is available; otherwise, <c>false</c>.</returns>
    bool HasSourceTextDecodeResult();
    /// <summary>
    /// Gets the source text decode result.
    /// </summary>
    /// <returns>The current source text decode result.</returns>
    EmailTextDecodeResult? GetSourceTextDecodeResult();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
