namespace barcode;

/// <summary>
/// Holds an image and the options needed for localization-only barcode detection.
/// </summary>
public class BarcodeDetectionRequest
{
    /// <summary>
    /// The raw image to inspect.
    /// </summary>
    public BarcodeImage? Image;

    /// <summary>
    /// The localization-only detection options.
    /// </summary>
    public BarcodeDetectionOptions? Options;
}
