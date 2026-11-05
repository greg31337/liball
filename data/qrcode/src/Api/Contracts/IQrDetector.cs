namespace qrcode;

/// <summary>Detects QR-family symbol geometry in raw images or supplied binary images.</summary>
/// <remarks>Profile and version metadata are estimates, not successful payload decoding. Sampled matrices are left unset.</remarks>
public interface IQrDetector
{
    /// <summary>Detects symbols after luminance conversion and the selected binary preprocessing.</summary>
    /// <param name="image">The caller-owned raw pixel image.</param>
    /// <param name="options">The preprocessing, family, polarity, and resource options.</param>
    /// <returns>Deterministically ordered detections, or an empty set when no symbol is found.</returns>
    QrDetectionSet DetectImage(QrImage image, QrDetectionOptions options);

    /// <summary>Detects symbols in a matrix whose entries represent image pixels.</summary>
    /// <param name="matrix">The caller-owned binary image; every value must be light or dark.</param>
    /// <param name="options">The family, polarity, cleanup, and resource options; the thresholding mode is bypassed.</param>
    /// <returns>Deterministically ordered detections, or an empty set when no symbol is found.</returns>
    QrDetectionSet DetectBinaryMatrix(QrMatrix matrix, QrDetectionOptions options);
}
