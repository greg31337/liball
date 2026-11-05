namespace qrcode;

/// <summary>Samples caller-provided symbol boundaries without running detection or decoding.</summary>
/// <remarks>Corner order defines the output orientation, including reflections. Corners describe module boundaries with no quiet zone. An explicit polarity must be supplied by the detection or options; conflicting explicit polarities are rejected.</remarks>
public interface IQrPerspectiveSampler
{
    /// <summary>Samples the supplied geometry using module centers or multiple votes.</summary>
    /// <param name="image">The raw image, or Gray8 pixels containing only zero and 255 in supplied-binary mode.</param>
    /// <param name="detection">The symbol boundary and positive module dimensions to sample.</param>
    /// <param name="options">The preprocessing, sampling, polarity, and resource options.</param>
    /// <returns>A new detection with independent geometry, sampled values and confidence, and fresh quiet-zone evidence when the profile is known.</returns>
    QrDetection SampleDetection(QrImage image, QrDetection detection, QrDetectionOptions options);

    /// <summary>Resamples geometry after optional bounded refinement against a caller's pattern hypothesis.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The starting boundary and module dimensions; previous sampled values are not reused.</param>
    /// <param name="expectedPatterns">A matrix matching the detection dimensions, with values and roles for finder, timing, alignment, or format evidence. Unknown values and other roles do not influence refinement.</param>
    /// <param name="options">The preprocessing and sampling options; disabling RefineGrid retains the starting geometry.</param>
    /// <returns>A new detection containing the resulting geometry and samples, without modifying any caller data.</returns>
    QrDetection ResampleDetection(QrImage image, QrDetection detection, QrMatrix expectedPatterns, QrDetectionOptions options);
}
