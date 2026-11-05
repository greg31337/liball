namespace qrcode;

/// <summary>Analyzes logical matrices and image observations without claiming verifier certification.</summary>
public interface IQrQualityAnalyzer
{
    /// <summary>Decodes and analyzes a matrix, preserving observed damage after orientation normalization.</summary>
    /// <param name="matrix">The observed modules, optional confidence, and explicit quiet-zone extents.</param>
    /// <param name="options">The permitted profiles, measurements, and resource limit.</param>
    /// <returns>An uncalibrated report; measurements requiring image evidence remain unavailable.</returns>
    /// <exception cref="QrDecodeException">A verified symbol and payload cannot be recovered.</exception>
    QrQualityReport AnalyzeMatrixQuality(QrMatrix matrix, QrQualityOptions options);

    /// <summary>Samples one supplied symbol boundary and analyzes its original luminance and binary evidence.</summary>
    /// <param name="image">The raw image or supplied binary image.</param>
    /// <param name="detection">The symbol boundary, dimensions, and explicit polarity; cached samples are ignored.</param>
    /// <param name="sampling">The preprocessing, polarity, and image resource options.</param>
    /// <param name="options">The quality measurements, aperture, sample count, and permitted profiles.</param>
    /// <returns>An uncalibrated report. Geometry measurements describe the supplied boundary estimate.</returns>
    /// <exception cref="QrDecodeException">The sampled symbol cannot be decoded.</exception>
    /// <exception cref="QrInputException">The geometry, aperture, or required luminance samples are invalid.</exception>
    QrQualityReport AnalyzeImageQuality(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrQualityOptions options);
}
