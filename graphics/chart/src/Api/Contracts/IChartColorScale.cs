namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartColorScale"/>.</summary>
public interface IChartColorScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting color.</returns>
    ChartColor Map(double value);
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
    /// <summary>Returns the current start color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetStartColor();
    /// <summary>Replaces the current start color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStartColor(ChartColor value);
    /// <summary>Returns the current end color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetEndColor();
    /// <summary>Replaces the current end color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEndColor(ChartColor value);
    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    bool GetClamp();
}
