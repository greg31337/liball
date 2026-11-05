namespace qrcode;

/// <summary>Provides reusable stateless perspective sampling and grid refinement.</summary>
public class QrPerspectiveSampler : IQrPerspectiveSampler
{
    /// <summary>Initializes a perspective sampler.</summary>
    public QrPerspectiveSampler()
    {
    }

    /// <summary>Samples caller-provided geometry without changing its orientation or refining its grid.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The boundary, dimensions, and polarity hypothesis.</param>
    /// <param name="options">The preprocessing, sampling, and resource options.</param>
    /// <returns>A new detection with independent samples, geometry, and quiet-zone evidence.</returns>
    public QrDetection SampleDetection(QrImage image, QrDetection detection, QrDetectionOptions options)
    {
        return PerspectiveSamplingPipeline.Sample(image, detection, null, options);
    }

    /// <summary>Refines and resamples a detection against explicit pattern evidence.</summary>
    /// <param name="image">The raw or supplied-binary source image.</param>
    /// <param name="detection">The starting boundary, dimensions, and polarity hypothesis.</param>
    /// <param name="expectedPatterns">The expected module values and roles in the same orientation and dimensions as the detection.</param>
    /// <param name="options">The preprocessing, refinement, sampling, and resource options.</param>
    /// <returns>A new detection with resulting geometry and samples, leaving caller data unchanged.</returns>
    public QrDetection ResampleDetection(QrImage image, QrDetection detection, QrMatrix expectedPatterns, QrDetectionOptions options)
    {
        if (expectedPatterns == null)
        {
            throw new QrInputException("Pattern evidence is required for resampling.", "expectedPatterns");
        }

        return PerspectiveSamplingPipeline.Sample(image, detection, expectedPatterns, options);
    }
}
