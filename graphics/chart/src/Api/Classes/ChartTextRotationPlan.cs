namespace chart;
/// <summary>Represents rotated bounds and corners for a text layout.</summary>
public class ChartTextRotationPlan : IChartTextRotationPlan
{
    internal double AngleRadians;
    internal ChartRect UnrotatedBounds;
    internal ChartRect RotatedBounds;
    internal ChartPoint[] Corners;

    /// <summary>Initializes a text rotation plan.</summary>
    /// <param name = "angleRadians">The finite rotation angle in radians.</param>
    /// <param name = "unrotatedBounds">The unrotated layout bounds.</param>
    /// <param name = "rotatedBounds">The rotated axis-aligned bounds.</param>
    /// <param name = "corners">The four rotated corners.</param>
    public ChartTextRotationPlan(double angleRadians, ChartRect unrotatedBounds, ChartRect rotatedBounds, ChartPoint[] corners)
    {
        ChartLinearScale.ValidateFinite(angleRadians, "angleRadians");
        ChartAxis.ValidatePlotArea(unrotatedBounds);
        ChartAxis.ValidatePlotArea(rotatedBounds);
        if (corners == null || corners.Length != 4)
        {
            throw new System.ArgumentException("A text rotation plan requires four corners.");
        }

        AngleRadians = angleRadians;
        UnrotatedBounds = ChartPlot.CopyRect(unrotatedBounds);
        RotatedBounds = ChartPlot.CopyRect(rotatedBounds);
        Corners = ChartRenderCommand.CopyPoints(corners);
    }

    /// <summary>Returns the rotation angle in radians.</summary>
    /// <returns>The angle in radians.</returns>
    public double GetAngleRadians()
    {
        return AngleRadians;
    }

    /// <summary>Returns the unrotated bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    public ChartRect GetUnrotatedBounds()
    {
        return ChartPlot.CopyRect(UnrotatedBounds);
    }

    /// <summary>Returns the rotated axis-aligned bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    public ChartRect GetRotatedBounds()
    {
        return ChartPlot.CopyRect(RotatedBounds);
    }

    /// <summary>Returns the four rotated corners.</summary>
    /// <returns>Independent corner copies.</returns>
    public ChartPoint[] GetCorners()
    {
        return ChartRenderCommand.CopyPoints(Corners);
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied rotation plan.</returns>
    public ChartTextRotationPlan Copy()
    {
        return new ChartTextRotationPlan(AngleRadians, UnrotatedBounds, RotatedBounds, Corners);
    }
}
