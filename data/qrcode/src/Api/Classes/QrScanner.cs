namespace qrcode;

/// <summary>Provides reusable stateless QR-family image scanning and structured-append assembly.</summary>
public class QrScanner : IQrScanner
{
    /// <summary>Initializes a QR scanner.</summary>
    public QrScanner()
    {
    }

    /// <summary>Scans a raw pixel image without modifying caller memory.</summary>
    /// <param name="image">The raw source image.</param>
    /// <param name="detection">The preprocessing, family, polarity, sampling, and image limits.</param>
    /// <param name="decoding">The payload policy and per-candidate recovery and decode limits.</param>
    /// <param name="retainFailures">Whether to retain failed candidate attempts and their diagnostics.</param>
    /// <returns>Decoded symbols and assembly outputs, or an empty successful set for ordinary no-symbol images.</returns>
    /// <exception cref="QrInputException">The input or options are invalid, including supplied-binary mode.</exception>
    /// <exception cref="QrResourceLimitException">The input or aggregate result exceeds a configured resource limit.</exception>
    public QrDecodeSet ScanImage(QrImage image, QrDetectionOptions detection, QrDecodeOptions decoding, bool retainFailures)
    {
        return ImageScanPipeline.ScanImage(image, detection, decoding, retainFailures);
    }

    /// <summary>Scans supplied binary image pixels without applying luminance thresholding or modifying caller memory.</summary>
    /// <param name="matrix">The binary image pixels, containing only light or dark values.</param>
    /// <param name="detection">The family, polarity, cleanup, sampling, and image limits.</param>
    /// <param name="decoding">The payload policy and per-candidate recovery and decode limits.</param>
    /// <param name="retainFailures">Whether to retain failed candidate attempts and their diagnostics.</param>
    /// <returns>Decoded symbols and assembly outputs, or an empty successful set for ordinary no-symbol images.</returns>
    /// <exception cref="QrInputException">The input or options are invalid.</exception>
    /// <exception cref="QrResourceLimitException">The input or aggregate result exceeds a configured resource limit.</exception>
    public QrDecodeSet ScanBinaryMatrix(QrMatrix matrix, QrDetectionOptions detection, QrDecodeOptions decoding, bool retainFailures)
    {
        return ImageScanPipeline.ScanBinaryMatrix(matrix, detection, decoding, retainFailures);
    }
}
