namespace chart;
/// <summary>Defines rotated bounds and corners for a text layout.</summary>
public interface IChartTextRotationPlan
{
    /// <summary>Returns the rotation angle in radians.</summary>
    /// <returns>The angle in radians.</returns>
    double GetAngleRadians();
    /// <summary>Returns the unrotated bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    ChartRect GetUnrotatedBounds();
    /// <summary>Returns the rotated axis-aligned bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    ChartRect GetRotatedBounds();
    /// <summary>Returns the four rotated corners.</summary>
    /// <returns>Independent corner copies.</returns>
    ChartPoint[] GetCorners();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied rotation plan.</returns>
    ChartTextRotationPlan Copy();
}
