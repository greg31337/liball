namespace qrcode;

/// <summary>Contains one named quality measurement, grade, and calibration status.</summary>
public class QrQualityMetric
{
    /// <summary>The stable metric name.</summary>
    public string Name = "";

    /// <summary>The measured numeric value.</summary>
    public double Value;

    /// <summary>The numeric quality grade band.</summary>
    public QrQualityGrade Grade = QrQualityGrade.NotGraded;

    /// <summary>Whether the metric could be calculated from the supplied samples.</summary>
    public bool IsCalculated;

    /// <summary>Whether the measurement used calibrated verifier inputs.</summary>
    public bool UsesCalibratedInputs;

    /// <summary>The display unit, or <c>null</c> for a dimensionless measurement.</summary>
    public string? Unit;

    /// <summary>A measurement limitation, or <c>null</c> when none applies.</summary>
    public string? Limitation;
}
