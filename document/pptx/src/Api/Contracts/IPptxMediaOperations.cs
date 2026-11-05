using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Defines audio and video creation, replacement, removal, inventory, and audit operations.
/// </summary>
public interface IPptxMediaOperations
{
    /// <summary>
    /// Adds embedded audio or video to a slide.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="bytes">The media payload.</param>
    /// <param name="contentType">The media content type.</param>
    /// <param name="mediaType">The audio or video media type.</param>
    /// <param name="bounds">The media shape bounds in English Metric Units (EMUs).</param>
    /// <returns>The created media shape.</returns>
    PptxShape AddEmbedded(PptxSlide slide, byte[] bytes, string contentType, string mediaType, PptxBounds bounds);
    /// <summary>
    /// Adds externally linked audio or video to a slide.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="targetUri">The external media target.</param>
    /// <param name="mediaType">The audio or video media type.</param>
    /// <param name="bounds">The media shape bounds in English Metric Units (EMUs).</param>
    /// <returns>The created media shape.</returns>
    PptxShape AddLinked(PptxSlide slide, string targetUri, string mediaType, PptxBounds bounds);
    /// <summary>
    /// Replaces a media shape with embedded content while preserving its geometry.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="bytes">The media payload.</param>
    /// <param name="contentType">The media content type.</param>
    void ReplaceEmbedded(PptxShape shape, byte[] bytes, string contentType);
    /// <summary>
    /// Replaces a media shape with an external target while preserving its geometry.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="targetUri">The external media target.</param>
    void ReplaceLinked(PptxShape shape, string targetUri);
    /// <summary>
    /// Removes a media shape, its relationships, and unreferenced owned parts.
    /// </summary>
    /// <param name="shape">The media shape to remove.</param>
    void Remove(PptxShape shape);
    /// <summary>
    /// Adds or replaces an embedded poster frame on a media shape.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    void SetPosterFrame(PptxShape shape, byte[] bytes, string contentType);
    /// <summary>
    /// Removes the poster-frame relationship and any unreferenced poster part.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    void RemovePosterFrame(PptxShape shape);
    /// <summary>
    /// Returns all audio and video in presentation order.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The media inventory.</returns>
    List<PptxMediaFile> GetInventory(PptxPresentation presentation);
    /// <summary>
    /// Returns all audio and video related to one slide.
    /// </summary>
    /// <param name="slide">The slide to inspect.</param>
    /// <returns>The slide media inventory.</returns>
    List<PptxMediaFile> GetSlideInventory(PptxSlide slide);
    /// <summary>
    /// Audits every externally linked audio and video target.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The external-media audit result.</returns>
    PptxExternalMediaAuditResult AuditExternal(PptxPresentation presentation);
}
