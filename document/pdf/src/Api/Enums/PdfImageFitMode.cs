namespace pdf;

/// <summary>
/// Specifies how an image is placed inside its layout box.
/// </summary>
public enum PdfImageFitMode
{
    /// <summary>
    /// Stretches the image to the complete layout box.
    /// </summary>
    Stretch = 0,
    /// <summary>
    /// Preserves aspect ratio while fitting completely inside the layout box.
    /// </summary>
    PreserveAspectFit = 1,
    /// <summary>
    /// Preserves aspect ratio while filling and clipping to the layout box.
    /// </summary>
    CropFill = 2
}
