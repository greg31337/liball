namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLogScale"/>.</summary>
public interface IChartLogScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    double Map(double value);
    /// <summary>Returns the current domain minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetDomainMinimum();
    /// <summary>Replaces the current domain minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainMinimum(double value);
    /// <summary>Returns the current domain maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetDomainMaximum();
    /// <summary>Replaces the current domain maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainMaximum(double value);
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
    /// <summary>Returns the current base value.</summary>
    /// <returns>The current field value.</returns>
    double GetBaseValue();
    /// <summary>Replaces the current base value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBaseValue(double value);
}
