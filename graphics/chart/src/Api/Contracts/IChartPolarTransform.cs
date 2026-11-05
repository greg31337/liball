namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPolarTransform"/>.</summary>
public interface IChartPolarTransform
{
    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    void SetClamp(bool clamp);
    /// <summary>
    /// Maps radius through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double MapRadius(double value);
    /// <summary>
    /// Maps angle through the configured input and output domains.
    /// </summary>
    /// <param name = "angle">The angle.</param>
    /// <returns>The resulting double.</returns>
    double MapAngle(double angle);
    /// <summary>
    /// Returns the requested point.
    /// </summary>
    /// <param name = "angle">The angle.</param>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint PointAt(double angle, double value);
    /// <summary>Returns the current center x.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterX();
    /// <summary>Replaces the current center x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterX(double value);
    /// <summary>Returns the current center y.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterY();
    /// <summary>Replaces the current center y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterY(double value);
    /// <summary>Returns the current inner radius.</summary>
    /// <returns>The current field value.</returns>
    double GetInnerRadius();
    /// <summary>Replaces the current inner radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetInnerRadius(double value);
    /// <summary>Returns the current outer radius.</summary>
    /// <returns>The current field value.</returns>
    double GetOuterRadius();
    /// <summary>Replaces the current outer radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOuterRadius(double value);
    /// <summary>Returns the current minimum value.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimumValue();
    /// <summary>Replaces the current minimum value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimumValue(double value);
    /// <summary>Returns the current maximum value.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximumValue();
    /// <summary>Replaces the current maximum value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximumValue(double value);
    /// <summary>Returns the current start angle.</summary>
    /// <returns>The current field value.</returns>
    double GetStartAngle();
    /// <summary>Replaces the current start angle.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStartAngle(double value);
    /// <summary>Returns the current clockwise.</summary>
    /// <returns>The current field value.</returns>
    bool GetClockwise();
    /// <summary>Replaces the current clockwise.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetClockwise(bool value);
    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    bool GetClamp();
}
