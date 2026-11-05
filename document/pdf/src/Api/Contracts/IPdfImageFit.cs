namespace pdf;

/// <summary>
/// Calculates image placements for common fitting modes.
/// </summary>
public interface IPdfImageFit
{
    /// <summary>
    /// Stretches an image to the supplied box without preserving its aspect ratio.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points.</param>
    /// <param name="y">The vertical coordinate in PDF points.</param>
    /// <param name="width">The box width in PDF points.</param>
    /// <param name="height">The box height in PDF points.</param>
    /// <returns>The resulting image placement.</returns>
    PdfImagePlacement Stretch(double x, double y, double width, double height);

    /// <summary>
    /// Fits an image inside the supplied box while preserving its aspect ratio.
    /// </summary>
    /// <param name="image">The image to fit.</param>
    /// <param name="x">The horizontal coordinate in PDF points.</param>
    /// <param name="y">The vertical coordinate in PDF points.</param>
    /// <param name="width">The box width in PDF points.</param>
    /// <param name="height">The box height in PDF points.</param>
    /// <returns>The centered image placement.</returns>
    PdfImagePlacement PreserveAspectFit(PdfImage image, double x, double y, double width, double height);

    /// <summary>
    /// Fills the supplied box while preserving the image aspect ratio and allowing cropping.
    /// </summary>
    /// <param name="image">The image to fit.</param>
    /// <param name="x">The horizontal coordinate in PDF points.</param>
    /// <param name="y">The vertical coordinate in PDF points.</param>
    /// <param name="width">The box width in PDF points.</param>
    /// <param name="height">The box height in PDF points.</param>
    /// <returns>The centered image placement.</returns>
    PdfImagePlacement CropFill(PdfImage image, double x, double y, double width, double height);
}
