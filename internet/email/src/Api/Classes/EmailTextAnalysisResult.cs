using System;

namespace email;
/// <summary>
/// Describes the outcome of a text analysis operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTextAnalysisResult : IEmailTextAnalysisResultContract
{
    string visibleText;
    string quotedText;
    string signatureText;
    string snippet;
    int lineCount;
    int quotedLineCount;
    int signatureLineCount;
    int quotedReplyOffset;
    int signatureOffset;
    bool snippetTruncated;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new text analysis result.
    /// </summary>
    /// <param name = "visibleText">The visible text used by the operation.</param>
    /// <param name = "quotedText">The quoted text used by the operation.</param>
    /// <param name = "signatureText">The signature text used by the operation.</param>
    /// <param name = "snippet">The snippet used by the operation.</param>
    /// <param name = "lineCount">The number of items or bytes to process.</param>
    /// <param name = "quotedLineCount">The number of items or bytes to process.</param>
    /// <param name = "signatureLineCount">The number of items or bytes to process.</param>
    /// <param name = "quotedReplyOffset">The zero-based quoted reply offset.</param>
    /// <param name = "signatureOffset">The zero-based signature offset.</param>
    /// <param name = "snippetTruncated">Whether to enable the snippet truncated behavior.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTextAnalysisResult(string visibleText, string quotedText, string signatureText, string snippet, int lineCount, int quotedLineCount, int signatureLineCount, int quotedReplyOffset, int signatureOffset, bool snippetTruncated, EmailIssueList issues)
    {
        if (visibleText == null)
            throw new ArgumentNullException("visibleText");
        if (quotedText == null)
            throw new ArgumentNullException("quotedText");
        if (signatureText == null)
            throw new ArgumentNullException("signatureText");
        if (snippet == null)
            throw new ArgumentNullException("snippet");
        if (lineCount < 0)
            throw new ArgumentOutOfRangeException("lineCount");
        if (quotedLineCount < 0 || quotedLineCount > lineCount)
            throw new ArgumentOutOfRangeException("quotedLineCount");
        if (signatureLineCount < 0 || signatureLineCount > lineCount)
            throw new ArgumentOutOfRangeException("signatureLineCount");
        if (quotedReplyOffset < -1)
            throw new ArgumentOutOfRangeException("quotedReplyOffset");
        if (signatureOffset < -1)
            throw new ArgumentOutOfRangeException("signatureOffset");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.visibleText = visibleText;
        this.quotedText = quotedText;
        this.signatureText = signatureText;
        this.snippet = snippet;
        this.lineCount = lineCount;
        this.quotedLineCount = quotedLineCount;
        this.signatureLineCount = signatureLineCount;
        this.quotedReplyOffset = quotedReplyOffset;
        this.signatureOffset = signatureOffset;
        this.snippetTruncated = snippetTruncated;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the visible text.
    /// </summary>
    /// <returns>The current visible text.</returns>
    public string GetVisibleText()
    {
        return visibleText;
    }

    /// <summary>
    /// Gets the quoted text.
    /// </summary>
    /// <returns>The current quoted text.</returns>
    public string GetQuotedText()
    {
        return quotedText;
    }

    /// <summary>
    /// Gets the signature text.
    /// </summary>
    /// <returns>The current signature text.</returns>
    public string GetSignatureText()
    {
        return signatureText;
    }

    /// <summary>
    /// Gets the snippet.
    /// </summary>
    /// <returns>The current snippet.</returns>
    public string GetSnippet()
    {
        return snippet;
    }

    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <returns>The current line count.</returns>
    public int GetLineCount()
    {
        return lineCount;
    }

    /// <summary>
    /// Gets the quoted line count.
    /// </summary>
    /// <returns>The current quoted line count.</returns>
    public int GetQuotedLineCount()
    {
        return quotedLineCount;
    }

    /// <summary>
    /// Gets the signature line count.
    /// </summary>
    /// <returns>The current signature line count.</returns>
    public int GetSignatureLineCount()
    {
        return signatureLineCount;
    }

    /// <summary>
    /// Determines whether the quoted reply is present or available.
    /// </summary>
    /// <returns><c>true</c> when the quoted reply is available; otherwise, <c>false</c>.</returns>
    public bool HasQuotedReply()
    {
        return quotedReplyOffset >= 0;
    }

    /// <summary>
    /// Gets the quoted reply offset.
    /// </summary>
    /// <returns>The current quoted reply offset.</returns>
    public int GetQuotedReplyOffset()
    {
        return quotedReplyOffset;
    }

    /// <summary>
    /// Determines whether the signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signature is available; otherwise, <c>false</c>.</returns>
    public bool HasSignature()
    {
        return signatureOffset >= 0;
    }

    /// <summary>
    /// Gets the signature offset.
    /// </summary>
    /// <returns>The current signature offset.</returns>
    public int GetSignatureOffset()
    {
        return signatureOffset;
    }

    /// <summary>
    /// Determines whether the operation or source value was snippet truncated.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasSnippetTruncated()
    {
        return snippetTruncated;
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
}
