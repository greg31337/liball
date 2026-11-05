using System;

namespace email;
/// <summary>
/// Describes the outcome of a HTML text operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHtmlTextResult : IEmailHtmlTextResultContract
{
    string text;
    EmailHtmlTokenizationResult tokenization;
    EmailIssueList issues;
    int skippedHiddenElementCount;
    int appendedLinkTargetCount;
    int appendedImageAlternativeCount;
    EmailTextDecodeResult? sourceTextDecodeResult;
    /// <summary>
    /// Creates a new HTML text result.
    /// </summary>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "tokenization">The tokenization used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "skippedHiddenElementCount">The number of items or bytes to process.</param>
    /// <param name = "appendedLinkTargetCount">The number of items or bytes to process.</param>
    /// <param name = "appendedImageAlternativeCount">The number of items or bytes to process.</param>
    /// <param name = "sourceTextDecodeResult">The source text decode result used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlTextResult(string text, EmailHtmlTokenizationResult tokenization, EmailIssueList issues, int skippedHiddenElementCount, int appendedLinkTargetCount, int appendedImageAlternativeCount, EmailTextDecodeResult? sourceTextDecodeResult)
    {
        if (text == null)
            throw new ArgumentNullException("text");
        if (tokenization == null)
            throw new ArgumentNullException("tokenization");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (skippedHiddenElementCount < 0)
            throw new ArgumentOutOfRangeException("skippedHiddenElementCount");
        if (appendedLinkTargetCount < 0)
            throw new ArgumentOutOfRangeException("appendedLinkTargetCount");
        if (appendedImageAlternativeCount < 0)
            throw new ArgumentOutOfRangeException("appendedImageAlternativeCount");
        this.text = text;
        this.tokenization = tokenization;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.skippedHiddenElementCount = skippedHiddenElementCount;
        this.appendedLinkTargetCount = appendedLinkTargetCount;
        this.appendedImageAlternativeCount = appendedImageAlternativeCount;
        this.sourceTextDecodeResult = sourceTextDecodeResult;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    public string GetText()
    {
        return text;
    }

    /// <summary>
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    public EmailHtmlTokenizationResult GetTokenizationResult()
    {
        return tokenization;
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
    /// Gets the skipped hidden element count.
    /// </summary>
    /// <returns>The current skipped hidden element count.</returns>
    public int GetSkippedHiddenElementCount()
    {
        return skippedHiddenElementCount;
    }

    /// <summary>
    /// Gets the appended link target count.
    /// </summary>
    /// <returns>The current appended link target count.</returns>
    public int GetAppendedLinkTargetCount()
    {
        return appendedLinkTargetCount;
    }

    /// <summary>
    /// Gets the appended image alternative count.
    /// </summary>
    /// <returns>The current appended image alternative count.</returns>
    public int GetAppendedImageAlternativeCount()
    {
        return appendedImageAlternativeCount;
    }

    /// <summary>
    /// Determines whether the source text decode result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source text decode result is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceTextDecodeResult()
    {
        return sourceTextDecodeResult != null;
    }

    /// <summary>
    /// Gets the source text decode result.
    /// </summary>
    /// <returns>The current source text decode result.</returns>
    public EmailTextDecodeResult? GetSourceTextDecodeResult()
    {
        return sourceTextDecodeResult;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
