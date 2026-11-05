namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParetoResult"/>.</summary>
public interface IChartParetoResult
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartParetoResult Copy();
    /// <summary>Returns the current labels.</summary>
    /// <returns>The current field value.</returns>
    string[] GetLabels();
    /// <summary>Replaces the current labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabels(string[] value);
    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    double[] GetValues();
    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValues(double[] value);
    /// <summary>Returns the current cumulative fractions.</summary>
    /// <returns>The current field value.</returns>
    double[] GetCumulativeFractions();
    /// <summary>Replaces the current cumulative fractions.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCumulativeFractions(double[] value);
}
