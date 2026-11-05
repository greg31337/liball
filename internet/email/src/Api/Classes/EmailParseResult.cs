using System;

namespace email;
/// <summary>
/// Describes the outcome of a parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailParseResult : IEmailParseResultContract
{
    EmailMessage? message;
    EmailIssueList issues;
    long consumedByteCount;
    long sourceByteCount;
    bool complete;
    /// <summary>
    /// Creates a new parse result.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailParseResult(EmailMessage? message, EmailIssueList issues, long consumedByteCount, long sourceByteCount, bool complete)
    {
        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        if (complete && message == null)
        {
            throw new ArgumentException("A complete parse result requires a message.", "message");
        }

        this.message = message;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
        this.complete = complete;
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
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
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public long GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
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
        return complete && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
