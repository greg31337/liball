namespace pdf;

/// <summary>
/// Configures a reusable ordered page-content layer.
/// </summary>
public interface IPdfContentLayer
{
    /// <summary>
    /// Sets the page-relative region used to measure, draw, clip, and debug the layer.
    /// </summary>
    /// <param name="bounds">The layer region in PDF points.</param>
    void SetBounds(PdfRectangle bounds);

    /// <summary>
    /// Clips layer drawing to its configured region.
    /// </summary>
    void EnableClipping();

    /// <summary>
    /// Allows layer drawing to extend beyond its configured region.
    /// </summary>
    void DisableClipping();
}
