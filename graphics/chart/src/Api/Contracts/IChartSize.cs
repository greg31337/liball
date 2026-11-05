namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartSize"/>.</summary>
public interface IChartSize
{
    /// <summary>
    /// Applies the area operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Area();
    /// <summary>
    /// Determines whether this value is empty.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsEmpty();
    /// <summary>
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "factor">The factor.</param>
    /// <returns>The resulting size.</returns>
    ChartSize Scale(double factor);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
}
