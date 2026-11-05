namespace qrcode;

/// <summary>Scans raw or binary images for decoded QR-family symbols and structured-append sequences.</summary>
/// <remarks>Ordinary images without symbols return empty successful sets. Expected candidate decode failures do not stop scanning. Invalid input and limits that reject the input before scanning throw exceptions.</remarks>
public interface IQrScanner
{
    /// <summary>Detects, samples, and decodes symbols in a raw pixel image.</summary>
    /// <param name="image">The caller-owned raw image, which is not modified.</param>
    /// <param name="detection">The preprocessing, family, polarity, sampling, and image limits. Supplied-binary mode requires <see cref="ScanBinaryMatrix"/>.</param>
    /// <param name="decoding">The payload policy and per-candidate recovery and decode limits.</param>
    /// <param name="retainFailures">Whether to retain failed candidate attempts and their diagnostics.</param>
    /// <returns>Spatially ordered decoded symbols, structured-append outputs, and scan diagnostics; the symbol arrays may be empty.</returns>
    QrDecodeSet ScanImage(QrImage image, QrDetectionOptions detection, QrDecodeOptions decoding, bool retainFailures);

    /// <summary>Detects, samples, and decodes symbols in supplied binary image pixels.</summary>
    /// <param name="matrix">The caller-owned image pixels, containing only light or dark values. Module metadata is ignored and caller buffers are not modified.</param>
    /// <param name="detection">The family, polarity, cleanup, sampling, and image limits. Thresholding and contrast normalization are bypassed.</param>
    /// <param name="decoding">The payload policy and per-candidate recovery and decode limits.</param>
    /// <param name="retainFailures">Whether to retain failed candidate attempts and their diagnostics.</param>
    /// <returns>Spatially ordered decoded symbols, structured-append outputs, and scan diagnostics; the symbol arrays may be empty.</returns>
    QrDecodeSet ScanBinaryMatrix(QrMatrix matrix, QrDetectionOptions detection, QrDecodeOptions decoding, bool retainFailures);
}
