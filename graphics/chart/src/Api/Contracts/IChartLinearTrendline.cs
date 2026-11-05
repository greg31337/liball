namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLinearTrendline"/>.</summary>
public interface IChartLinearTrendline
{
    /// <summary>
    /// Applies the predict operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <returns>The resulting double.</returns>
    double Predict(double x);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLinearTrendline Copy();
    /// <summary>Returns the current slope.</summary>
    /// <returns>The current field value.</returns>
    double GetSlope();
    /// <summary>Replaces the current slope.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSlope(double value);
    /// <summary>Returns the current intercept.</summary>
    /// <returns>The current field value.</returns>
    double GetIntercept();
    /// <summary>Replaces the current intercept.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIntercept(double value);
    /// <summary>Returns the current r squared.</summary>
    /// <returns>The current field value.</returns>
    double GetRSquared();
    /// <summary>Replaces the current r squared.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRSquared(double value);
}
