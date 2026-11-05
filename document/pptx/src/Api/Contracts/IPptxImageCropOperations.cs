namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxImageCropOperations"/>.
/// </summary>
public interface IPptxImageCropOperations
{
    /// <summary>
    /// Reports whether all crop values form a valid percentage rectangle.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when the crop is valid; otherwise, <c>false</c>.</returns>
    bool IsValid(PptxImageCrop target);
    /// <summary>
    /// Returns the visible rectangle after applying the percentage crop to image bounds.
    /// </summary>
    /// <param name="imageBounds">The uncropped image bounds in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The resulting bounds value.</returns>
    PptxBounds VisibleBounds(PptxImageCrop target, PptxBounds imageBounds);
}
