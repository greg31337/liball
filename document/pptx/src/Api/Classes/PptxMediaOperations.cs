using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Provides audio and video creation, replacement, removal, inventory, and audit operations.
/// </summary>
public class PptxMediaOperations : IPptxMediaOperations
{
    /// <summary>
    /// Creates a media operations instance.
    /// </summary>
    public PptxMediaOperations()
    {
    }

    /// <summary>
    /// Adds embedded audio or video to a slide.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="bytes">The media payload.</param>
    /// <param name="contentType">The media content type.</param>
    /// <param name="mediaType">The audio or video media type.</param>
    /// <param name="bounds">The media shape bounds in English Metric Units (EMUs).</param>
    /// <returns>The created media shape.</returns>
    public PptxShape AddEmbedded(PptxSlide slide, byte[] bytes, string contentType, string mediaType, PptxBounds bounds)
    {
        return GetSlidePresentation(slide).AddMediaShapeToSlide(slide, bytes, contentType, mediaType, bounds);
    }

    /// <summary>
    /// Adds externally linked audio or video to a slide.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="targetUri">The external media target.</param>
    /// <param name="mediaType">The audio or video media type.</param>
    /// <param name="bounds">The media shape bounds in English Metric Units (EMUs).</param>
    /// <returns>The created media shape.</returns>
    public PptxShape AddLinked(PptxSlide slide, string targetUri, string mediaType, PptxBounds bounds)
    {
        return GetSlidePresentation(slide).AddLinkedMediaShapeToSlide(slide, targetUri, mediaType, bounds);
    }

    /// <summary>
    /// Replaces a media shape with embedded content while preserving its geometry.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="bytes">The media payload.</param>
    /// <param name="contentType">The media content type.</param>
    public void ReplaceEmbedded(PptxShape shape, byte[] bytes, string contentType)
    {
        GetShapePresentation(shape).ReplaceMediaEmbedded(shape, bytes, contentType);
    }

    /// <summary>
    /// Replaces a media shape with an external target while preserving its geometry.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="targetUri">The external media target.</param>
    public void ReplaceLinked(PptxShape shape, string targetUri)
    {
        GetShapePresentation(shape).ReplaceMediaLinked(shape, targetUri);
    }

    /// <summary>
    /// Removes a media shape, its relationships, and unreferenced owned parts.
    /// </summary>
    /// <param name="shape">The media shape to remove.</param>
    public void Remove(PptxShape shape)
    {
        GetShapePresentation(shape).RemoveMedia(shape);
    }

    /// <summary>
    /// Adds or replaces an embedded poster frame on a media shape.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    public void SetPosterFrame(PptxShape shape, byte[] bytes, string contentType)
    {
        GetShapePresentation(shape).SetMediaPosterFrame(shape, bytes, contentType);
    }

    /// <summary>
    /// Removes the poster-frame relationship and any unreferenced poster part.
    /// </summary>
    /// <param name="shape">The media shape to modify.</param>
    public void RemovePosterFrame(PptxShape shape)
    {
        GetShapePresentation(shape).RemoveMediaPosterFrame(shape);
    }

    /// <summary>
    /// Returns all audio and video in presentation order.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The media inventory.</returns>
    public List<PptxMediaFile> GetInventory(PptxPresentation presentation)
    {
        return presentation.GetMedia();
    }

    /// <summary>
    /// Returns all audio and video related to one slide.
    /// </summary>
    /// <param name="slide">The slide to inspect.</param>
    /// <returns>The slide media inventory.</returns>
    public List<PptxMediaFile> GetSlideInventory(PptxSlide slide)
    {
        return GetSlidePresentation(slide).GetSlideMedia(slide);
    }

    /// <summary>
    /// Audits every externally linked audio and video target.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The external-media audit result.</returns>
    public PptxExternalMediaAuditResult AuditExternal(PptxPresentation presentation)
    {
        return presentation.AuditExternalMedia();
    }

    internal static PptxPresentation GetSlidePresentation(PptxSlide slide)
    {
        if (slide == null)
        {
            throw new System.ArgumentNullException("slide");
        }

        if (slide.Presentation == null)
        {
            throw new System.ArgumentException("Slide presentation is required.");
        }

        return slide.Presentation;
    }

    internal static PptxPresentation GetShapePresentation(PptxShape shape)
    {
        if (shape == null)
        {
            throw new System.ArgumentNullException("shape");
        }

        return GetSlidePresentation(shape.Tree.Slide);
    }
}
