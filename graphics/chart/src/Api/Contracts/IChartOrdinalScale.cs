namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartOrdinalScale"/>.</summary>
public interface IChartOrdinalScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    double Map(string category);
    /// <summary>Returns the current categories.</summary>
    /// <returns>The current field value.</returns>
    string[] GetCategories();
    /// <summary>Replaces the current categories.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategories(string[] value);
    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    double[] GetValues();
    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValues(double[] value);
}
