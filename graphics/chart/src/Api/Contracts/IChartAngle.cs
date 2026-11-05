namespace chart;
/// <summary>Defines a finite angle expressed in degrees with conversion and normalization operations.</summary>
public interface IChartAngle
{
    /// <summary>Converts this angle to radians.</summary>
    /// <returns>The angle in radians.</returns>
    double ToRadians();
    /// <summary>Creates an equivalent angle normalized to the range from zero inclusive to 360 exclusive.</summary>
    /// <returns>The normalized angle.</returns>
    ChartAngle Normalize();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A new angle with the same value.</returns>
    ChartAngle Copy();
    /// <summary>Returns the angle in degrees.</summary>
    /// <returns>The current angle in degrees.</returns>
    double GetDegrees();
    /// <summary>Replaces the angle in degrees.</summary>
    /// <param name = "value">The finite angle in degrees.</param>
    void SetDegrees(double value);
}
