namespace qrcode;

/// <summary>Provides reusable stateless QR-family geometry detection.</summary>
public class QrDetector : IQrDetector
{
    /// <summary>Initializes a QR detector.</summary>
    public QrDetector()
    {
    }

    /// <summary>Detects symbol geometry in a raw pixel image without modifying caller memory.</summary>
    /// <param name="image">The raw source image.</param>
    /// <param name="options">The preprocessing, family, polarity, and resource options.</param>
    /// <returns>Detected geometry and preprocessing diagnostics, including an empty set for ordinary no-symbol images.</returns>
    public QrDetectionSet DetectImage(QrImage image, QrDetectionOptions options)
    {
        return DetectorPipeline.DetectImage(image, options);
    }

    /// <summary>Detects symbol geometry in a supplied binary image without modifying caller memory.</summary>
    /// <param name="matrix">The binary image pixels, containing only light or dark values.</param>
    /// <param name="options">The family, polarity, cleanup, and resource options.</param>
    /// <returns>Detected geometry and diagnostics; no thresholding is applied to the supplied binary pixels.</returns>
    public QrDetectionSet DetectBinaryMatrix(QrMatrix matrix, QrDetectionOptions options)
    {
        return DetectorPipeline.DetectBinaryMatrix(matrix, options);
    }
}
