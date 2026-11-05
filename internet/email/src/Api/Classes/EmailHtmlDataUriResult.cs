using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a HTML data URI operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHtmlDataUriResult : IEmailHtmlDataUriResultContract
{
    string html;
    List<MimeEntity> relatedParts;
    List<EmailHtmlDataUriExtraction> extractions;
    EmailIssueList issues;
    EmailHtmlTokenizationResult tokenization;
    int rejectedDataUriCount;
    long extractedByteCount;
    EmailTextDecodeResult? sourceTextDecodeResult;
    /// <summary>
    /// Creates a new HTML data URI result.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "relatedParts">The related parts used by the operation.</param>
    /// <param name = "extractions">The extractions used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "tokenization">The tokenization used by the operation.</param>
    /// <param name = "rejectedDataUriCount">The number of items or bytes to process.</param>
    /// <param name = "extractedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceTextDecodeResult">The source text decode result used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlDataUriResult(string html, List<MimeEntity> relatedParts, List<EmailHtmlDataUriExtraction> extractions, EmailIssueList issues, EmailHtmlTokenizationResult tokenization, int rejectedDataUriCount, long extractedByteCount, EmailTextDecodeResult? sourceTextDecodeResult)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (relatedParts == null)
            throw new ArgumentNullException("relatedParts");
        if (extractions == null)
            throw new ArgumentNullException("extractions");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (tokenization == null)
            throw new ArgumentNullException("tokenization");
        if (rejectedDataUriCount < 0)
            throw new ArgumentOutOfRangeException("rejectedDataUriCount");
        if (extractedByteCount < 0)
            throw new ArgumentOutOfRangeException("extractedByteCount");
        this.html = html;
        this.relatedParts = new List<MimeEntity>(relatedParts);
        this.extractions = new List<EmailHtmlDataUriExtraction>(extractions);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.tokenization = tokenization;
        this.rejectedDataUriCount = rejectedDataUriCount;
        this.extractedByteCount = extractedByteCount;
        this.sourceTextDecodeResult = sourceTextDecodeResult;
    }

    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    public string GetHtml()
    {
        return html;
    }

    /// <summary>
    /// Gets the related part count.
    /// </summary>
    /// <returns>The current related part count.</returns>
    public int GetRelatedPartCount()
    {
        return relatedParts.Count;
    }

    /// <summary>
    /// Gets the related part.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current related part.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public MimeEntity GetRelatedPart(int index)
    {
        if (index < 0 || index >= relatedParts.Count)
            throw new ArgumentOutOfRangeException("index");
        return relatedParts[index];
    }

    /// <summary>
    /// Gets the related parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current related parts.</returns>
    public List<MimeEntity> GetRelatedParts()
    {
        return new List<MimeEntity>(relatedParts);
    }

    /// <summary>
    /// Gets the extraction count.
    /// </summary>
    /// <returns>The current extraction count.</returns>
    public int GetExtractionCount()
    {
        return extractions.Count;
    }

    /// <summary>
    /// Gets the extraction.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current extraction.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlDataUriExtraction GetExtraction(int index)
    {
        if (index < 0 || index >= extractions.Count)
            throw new ArgumentOutOfRangeException("index");
        return extractions[index];
    }

    /// <summary>
    /// Gets the extractions.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extractions.</returns>
    public List<EmailHtmlDataUriExtraction> GetExtractions()
    {
        return new List<EmailHtmlDataUriExtraction>(extractions);
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
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    public EmailHtmlTokenizationResult GetTokenizationResult()
    {
        return tokenization;
    }

    /// <summary>
    /// Gets the rejected data URI count.
    /// </summary>
    /// <returns>The current rejected data URI count.</returns>
    public int GetRejectedDataUriCount()
    {
        return rejectedDataUriCount;
    }

    /// <summary>
    /// Gets the extracted byte count.
    /// </summary>
    /// <returns>The current extracted byte count.</returns>
    public long GetExtractedByteCount()
    {
        return extractedByteCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was modified.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasModified()
    {
        return extractions.Count > 0;
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
