using System;

namespace email;
/// <summary>
/// Describes the outcome of a HTML entity decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHtmlEntityDecodeResult : IEmailHtmlEntityDecodeResultContract
{
    string text;
    int decodedReferenceCount;
    int malformedReferenceCount;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new HTML entity decode result.
    /// </summary>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "decodedReferenceCount">The number of items or bytes to process.</param>
    /// <param name = "malformedReferenceCount">The number of items or bytes to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlEntityDecodeResult(string text, int decodedReferenceCount, int malformedReferenceCount, EmailIssueList issues)
    {
        if (text == null)
            throw new ArgumentNullException("text");
        if (decodedReferenceCount < 0)
            throw new ArgumentOutOfRangeException("decodedReferenceCount");
        if (malformedReferenceCount < 0)
            throw new ArgumentOutOfRangeException("malformedReferenceCount");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.text = text;
        this.decodedReferenceCount = decodedReferenceCount;
        this.malformedReferenceCount = malformedReferenceCount;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
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
    /// Gets the decoded reference count.
    /// </summary>
    /// <returns>The current decoded reference count.</returns>
    public int GetDecodedReferenceCount()
    {
        return decodedReferenceCount;
    }

    /// <summary>
    /// Gets the malformed reference count.
    /// </summary>
    /// <returns>The current malformed reference count.</returns>
    public int GetMalformedReferenceCount()
    {
        return malformedReferenceCount;
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
