using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextAnalysisResult"/>.
/// </summary>
public interface IEmailTextAnalysisResultContract
{
    /// <summary>
    /// Gets the visible text.
    /// </summary>
    /// <returns>The current visible text.</returns>
    string GetVisibleText();
    /// <summary>
    /// Gets the quoted text.
    /// </summary>
    /// <returns>The current quoted text.</returns>
    string GetQuotedText();
    /// <summary>
    /// Gets the signature text.
    /// </summary>
    /// <returns>The current signature text.</returns>
    string GetSignatureText();
    /// <summary>
    /// Gets the snippet.
    /// </summary>
    /// <returns>The current snippet.</returns>
    string GetSnippet();
    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <returns>The current line count.</returns>
    int GetLineCount();
    /// <summary>
    /// Gets the quoted line count.
    /// </summary>
    /// <returns>The current quoted line count.</returns>
    int GetQuotedLineCount();
    /// <summary>
    /// Gets the signature line count.
    /// </summary>
    /// <returns>The current signature line count.</returns>
    int GetSignatureLineCount();
    /// <summary>
    /// Determines whether the quoted reply is present or available.
    /// </summary>
    /// <returns><c>true</c> when the quoted reply is available; otherwise, <c>false</c>.</returns>
    bool HasQuotedReply();
    /// <summary>
    /// Gets the quoted reply offset.
    /// </summary>
    /// <returns>The current quoted reply offset.</returns>
    int GetQuotedReplyOffset();
    /// <summary>
    /// Determines whether the signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature is available; otherwise, <c>false</c>.</returns>
    bool HasSignature();
    /// <summary>
    /// Gets the signature offset.
    /// </summary>
    /// <returns>The current signature offset.</returns>
    int GetSignatureOffset();
    /// <summary>
    /// Determines whether the operation or source value was snippet truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasSnippetTruncated();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
