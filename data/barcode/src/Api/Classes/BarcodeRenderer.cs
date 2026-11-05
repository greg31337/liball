namespace barcode;

/// <summary>
/// Renders logical barcode symbols into raster or vector output.
/// </summary>
public class BarcodeRenderer : IBarcodeRenderer
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a barcode renderer backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeRenderer()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Renders an encoded logical symbol.
    /// </summary>
    /// <param name="request">The symbol and rendering options.</param>
    /// <returns>The rendered output and its actual dimensions.</returns>
    /// <exception cref="BarcodeRenderingException">Rendering is unavailable or the request cannot be rendered.</exception>
    public BarcodeRenderedImage RenderBarcode(BarcodeRenderRequest request)
    {
        return Dispatcher.RenderBarcode(request);
    }
}
