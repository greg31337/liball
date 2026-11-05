using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a display model.
/// </summary>
public class EmailDisplayModel : IEmailDisplayModelContract
{
    EmailBodySelection selection;
    string text;
    string? html;
    EmailTextAnalysisResult textAnalysis;
    List<EmailAttachmentView> inlineResources;
    List<EmailAttachmentView> attachments;
    List<EmailRelatedReference> missingRelatedReferences;
    EmailDisplaySecuritySummary securitySummary;
    EmailIssueList issues;
    EmailTextDecodeResult? sourceTextDecodeResult;
    EmailHtmlSanitizeResult? htmlSanitizeResult;
    /// <summary>
    /// Creates a new display model.
    /// </summary>
    /// <param name = "selection">The selection used by the operation.</param>
    /// <param name = "text">The text used by the operation.</param>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "textAnalysis">The text analysis used by the operation.</param>
    /// <param name = "inlineResources">The inline resources used by the operation.</param>
    /// <param name = "attachments">The attachments used by the operation.</param>
    /// <param name = "missingRelatedReferences">The missing related references used by the operation.</param>
    /// <param name = "securitySummary">The security summary used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "sourceTextDecodeResult">The source text decode result used by the operation.</param>
    /// <param name = "htmlSanitizeResult">The html sanitize result used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDisplayModel(EmailBodySelection selection, string text, string? html, EmailTextAnalysisResult textAnalysis, List<EmailAttachmentView> inlineResources, List<EmailAttachmentView> attachments, List<EmailRelatedReference> missingRelatedReferences, EmailDisplaySecuritySummary securitySummary, EmailIssueList issues, EmailTextDecodeResult? sourceTextDecodeResult, EmailHtmlSanitizeResult? htmlSanitizeResult)
    {
        if (selection == null)
            throw new ArgumentNullException("selection");
        if (text == null)
            throw new ArgumentNullException("text");
        if (textAnalysis == null)
            throw new ArgumentNullException("textAnalysis");
        if (inlineResources == null)
            throw new ArgumentNullException("inlineResources");
        if (attachments == null)
            throw new ArgumentNullException("attachments");
        if (missingRelatedReferences == null)
            throw new ArgumentNullException("missingRelatedReferences");
        if (securitySummary == null)
            throw new ArgumentNullException("securitySummary");
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.selection = selection;
        this.text = text;
        this.html = html;
        this.textAnalysis = textAnalysis;
        this.inlineResources = new List<EmailAttachmentView>(inlineResources);
        this.attachments = new List<EmailAttachmentView>(attachments);
        this.missingRelatedReferences = new List<EmailRelatedReference>(missingRelatedReferences);
        this.securitySummary = securitySummary;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.sourceTextDecodeResult = sourceTextDecodeResult;
        this.htmlSanitizeResult = htmlSanitizeResult;
    }

    /// <summary>
    /// Gets the body selection.
    /// </summary>
    /// <returns>The current body selection.</returns>
    public EmailBodySelection GetBodySelection()
    {
        return selection;
    }

    /// <summary>
    /// Determines whether the selected body is present or available.
    /// </summary>
    /// <returns><c>true</c> when the selected body is available; otherwise, <c>false</c>.</returns>
    public bool HasSelectedBody()
    {
        return selection.HasBody();
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
    /// Determines whether the HTML is present or available.
    /// </summary>
    /// <returns><c>true</c> when the HTML is available; otherwise, <c>false</c>.</returns>
    public bool HasHtml()
    {
        return html != null;
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    public string? GetHtml()
    {
        return html;
    }

    /// <summary>
    /// Gets the snippet.
    /// </summary>
    /// <returns>The current snippet.</returns>
    public string GetSnippet()
    {
        return textAnalysis.GetSnippet();
    }

    /// <summary>
    /// Gets the text analysis.
    /// </summary>
    /// <returns>The current text analysis.</returns>
    public EmailTextAnalysisResult GetTextAnalysis()
    {
        return textAnalysis;
    }

    /// <summary>
    /// Gets the alternative count.
    /// </summary>
    /// <returns>The current alternative count.</returns>
    public int GetAlternativeCount()
    {
        return selection.GetAlternativeCount();
    }

    /// <summary>
    /// Gets the alternative.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current alternative.</returns>
    public MimeEntity GetAlternative(int index)
    {
        return selection.GetAlternativeAt(index);
    }

    /// <summary>
    /// Gets the alternatives.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current alternatives.</returns>
    public List<MimeEntity> GetAlternatives()
    {
        return selection.GetAlternatives();
    }

    /// <summary>
    /// Gets the inline resource count.
    /// </summary>
    /// <returns>The current inline resource count.</returns>
    public int GetInlineResourceCount()
    {
        return inlineResources.Count;
    }

    /// <summary>
    /// Gets the inline resource.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current inline resource.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAttachmentView GetInlineResource(int index)
    {
        if (index < 0 || index >= inlineResources.Count)
            throw new ArgumentOutOfRangeException("index");
        return inlineResources[index];
    }

    /// <summary>
    /// Gets the inline resources.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current inline resources.</returns>
    public List<EmailAttachmentView> GetInlineResources()
    {
        return new List<EmailAttachmentView>(inlineResources);
    }

    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    public int GetAttachmentCount()
    {
        return attachments.Count;
    }

    /// <summary>
    /// Gets the attachment.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current attachment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailAttachmentView GetAttachment(int index)
    {
        if (index < 0 || index >= attachments.Count)
            throw new ArgumentOutOfRangeException("index");
        return attachments[index];
    }

    /// <summary>
    /// Gets the attachments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attachments.</returns>
    public List<EmailAttachmentView> GetAttachments()
    {
        return new List<EmailAttachmentView>(attachments);
    }

    /// <summary>
    /// Gets the missing related references.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing related references.</returns>
    public List<EmailRelatedReference> GetMissingRelatedReferences()
    {
        return new List<EmailRelatedReference>(missingRelatedReferences);
    }

    /// <summary>
    /// Gets the security summary.
    /// </summary>
    /// <returns>The current security summary.</returns>
    public EmailDisplaySecuritySummary GetSecuritySummary()
    {
        return securitySummary;
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
    /// Gets the warnings.
    /// </summary>
    /// <returns>The current warnings.</returns>
    public EmailIssueList GetWarnings()
    {
        EmailIssueList warnings = new EmailIssueList();
        int index = 0;
        while (index < issues.GetCount())
        {
            if (issues.GetAt(index).GetSeverity() >= EmailIssueSeverity.Warning)
                warnings.Add(issues.GetAt(index));
            index++;
        }

        return warnings;
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
    /// Determines whether the HTML sanitize result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the HTML sanitize result is available; otherwise, <c>false</c>.</returns>
    public bool HasHtmlSanitizeResult()
    {
        return htmlSanitizeResult != null;
    }

    /// <summary>
    /// Gets the HTML sanitize result.
    /// </summary>
    /// <returns>The current HTML sanitize result.</returns>
    public EmailHtmlSanitizeResult? GetHtmlSanitizeResult()
    {
        return htmlSanitizeResult;
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
