using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlDataUriExtraction"/>.
/// </summary>
public interface IEmailHtmlDataUriExtractionContract
{
    /// <summary>
    /// Gets the original URI.
    /// </summary>
    /// <returns>The current original URI.</returns>
    string GetOriginalUri();
    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    string GetContentId();
    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    EmailMediaType GetMediaType();
    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    long GetDecodedByteCount();
    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    int GetSourceOffset();
    /// <summary>
    /// Gets the related part.
    /// </summary>
    /// <returns>The current related part.</returns>
    MimeEntity GetRelatedPart();
    /// <summary>
    /// Determines whether the operation reused part.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ReusedPart();
}
