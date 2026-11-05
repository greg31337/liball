namespace pptx;

/// <summary>
/// Provides public operations for <see cref="PptxImageCrop"/> values.
/// </summary>
public class PptxImageCropOperations : IPptxImageCropOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxImageCropOperations()
    {
    }

    /// <summary>
    /// Reports whether all crop values form a valid percentage rectangle.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when the crop is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid(PptxImageCrop target)
    {
        return target.IsValid();
    }

    /// <summary>
    /// Returns the visible rectangle after applying the percentage crop to image bounds.
    /// </summary>
    /// <param name="imageBounds">The uncropped image bounds in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The resulting bounds value.</returns>
    public PptxBounds VisibleBounds(PptxImageCrop target, PptxBounds imageBounds)
    {
        return target.VisibleBounds(imageBounds);
    }
}
