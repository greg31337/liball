namespace barcode;

/// <summary>
/// Locates barcode candidates in raw images without decoding their payloads.
/// </summary>
public class BarcodeDetector : IBarcodeDetector
{
    internal BarcodeDispatcher Dispatcher;

    /// <summary>
    /// Creates a barcode detector backed by the shared operation dispatcher.
    /// </summary>
    public BarcodeDetector()
    {
        Dispatcher = new BarcodeDispatcher();
    }

    /// <summary>
    /// Detects barcode candidates in an image.
    /// </summary>
    /// <param name="request">The image and localization options.</param>
    /// <returns>The localized candidates and diagnostics.</returns>
    /// <exception cref="BarcodeDetectionException">Detection is unavailable or the image cannot be processed.</exception>
    public BarcodeDetectionResult DetectBarcodes(BarcodeDetectionRequest request)
    {
        return Dispatcher.DetectBarcodes(request);
    }
}
