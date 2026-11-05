namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPadding"/>.</summary>
public interface IChartPadding
{
    /// <summary>
    /// Applies the horizontal operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Horizontal();
    /// <summary>
    /// Applies the vertical operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Vertical();
    /// <summary>
    /// Shrinks rect using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect DeflateRect(ChartRect rect);
    /// <summary>
    /// Expands rect using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect InflateRect(ChartRect rect);
    /// <summary>Returns the current left.</summary>
    /// <returns>The current field value.</returns>
    double GetLeft();
    /// <summary>Replaces the current left.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLeft(double value);
    /// <summary>Returns the current top.</summary>
    /// <returns>The current field value.</returns>
    double GetTop();
    /// <summary>Replaces the current top.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTop(double value);
    /// <summary>Returns the current right.</summary>
    /// <returns>The current field value.</returns>
    double GetRight();
    /// <summary>Replaces the current right.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRight(double value);
    /// <summary>Returns the current bottom.</summary>
    /// <returns>The current field value.</returns>
    double GetBottom();
    /// <summary>Replaces the current bottom.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBottom(double value);
}
