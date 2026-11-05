using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlDataUriResult"/>.
/// </summary>
public interface IEmailHtmlDataUriResultContract
{
    /// <summary>
    /// Gets the HTML.
    /// </summary>
    /// <returns>The current HTML.</returns>
    string GetHtml();
    /// <summary>
    /// Gets the related part count.
    /// </summary>
    /// <returns>The current related part count.</returns>
    int GetRelatedPartCount();
    /// <summary>
    /// Gets the related part.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current related part.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    MimeEntity GetRelatedPart(int index);
    /// <summary>
    /// Gets the related parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current related parts.</returns>
    List<MimeEntity> GetRelatedParts();
    /// <summary>
    /// Gets the extraction count.
    /// </summary>
    /// <returns>The current extraction count.</returns>
    int GetExtractionCount();
    /// <summary>
    /// Gets the extraction.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current extraction.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHtmlDataUriExtraction GetExtraction(int index);
    /// <summary>
    /// Gets the extractions.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current extractions.</returns>
    List<EmailHtmlDataUriExtraction> GetExtractions();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the tokenization result.
    /// </summary>
    /// <returns>The current tokenization result.</returns>
    EmailHtmlTokenizationResult GetTokenizationResult();
    /// <summary>
    /// Gets the rejected data URI count.
    /// </summary>
    /// <returns>The current rejected data URI count.</returns>
    int GetRejectedDataUriCount();
    /// <summary>
    /// Gets the extracted byte count.
    /// </summary>
    /// <returns>The current extracted byte count.</returns>
    long GetExtractedByteCount();
    /// <summary>
    /// Determines whether the operation or source value was modified.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasModified();
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
