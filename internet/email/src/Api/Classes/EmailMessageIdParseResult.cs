using System;

namespace email;
/// <summary>
/// Describes the outcome of a message ID parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailMessageIdParseResult : IEmailMessageIdParseResultContract
{
    EmailMessageIdList identifiers;
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;
    /// <summary>
    /// Creates a new message ID parse result.
    /// </summary>
    /// <param name = "identifiers">The identifiers used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMessageIdParseResult(EmailMessageIdList identifiers, EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        if (identifiers == null)
        {
            throw new ArgumentNullException("identifiers");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        this.identifiers = identifiers;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
    }

    /// <summary>
    /// Gets the identifiers.
    /// </summary>
    /// <returns>The current identifiers.</returns>
    public EmailMessageIdList GetIdentifiers()
    {
        return identifiers;
    }

    /// <summary>
    /// Determines whether the single identifier is present or available.
    /// </summary>
    /// <returns><c>true</c> when the single identifier is available; otherwise, <c>false</c>.</returns>
    public bool HasSingleIdentifier()
    {
        return identifiers.GetCount() == 1;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <returns>The current identifier.</returns>
    public EmailMessageId? GetIdentifier()
    {
        if (!HasSingleIdentifier())
        {
            return null;
        }

        return identifiers.GetAt(0);
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
    public int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return consumedByteCount == sourceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
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
