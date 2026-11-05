namespace raw;

/// <summary>Reports deterministic masked-sample row and column banding calibration applied during preprocessing.</summary>
public class RawBandingCorrectionResult
{
    /// <summary>Whether one additive offset was estimated and removed for every stored row.</summary>
    public bool RowCorrectionApplied;
    /// <summary>Whether one additive offset was estimated and removed for every stored column.</summary>
    public bool ColumnCorrectionApplied;
    /// <summary>Estimated additive offsets in stored-row order, or an empty array.</summary>
    public double[] RowOffsets;
    /// <summary>Estimated additive offsets in stored-column order, or an empty array.</summary>
    public double[] ColumnOffsets;
    /// <summary>The number of masked logical samples contributing to calibration.</summary>
    public long CalibrationSampleCount;
    /// <summary>The largest absolute combined offset removed from any sample.</summary>
    public double MaximumAbsoluteCorrection;

    /// <summary>Creates an applied banding-correction result.</summary>
    /// <param name="rowCorrectionApplied">Whether row correction was applied.</param>
    /// <param name="columnCorrectionApplied">Whether column correction was applied.</param>
    /// <param name="rowOffsets">One offset per stored row, or <c>null</c>.</param>
    /// <param name="columnOffsets">One offset per stored column, or <c>null</c>.</param>
    /// <param name="calibrationSampleCount">The masked logical sample count.</param>
    /// <param name="maximumAbsoluteCorrection">The largest absolute combined correction.</param>
    public RawBandingCorrectionResult(bool rowCorrectionApplied, bool columnCorrectionApplied, double[]? rowOffsets, double[]? columnOffsets, long calibrationSampleCount, double maximumAbsoluteCorrection)
    {
        RowCorrectionApplied = rowCorrectionApplied;
        ColumnCorrectionApplied = columnCorrectionApplied;
        RowOffsets = rowOffsets == null ? System.Array.Empty<double>() : rowOffsets;
        ColumnOffsets = columnOffsets == null ? System.Array.Empty<double>() : columnOffsets;
        CalibrationSampleCount = calibrationSampleCount;
        MaximumAbsoluteCorrection = maximumAbsoluteCorrection;
    }

    internal RawValidationReport Validate(int width, int height)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        bool rowGeometry = RowOffsets != null && (RowCorrectionApplied && RowOffsets.Length == height || !RowCorrectionApplied && RowOffsets.Length == 0);
        bool columnGeometry = ColumnOffsets != null && (ColumnCorrectionApplied && ColumnOffsets.Length == width || !ColumnCorrectionApplied && ColumnOffsets.Length == 0);
        bool finiteOffsets = RowOffsets != null && ColumnOffsets != null && RawBlackLevel.AllFinite(RowOffsets) && RawBlackLevel.AllFinite(ColumnOffsets);
        if (width <= 0 || height <= 0 || !rowGeometry || !columnGeometry || !RowCorrectionApplied && !ColumnCorrectionApplied)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "BandingCorrection.Geometry", "Applied row and column flags must match complete image-axis offset arrays.");
        }
        if (!finiteOffsets || CalibrationSampleCount <= 0 || !RawModelValidation.IsFinite(MaximumAbsoluteCorrection) || MaximumAbsoluteCorrection < 0.0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "BandingCorrection.Values", "Banding offsets, sample count, and maximum correction must be finite and valid.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}
