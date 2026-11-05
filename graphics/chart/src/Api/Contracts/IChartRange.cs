namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartRange"/>.</summary>
public interface IChartRange
{
    /// <summary>
    /// Applies the length operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Length();
    /// <summary>
    /// Determines whether this value is valid.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsValid();
    /// <summary>
    /// Determines whether this value contains the configured value.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool Contains(double value);
    /// <summary>
    /// Clamps the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Clamp(double value);
    /// <summary>
    /// Normalizes the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Normalize(double value);
    /// <summary>
    /// Applies the lerp operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting double.</returns>
    double Lerp(double amount);
    /// <summary>
    /// Applies the pad operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting range.</returns>
    ChartRange Pad(double amount);
    /// <summary>
    /// Expands this value to include the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting range.</returns>
    ChartRange Include(double value);
    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimum();
    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimum(double value);
    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximum();
    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximum(double value);
}
