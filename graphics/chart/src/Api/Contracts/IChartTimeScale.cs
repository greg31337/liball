namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTimeScale"/>.</summary>
public interface IChartTimeScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Map(System.DateTime value);
    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting date time.</returns>
    System.DateTime Invert(double value);
    /// <summary>Returns the current domain start.</summary>
    /// <returns>The current field value.</returns>
    System.DateTime GetDomainStart();
    /// <summary>Replaces the current domain start.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainStart(System.DateTime value);
    /// <summary>Returns the current domain end.</summary>
    /// <returns>The current field value.</returns>
    System.DateTime GetDomainEnd();
    /// <summary>Replaces the current domain end.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainEnd(System.DateTime value);
    /// <summary>Returns the current range minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetRangeMinimum();
    /// <summary>Replaces the current range minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRangeMinimum(double value);
    /// <summary>Returns the current range maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetRangeMaximum();
    /// <summary>Replaces the current range maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRangeMaximum(double value);
}
