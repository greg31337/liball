namespace barcode;

/// <summary>
/// Locates barcode candidates in raw images without decoding their payloads.
/// </summary>
public interface IBarcodeDetector
{
    /// <summary>
    /// Detects barcode candidates in an image.
    /// </summary>
    /// <param name="request">The image and localization options.</param>
    /// <returns>The localized candidates and diagnostics.</returns>
    BarcodeDetectionResult DetectBarcodes(BarcodeDetectionRequest request);
}
