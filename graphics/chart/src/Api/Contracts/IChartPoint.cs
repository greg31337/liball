namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPoint"/>.</summary>
public interface IChartPoint
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartPoint Copy();
    /// <summary>
    /// Applies the distance to operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns>The resulting double.</returns>
    double DistanceTo(ChartPoint point);
    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    double GetX();
    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX(double value);
    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    double GetY();
    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY(double value);
}
