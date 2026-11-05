using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMetadataParseResult"/>.
/// </summary>
public interface IEmailMetadataParseResultContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailMetadataValueKind GetKind();
    /// <summary>
    /// Determines whether the value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the value is available; otherwise, <c>false</c>.</returns>
    bool HasValue();
    /// <summary>
    /// Gets the keywords.
    /// </summary>
    /// <returns>The current keywords.</returns>
    EmailKeywordList? GetKeywords();
    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <returns>The current priority.</returns>
    EmailPriority? GetPriority();
    /// <summary>
    /// Gets the importance.
    /// </summary>
    /// <returns>The current importance.</returns>
    EmailImportance? GetImportance();
    /// <summary>
    /// Gets the sensitivity.
    /// </summary>
    /// <returns>The current sensitivity.</returns>
    EmailSensitivity? GetSensitivity();
    /// <summary>
    /// Gets the auto submitted.
    /// </summary>
    /// <returns>The current auto submitted.</returns>
    EmailAutoSubmitted? GetAutoSubmitted();
    /// <summary>
    /// Gets the list management.
    /// </summary>
    /// <returns>The current list management.</returns>
    EmailListManagementValue? GetListManagement();
    /// <summary>
    /// Gets the list unsubscribe post.
    /// </summary>
    /// <returns>The current list unsubscribe post.</returns>
    EmailListUnsubscribePost? GetListUnsubscribePost();
    /// <summary>
    /// Gets the resent field.
    /// </summary>
    /// <returns>The current resent field.</returns>
    EmailResentField? GetResentField();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    int GetConsumedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    int GetSourceByteCount();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
