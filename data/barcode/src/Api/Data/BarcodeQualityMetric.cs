namespace barcode;

/// <summary>
/// Holds one named barcode quality or conformance diagnostic measurement.
/// </summary>
public class BarcodeQualityMetric
{
    /// <summary>
    /// The stable metric name.
    /// </summary>
    public string? Name;

    /// <summary>
    /// The observed metric value.
    /// </summary>
    public double Value;

    /// <summary>
    /// The comparison threshold when one applies.
    /// </summary>
    public double Threshold;

    /// <summary>
    /// Indicates whether the observed value meets the threshold.
    /// </summary>
    public bool MeetsThreshold;

    /// <summary>
    /// Indicates whether the metric is heuristic rather than a certified grade.
    /// </summary>
    public bool IsHeuristic;

    /// <summary>
    /// The human-readable interpretation of the metric.
    /// </summary>
    public string? Interpretation;
}
