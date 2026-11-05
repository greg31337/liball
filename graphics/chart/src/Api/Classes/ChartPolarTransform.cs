namespace chart;
/// <summary>
/// Represents a polar transform used by the chart model or rendering pipeline.
/// </summary>
public class ChartPolarTransform : IChartPolarTransform
{
    internal double CenterX;
    internal double CenterY;
    internal double InnerRadius;
    internal double OuterRadius;
    internal double MinimumValue;
    internal double MaximumValue;
    internal double StartAngle;
    internal bool Clockwise;
    internal bool Clamp;
    /// <summary>
    /// Initializes a polar transform from the supplied center x, center y, inner radius, outer radius, minimum value, maximum value, start angle, and clockwise.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "minimumValue">The minimum value.</param>
    /// <param name = "maximumValue">The maximum value.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "clockwise">The clockwise.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartPolarTransform(double centerX, double centerY, double innerRadius, double outerRadius, double minimumValue, double maximumValue, double startAngle, bool clockwise)
    {
        ChartPieChartsOperations.ValidateLayoutPoint(centerX, centerY);
        ChartStyle.ValidateNonNegative(innerRadius, "innerRadius");
        ChartRenderStyle.ValidatePositive(outerRadius, "outerRadius");
        if (innerRadius >= outerRadius)
        {
            throw new System.ArgumentException("Inner radius must be smaller than outer radius.");
        }

        ChartLinearScale.ValidateDomain(minimumValue, maximumValue);
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        CenterX = centerX;
        CenterY = centerY;
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
        MinimumValue = minimumValue;
        MaximumValue = maximumValue;
        StartAngle = startAngle;
        Clockwise = clockwise;
        Clamp = false;
    }

    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    public void SetClamp(bool clamp)
    {
        Clamp = clamp;
    }

    /// <summary>
    /// Maps radius through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double MapRadius(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - MinimumValue) / (MaximumValue - MinimumValue);
        if (Clamp)
        {
            amount = ChartScaleUtilities.ClampUnit(amount);
        }

        return InnerRadius + amount * (OuterRadius - InnerRadius);
    }

    /// <summary>
    /// Maps angle through the configured input and output domains.
    /// </summary>
    /// <param name = "angle">The angle.</param>
    /// <returns>The resulting double.</returns>
    public double MapAngle(double angle)
    {
        ChartLinearScale.ValidateFinite(angle, "angle");
        if (Clockwise)
        {
            return StartAngle + angle;
        }

        return StartAngle - angle;
    }

    /// <summary>
    /// Returns the requested point.
    /// </summary>
    /// <param name = "angle">The angle.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint PointAt(double angle, double value)
    {
        double radius = MapRadius(value);
        ChartStyle.ValidateNonNegative(radius, "radius");
        if (radius == 0.0)
        {
            return new ChartPoint(CenterX, CenterY);
        }

        return ChartPieChartsOperations.PointAtAngle(CenterX, CenterY, MapAngle(angle), radius);
    }

    /// <summary>Returns the current center x.</summary>
    /// <returns>The current field value.</returns>
    public double GetCenterX()
    {
        return CenterX;
    }

    /// <summary>Replaces the current center x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCenterX(double value)
    {
        CenterX = value;
    }

    /// <summary>Returns the current center y.</summary>
    /// <returns>The current field value.</returns>
    public double GetCenterY()
    {
        return CenterY;
    }

    /// <summary>Replaces the current center y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCenterY(double value)
    {
        CenterY = value;
    }

    /// <summary>Returns the current inner radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetInnerRadius()
    {
        return InnerRadius;
    }

    /// <summary>Replaces the current inner radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetInnerRadius(double value)
    {
        InnerRadius = value;
    }

    /// <summary>Returns the current outer radius.</summary>
    /// <returns>The current field value.</returns>
    public double GetOuterRadius()
    {
        return OuterRadius;
    }

    /// <summary>Replaces the current outer radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOuterRadius(double value)
    {
        OuterRadius = value;
    }

    /// <summary>Returns the current minimum value.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimumValue()
    {
        return MinimumValue;
    }

    /// <summary>Replaces the current minimum value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimumValue(double value)
    {
        MinimumValue = value;
    }

    /// <summary>Returns the current maximum value.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximumValue()
    {
        return MaximumValue;
    }

    /// <summary>Replaces the current maximum value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximumValue(double value)
    {
        MaximumValue = value;
    }

    /// <summary>Returns the current start angle.</summary>
    /// <returns>The current field value.</returns>
    public double GetStartAngle()
    {
        return StartAngle;
    }

    /// <summary>Replaces the current start angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStartAngle(double value)
    {
        StartAngle = value;
    }

    /// <summary>Returns the current clockwise.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClockwise()
    {
        return Clockwise;
    }

    /// <summary>Replaces the current clockwise.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetClockwise(bool value)
    {
        Clockwise = value;
    }

    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClamp()
    {
        return Clamp;
    }
}
