using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDisplayModel"/>.
/// </summary>
public interface IEmailDisplayModelContract
{
    /// <summary>
    /// Gets the body selection.
    /// </summary>
    /// <returns>The current body selection.</returns>
    EmailBodySelection GetBodySelection();
    /// <summary>
    /// Determines whether the selected body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the selected body is available; otherwise, <c>false</c>.</returns>
    bool HasSelectedBody();
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
    /// <summary>
    /// Determines whether the HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the HTML is available; otherwise, <c>false</c>.</returns>
    bool HasHtml();
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    string? GetHtml();
    /// <summary>
    /// Gets the snippet.
    /// </summary>
    /// <returns>The current snippet.</returns>
    string GetSnippet();
    /// <summary>
    /// Gets the text analysis.
    /// </summary>
    /// <returns>The current text analysis.</returns>
    EmailTextAnalysisResult GetTextAnalysis();
    /// <summary>
    /// Gets the alternative count.
    /// </summary>
    /// <returns>The current alternative count.</returns>
    int GetAlternativeCount();
    /// <summary>
    /// Gets the alternative.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current alternative.</returns>
    MimeEntity GetAlternative(int index);
    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current alternatives.</returns>
    List<MimeEntity> GetAlternatives();
    /// <summary>
    /// Gets the inline resource count.
    /// </summary>
    /// <returns>The current inline resource count.</returns>
    int GetInlineResourceCount();
    /// <summary>
    /// Gets the inline resource.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current inline resource.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAttachmentView GetInlineResource(int index);
    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current inline resources.</returns>
    List<EmailAttachmentView> GetInlineResources();
    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    int GetAttachmentCount();
    /// <summary>
    /// Gets the attachment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current attachment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAttachmentView GetAttachment(int index);
    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    List<EmailAttachmentView> GetAttachments();
    /// <summary>
    /// Gets the missing related references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing related references.</returns>
    List<EmailRelatedReference> GetMissingRelatedReferences();
    /// <summary>
    /// Gets the security summary.
    /// </summary>
    /// <returns>The current security summary.</returns>
    EmailDisplaySecuritySummary GetSecuritySummary();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the warnings.
    /// </summary>
    /// <returns>The current warnings.</returns>
    EmailIssueList GetWarnings();
    /// <summary>
    /// Determines whether the source text decode result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source text decode result is available; otherwise, <c>false</c>.</returns>
    bool HasSourceTextDecodeResult();
    /// <summary>
    /// Gets the source text decode result.
    /// </summary>
    /// <returns>The current source text decode result.</returns>
    EmailTextDecodeResult? GetSourceTextDecodeResult();
    /// <summary>
    /// Determines whether the HTML sanitize result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the HTML sanitize result is available; otherwise, <c>false</c>.</returns>
    bool HasHtmlSanitizeResult();
    /// <summary>
    /// Gets the HTML sanitize result.
    /// </summary>
    /// <returns>The current HTML sanitize result.</returns>
    EmailHtmlSanitizeResult? GetHtmlSanitizeResult();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
