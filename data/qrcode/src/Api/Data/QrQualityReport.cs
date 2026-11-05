namespace qrcode;

/// <summary>Contains QR quality measurements and explicitly states verifier limitations.</summary>
public class QrQualityReport
{
    /// <summary>The QR family whose quality was analyzed.</summary>
    public QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    /// <summary>The named quality measurements in stable report order.</summary>
    public QrQualityMetric[] Metrics = new QrQualityMetric[0];

    /// <summary>The overall numeric grade, or <see cref="QrQualityGrade.NotGraded"/>.</summary>
    public QrQualityGrade OverallGrade = QrQualityGrade.NotGraded;

    /// <summary>Whether calibrated verifier inputs were supplied for every graded metric.</summary>
    public bool UsesCalibratedInputs;

    /// <summary>Whether this report claims certification from a calibrated ISO verifier.</summary>
    /// <remarks>Library-calculated reports do not claim verifier certification.</remarks>
    public bool IsVerifierCertified;

    /// <summary>The number of luminance or module samples used by the report.</summary>
    public int SampleCount;

    /// <summary>The logical validation result considered during grading, or <c>null</c> when unavailable.</summary>
    public QrValidationResult? Validation;

    /// <summary>Report-wide grading and calibration limitations.</summary>
    public string[] Limitations = new string[0];
}
