namespace pptx;

/// <summary>
/// Defines creation, replacement, deduplication, and layout operations for pictures.
/// </summary>
public interface IPptxImageOperations
{
    /// <summary>
    /// Adds an embedded picture using the caller-supplied content type.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <param name="bounds">The picture bounds in English Metric Units (EMUs).</param>
    /// <returns>The created picture shape.</returns>
    PptxShape AddEmbedded(PptxSlide slide, byte[] bytes, string contentType, PptxBounds bounds);
    /// <summary>
    /// Adds a picture linked to an external target.
    /// </summary>
    /// <param name="slide">The slide to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    /// <param name="bounds">The picture bounds in English Metric Units (EMUs).</param>
    /// <returns>The created picture shape.</returns>
    PptxShape AddLinked(PptxSlide slide, string targetUri, PptxBounds bounds);
    /// <summary>
    /// Replaces one picture shape with embedded image data.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    void ReplaceShapeEmbedded(PptxShape shape, byte[] bytes, string contentType);
    /// <summary>
    /// Replaces one picture shape with an external image target.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    void ReplaceShapeLinked(PptxShape shape, string targetUri);
    /// <summary>
    /// Replaces every picture using a slide relationship with embedded image data.
    /// </summary>
    /// <param name="slide">The slide containing the relationship.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    void ReplaceRelationshipEmbedded(PptxSlide slide, string relationshipId, byte[] bytes, string contentType);
    /// <summary>
    /// Replaces every picture using a slide relationship with an external target.
    /// </summary>
    /// <param name="slide">The slide containing the relationship.</param>
    /// <param name="relationshipId">The image relationship identifier.</param>
    /// <param name="targetUri">The external image target.</param>
    void ReplaceRelationshipLinked(PptxSlide slide, string relationshipId, string targetUri);
    /// <summary>
    /// Replaces every image relationship in a presentation with one embedded image.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="bytes">The image payload.</param>
    /// <param name="contentType">The image content type.</param>
    /// <returns>The number of image relationships replaced.</returns>
    int ReplaceAllEmbedded(PptxPresentation presentation, byte[] bytes, string contentType);
    /// <summary>
    /// Replaces every image relationship in a presentation with one external image target.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <param name="targetUri">The external image target.</param>
    /// <returns>The number of image relationships replaced.</returns>
    int ReplaceAllLinked(PptxPresentation presentation, string targetUri);
    /// <summary>
    /// Retargets identical embedded image parts and removes their duplicates.
    /// </summary>
    /// <param name="presentation">The presentation to modify.</param>
    /// <returns>The number of duplicate parts removed.</returns>
    int Deduplicate(PptxPresentation presentation);
    /// <summary>
    /// Applies typed stretch insets to a picture.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="stretch">The stretch settings.</param>
    void SetStretch(PptxShape shape, PptxFillStretch stretch);
    /// <summary>
    /// Returns typed stretch insets from a picture.
    /// </summary>
    /// <param name="shape">The picture shape to inspect.</param>
    /// <returns>The stretch settings.</returns>
    PptxFillStretch GetStretch(PptxShape shape);
    /// <summary>
    /// Applies typed tile settings to a picture.
    /// </summary>
    /// <param name="shape">The picture shape to modify.</param>
    /// <param name="tile">The tile settings.</param>
    void SetTile(PptxShape shape, PptxFillTile tile);
    /// <summary>
    /// Returns typed tile settings from a picture.
    /// </summary>
    /// <param name="shape">The picture shape to inspect.</param>
    /// <returns>The tile settings.</returns>
    PptxFillTile GetTile(PptxShape shape);
}
