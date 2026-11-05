namespace pptx;

/// <summary>
/// Provides creation, replacement, deduplication, and layout operations for pictures.
/// </summary>
public class PptxImageOperations : IPptxImageOperations
{
    /// <summary>
    /// Creates an image operations instance.
    /// </summary>
    public PptxImageOperations()
    {
    }

    /// <summary>
    /// Adds an embedded picture using the caller-supplied content type.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <param name="bounds">The picture bounds in English Metric Units (EMUs).</param>
    /// <returns>The created picture shape.</returns>
    public PptxShape AddEmbedded(PptxSlide slide, byte[] bytes, string contentType, PptxBounds bounds)
    {
        return GetSlidePresentation(slide).AddImageWithContentType(slide, bytes, contentType, bounds);
    }

    /// <summary>
    /// Adds a picture linked to an external target.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    /// <param name="bounds">The picture bounds in English Metric Units (EMUs).</param>
    /// <returns>The created picture shape.</returns>
    public PptxShape AddLinked(PptxSlide slide, string targetUri, PptxBounds bounds)
    {
        return GetSlidePresentation(slide).AddLinkedImage(slide, targetUri, bounds);
    }

    /// <summary>
    /// Replaces one picture shape with embedded image data.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    public void ReplaceShapeEmbedded(PptxShape shape, byte[] bytes, string contentType)
    {
        GetShapePresentation(shape).ReplaceImageWithContentType(shape, bytes, contentType);
    }

    /// <summary>
    /// Replaces one picture shape with an external image target.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    public void ReplaceShapeLinked(PptxShape shape, string targetUri)
    {
        GetShapePresentation(shape).ReplaceImageWithLink(shape, targetUri);
    }

    /// <summary>
    /// Replaces every picture using a slide relationship with embedded image data.
    /// </summary>
    /// <param name="slide">The slide containing the relationship.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    public void ReplaceRelationshipEmbedded(PptxSlide slide, string relationshipId, byte[] bytes, string contentType)
    {
        GetSlidePresentation(slide).ReplaceImageRelationship(slide, relationshipId, bytes, contentType);
    }

    /// <summary>
    /// Replaces every picture using a slide relationship with an external target.
    /// </summary>
    /// <param name="slide">The slide containing the relationship.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <param name="targetUri">The external image target.</param>
    public void ReplaceRelationshipLinked(PptxSlide slide, string relationshipId, string targetUri)
    {
        GetSlidePresentation(slide).ReplaceImageRelationshipWithLink(slide, relationshipId, targetUri);
    }

    /// <summary>
    /// Replaces every image relationship in a presentation with one embedded image.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <returns>The number of image relationships replaced.</returns>
    public int ReplaceAllEmbedded(PptxPresentation presentation, byte[] bytes, string contentType)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        return presentation.ReplaceAllImages(bytes, contentType);
    }

    /// <summary>
    /// Replaces every image relationship in a presentation with one external image target.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    /// <returns>The number of image relationships replaced.</returns>
    public int ReplaceAllLinked(PptxPresentation presentation, string targetUri)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        return presentation.ReplaceAllImagesWithLink(targetUri);
    }

    /// <summary>
    /// Retargets identical embedded image parts and removes their duplicates.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <returns>The number of duplicate parts removed.</returns>
    public int Deduplicate(PptxPresentation presentation)
    {
        return presentation.DeduplicateImages();
    }

    /// <summary>
    /// Applies typed stretch insets to a picture.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="stretch">The stretch settings.</param>
    public void SetStretch(PptxShape shape, PptxFillStretch stretch)
    {
        shape.SetImageStretchSettings(stretch);
    }

    /// <summary>
    /// Returns typed stretch insets from a picture.
    /// </summary>
    /// <param name="shape">The picture shape to inspect.</param>
    /// <returns>The stretch settings.</returns>
    public PptxFillStretch GetStretch(PptxShape shape)
    {
        return shape.GetImageStretchSettings();
    }

    /// <summary>
    /// Applies typed tile settings to a picture.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="tile">The tile settings.</param>
    public void SetTile(PptxShape shape, PptxFillTile tile)
    {
        shape.SetImageTileSettings(tile);
    }

    /// <summary>
    /// Returns typed tile settings from a picture.
    /// </summary>
    /// <param name="shape">The picture shape to inspect.</param>
    /// <returns>The tile settings.</returns>
    public PptxFillTile GetTile(PptxShape shape)
    {
        return shape.GetImageTileSettings();
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
