using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a TNEF conversion operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTnefConversionResult : IEmailTnefConversionResultContract
{
    EmailTnefParseResult originalParseResult;
    EmailTnefMessage? originalMessage;
    MimeEntity? entity;
    List<EmailTnefConvertedAttachment> attachments;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new TNEF conversion result.
    /// </summary>
    /// <param name = "originalParseResult">The original parse result used by the operation.</param>
    /// <param name = "originalMessage">The original message used by the operation.</param>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "attachments">The attachments used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailTnefConversionResult(EmailTnefParseResult originalParseResult, EmailTnefMessage? originalMessage, MimeEntity? entity, List<EmailTnefConvertedAttachment> attachments, EmailIssueList issues)
    {
        if (attachments == null)
            throw new ArgumentNullException("attachments");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.originalParseResult = originalParseResult;
        this.originalMessage = originalMessage;
        this.entity = entity;
        this.attachments = new List<EmailTnefConvertedAttachment>(attachments);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the original parse result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original parse result is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalParseResult()
    {
        return originalParseResult != null;
    }

    /// <summary>
    /// Gets the original parse result.
    /// </summary>
    /// <returns>The current original parse result.</returns>
    public EmailTnefParseResult GetOriginalParseResult()
    {
        return originalParseResult;
    }

    /// <summary>
    /// Determines whether the original message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message is available; otherwise, <c>false</c>.</returns>
    public bool HasOriginalMessage()
    {
        return originalMessage != null;
    }

    /// <summary>
    /// Gets the original message.
    /// </summary>
    /// <returns>The current original message.</returns>
    public EmailTnefMessage? GetOriginalMessage()
    {
        return originalMessage;
    }

    /// <summary>
    /// Gets the original properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original properties.</returns>
    public List<EmailTnefProperty> GetOriginalProperties()
    {
        return originalMessage == null ? new List<EmailTnefProperty>() : originalMessage.GetProperties();
    }

    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    public bool HasEntity()
    {
        return entity != null;
    }

    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    public MimeEntity? GetEntity()
    {
        return entity;
    }

    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    public List<EmailTnefConvertedAttachment> GetAttachments()
    {
        return new List<EmailTnefConvertedAttachment>(attachments);
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
        return entity != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
