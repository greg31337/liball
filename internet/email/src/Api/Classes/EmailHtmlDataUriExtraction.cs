using System;

namespace email;
/// <summary>
/// Represents a HTML data URI extraction.
/// </summary>
public class EmailHtmlDataUriExtraction : IEmailHtmlDataUriExtractionContract
{
    string originalUri;
    string contentId;
    EmailMediaType mediaType;
    long decodedByteCount;
    int sourceOffset;
    MimeEntity relatedPart;
    bool reusedPart;
    /// <summary>
    /// Creates a new HTML data URI extraction.
    /// </summary>
    /// <param name = "originalUri">The original URI used by the operation.</param>
    /// <param name = "contentId">The content ID used by the operation.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "decodedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceOffset">The zero-based source offset.</param>
    /// <param name = "relatedPart">The related part used by the operation.</param>
    /// <param name = "reusedPart">Whether to enable the reused part behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHtmlDataUriExtraction(string originalUri, string contentId, EmailMediaType mediaType, long decodedByteCount, int sourceOffset, MimeEntity relatedPart, bool reusedPart)
    {
        if (originalUri == null || originalUri.Length == 0)
            throw new ArgumentException("The original data URI is required.", "originalUri");
        if (contentId == null || contentId.Length == 0)
            throw new ArgumentException("The generated content ID is required.", "contentId");
        if (mediaType == null)
            throw new ArgumentNullException("mediaType");
        if (decodedByteCount < 0)
            throw new ArgumentOutOfRangeException("decodedByteCount");
        if (sourceOffset < 0)
            throw new ArgumentOutOfRangeException("sourceOffset");
        if (relatedPart == null)
            throw new ArgumentNullException("relatedPart");
        this.originalUri = originalUri;
        this.contentId = contentId;
        this.mediaType = mediaType;
        this.decodedByteCount = decodedByteCount;
        this.sourceOffset = sourceOffset;
        this.relatedPart = relatedPart;
        this.reusedPart = reusedPart;
    }

    /// <summary>
    /// Gets the original URI.
    /// </summary>
    /// <returns>The current original URI.</returns>
    public string GetOriginalUri()
    {
        return originalUri;
    }

    /// <summary>
    /// Gets the content ID.
    /// </summary>
    /// <returns>The current content ID.</returns>
    public string GetContentId()
    {
        return contentId;
    }

    /// <summary>
    /// Gets the media type.
    /// </summary>
    /// <returns>The current media type.</returns>
    public EmailMediaType GetMediaType()
    {
        return mediaType;
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedByteCount;
    }

    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    public int GetSourceOffset()
    {
        return sourceOffset;
    }

    /// <summary>
    /// Gets the related part.
    /// </summary>
    /// <returns>The current related part.</returns>
    public MimeEntity GetRelatedPart()
    {
        return relatedPart;
    }

    /// <summary>
    /// Determines whether the operation reused part.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ReusedPart()
    {
        return reusedPart;
    }
}
