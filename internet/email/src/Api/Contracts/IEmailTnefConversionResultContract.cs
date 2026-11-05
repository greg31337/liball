using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefConversionResult"/>.
/// </summary>
public interface IEmailTnefConversionResultContract
{
    /// <summary>
    /// Determines whether the original parse result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original parse result is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalParseResult();
    /// <summary>
    /// Gets the original parse result.
    /// </summary>
    /// <returns>The current original parse result.</returns>
    EmailTnefParseResult GetOriginalParseResult();
    /// <summary>
    /// Determines whether the original message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the original message is available; otherwise, <c>false</c>.</returns>
    bool HasOriginalMessage();
    /// <summary>
    /// Gets the original message.
    /// </summary>
    /// <returns>The current original message.</returns>
    EmailTnefMessage? GetOriginalMessage();
    /// <summary>
    /// Gets the original properties.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original properties.</returns>
    List<EmailTnefProperty> GetOriginalProperties();
    /// <summary>
    /// Determines whether the entity is present or available.
    /// </summary>
    /// <returns><c>true</c> when the entity is available; otherwise, <c>false</c>.</returns>
    bool HasEntity();
    /// <summary>
    /// Gets the entity.
    /// </summary>
    /// <returns>The current entity.</returns>
    MimeEntity? GetEntity();
    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    List<EmailTnefConvertedAttachment> GetAttachments();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
