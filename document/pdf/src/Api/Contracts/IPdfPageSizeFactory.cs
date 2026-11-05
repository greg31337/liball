namespace pdf;

/// <summary>
/// Creates standard, custom, portrait, and landscape PDF page dimensions.
/// </summary>
public interface IPdfPageSizeFactory
{
    /// <summary>
    /// Creates and returns a page dimensions configured for letter.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Letter();

    /// <summary>
    /// Creates and returns a page dimensions configured for legal.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Legal();

    /// <summary>
    /// Creates and returns a page dimensions configured for a3.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions A3();

    /// <summary>
    /// Creates and returns a page dimensions configured for a4.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions A4();

    /// <summary>
    /// Creates and returns a page dimensions configured for a5.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions A5();

    /// <summary>
    /// Creates and returns a page dimensions configured for receipt80.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Receipt80();

    /// <summary>
    /// Creates and returns a page dimensions configured for receipt58.
    /// </summary>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Receipt58();

    /// <summary>
    /// Creates and returns a page dimensions configured for custom.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Custom(double width, double height);

    /// <summary>
    /// Creates and returns a page dimensions configured for portrait.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Portrait(PdfPageDimensions size);

    /// <summary>
    /// Creates and returns a page dimensions configured for landscape.
    /// </summary>
    /// <param name="size">The size to use for this operation.</param>
    /// <returns>The resulting page dimensions.</returns>
    PdfPageDimensions Landscape(PdfPageDimensions size);
}

