namespace qrcode;

/// <summary>Provides stateless matrix and image-derived QR quality reports.</summary>
public class QrQualityAnalyzer : IQrQualityAnalyzer
{
    /// <summary>Initializes a stateless quality analyzer.</summary>
    public QrQualityAnalyzer()
    {
    }

    /// <summary>Measures observed matrix damage, quiet-zone evidence, and actual error-correction usage.</summary>
    /// <param name="matrix">The observed matrix, including any declared quiet zone.</param>
    /// <param name="options">The quality policy and sample limit.</param>
    /// <returns>A new report with unavailable image measurements explicitly identified.</returns>
    /// <exception cref="QrDecodeException">The observed symbol cannot be decoded.</exception>
    public QrQualityReport AnalyzeMatrixQuality(QrMatrix matrix, QrQualityOptions options)
    {
        QualityAnalysisState state = QualityAnalysisPipeline.AnalyzeMatrix(matrix, options);
        return QualityAnalysisPipeline.Complete(state.Report, options);
    }

    /// <summary>Measures a freshly sampled detection using original luminance and supplied geometry.</summary>
    /// <param name="image">The source pixel buffer.</param>
    /// <param name="detection">The symbol boundary and dimensions; retained samples are ignored.</param>
    /// <param name="sampling">The image preprocessing and resource options.</param>
    /// <param name="options">The quality policy, aperture, and sample limit.</param>
    /// <returns>A new report without calibrated grades or verifier certification.</returns>
    /// <exception cref="QrDecodeException">The sampled symbol cannot be decoded.</exception>
    /// <exception cref="QrInputException">The input geometry or sampling configuration is invalid.</exception>
    public QrQualityReport AnalyzeImageQuality(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrQualityOptions options)
    {
        return QualityImageAnalysis.Analyze(image, detection, sampling, options);
    }
}
