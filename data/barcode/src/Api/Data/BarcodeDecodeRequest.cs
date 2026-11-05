namespace barcode;

/// <summary>
/// Holds an image or pure symbol and the options needed to decode it.
/// </summary>
public class BarcodeDecodeRequest
{
    /// <summary>
    /// The raw image to decode when using the image path.
    /// </summary>
    public BarcodeImage? Image;

    /// <summary>
    /// The sampled logical symbol to decode when using the pure-symbol path.
    /// </summary>
    public BarcodeSymbol? Symbol;

    /// <summary>
    /// The decoding options.
    /// </summary>
    public BarcodeDecodeOptions? Options;
}
