namespace barcode;

/// <summary>
/// Holds an encoded symbol and the options needed to render it.
/// </summary>
public class BarcodeRenderRequest
{
    /// <summary>
    /// The encoded logical symbol to render.
    /// </summary>
    public BarcodeSymbol? Symbol;

    /// <summary>
    /// The rendering options.
    /// </summary>
    public BarcodeRenderOptions? Options;
}
