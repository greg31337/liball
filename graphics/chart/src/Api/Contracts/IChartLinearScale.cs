namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLinearScale"/>.</summary>
public interface IChartLinearScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Map(double value);
    /// <summary>
    /// Applies the invert operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double Invert(double value);
    /// <summary>
    /// Sets the domain configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    void SetDomain(double minimum, double maximum);
    /// <summary>
    /// Sets the range configuration.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    void SetRange(double minimum, double maximum);
    /// <summary>
    /// Sets the clamp configuration.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    void SetClamp(bool clamp);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLinearScale Copy();
    /// <summary>
    /// Creates a copy with the specified domain.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale WithDomain(double minimum, double maximum);
    /// <summary>
    /// Creates a copy with the specified range.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale WithRange(double minimum, double maximum);
    /// <summary>
    /// Creates a copy with the specified clamp.
    /// </summary>
    /// <param name = "clamp"><c>true</c> to constrain mapped values to the output range.</param>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale WithClamp(bool clamp);
    /// <summary>
    /// Creates a reversed copy of the range.
    /// </summary>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale ReverseRange();
    /// <summary>
    /// Expands this value to include zero using the supplied data and configuration.
    /// </summary>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale IncludeZero();
    /// <summary>
    /// Applies the pad domain operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "fraction">The fraction.</param>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale PadDomain(double fraction);
    /// <summary>
    /// Applies the nice domain operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "tickCount">The requested number of tick entries.</param>
    /// <returns>The resulting linear scale.</returns>
    ChartLinearScale NiceDomain(int tickCount);
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
    /// <summary>Returns the current clamp.</summary>
    /// <returns>The current field value.</returns>
    bool GetClamp();
}
