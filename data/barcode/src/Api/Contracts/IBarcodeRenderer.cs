namespace barcode;

/// <summary>
/// Renders logical barcode symbols into raster or vector output.
/// </summary>
public interface IBarcodeRenderer
{
    /// <summary>
    /// Renders an encoded logical symbol.
    /// </summary>
    /// <param name="request">The symbol and rendering options.</param>
    /// <returns>The rendered output and its actual dimensions.</returns>
    BarcodeRenderedImage RenderBarcode(BarcodeRenderRequest request);
}
