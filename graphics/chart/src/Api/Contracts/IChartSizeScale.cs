namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartSizeScale"/>.</summary>
public interface IChartSizeScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Map(double value);
    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    void SetClamp(bool clamp);
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
    /// <summary>Returns the current size minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetSizeMinimum();
    /// <summary>Replaces the current size minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSizeMinimum(double value);
    /// <summary>Returns the current size maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetSizeMaximum();
    /// <summary>Replaces the current size maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSizeMaximum(double value);
    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    bool GetClamp();
}
