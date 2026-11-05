using System;

namespace email;
/// <summary>
/// Describes the outcome of a metadata parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailMetadataParseResult : IEmailMetadataParseResultContract
{
    EmailMetadataValueKind kind;
    object? value;
    EmailIssueList issues;
    int consumedByteCount;
    int sourceByteCount;
    /// <summary>
    /// Creates a new metadata parse result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailMetadataParseResult(EmailMetadataValueKind kind, object? value, EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        if (kind < EmailMetadataValueKind.Keywords || kind > EmailMetadataValueKind.ResentField)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (consumedByteCount < 0 || sourceByteCount < 0 || consumedByteCount > sourceByteCount)
        {
            throw new ArgumentOutOfRangeException("consumedByteCount");
        }

        this.kind = kind;
        this.value = value;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.consumedByteCount = consumedByteCount;
        this.sourceByteCount = sourceByteCount;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailMetadataValueKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Determines whether the value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the value is available; otherwise, <c>false</c>.</returns>
    public bool HasValue()
    {
        return value != null;
    }

    /// <summary>
    /// Gets the keywords.
    /// </summary>
    /// <returns>The current keywords.</returns>
    public EmailKeywordList? GetKeywords()
    {
        return kind == EmailMetadataValueKind.Keywords ? (EmailKeywordList? )value : null;
    }

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <returns>The current priority.</returns>
    public EmailPriority? GetPriority()
    {
        return kind == EmailMetadataValueKind.Priority ? (EmailPriority? )value : null;
    }

    /// <summary>
    /// Gets the importance.
    /// </summary>
    /// <returns>The current importance.</returns>
    public EmailImportance? GetImportance()
    {
        return kind == EmailMetadataValueKind.Importance ? (EmailImportance? )value : null;
    }

    /// <summary>
    /// Gets the sensitivity.
    /// </summary>
    /// <returns>The current sensitivity.</returns>
    public EmailSensitivity? GetSensitivity()
    {
        return kind == EmailMetadataValueKind.Sensitivity ? (EmailSensitivity? )value : null;
    }

    /// <summary>
    /// Gets the auto submitted.
    /// </summary>
    /// <returns>The current auto submitted.</returns>
    public EmailAutoSubmitted? GetAutoSubmitted()
    {
        return kind == EmailMetadataValueKind.AutoSubmitted ? (EmailAutoSubmitted? )value : null;
    }

    /// <summary>
    /// Gets the list management.
    /// </summary>
    /// <returns>The current list management.</returns>
    public EmailListManagementValue? GetListManagement()
    {
        return kind == EmailMetadataValueKind.ListManagement ? (EmailListManagementValue? )value : null;
    }

    /// <summary>
    /// Gets the list unsubscribe post.
    /// </summary>
    /// <returns>The current list unsubscribe post.</returns>
    public EmailListUnsubscribePost? GetListUnsubscribePost()
    {
        return kind == EmailMetadataValueKind.ListUnsubscribePost ? (EmailListUnsubscribePost? )value : null;
    }

    /// <summary>
    /// Gets the resent field.
    /// </summary>
    /// <returns>The current resent field.</returns>
    public EmailResentField? GetResentField()
    {
        return kind == EmailMetadataValueKind.ResentField ? (EmailResentField? )value : null;
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
