namespace chart;
/// <summary>
/// Represents a pareto result used by the chart model or rendering pipeline.
/// </summary>
public class ChartParetoResult : IChartParetoResult
{
    internal string[] Labels;
    internal double[] Values;
    internal double[] CumulativeFractions;
    /// <summary>
    /// Initializes a pareto result from the supplied labels, values, and cumulative fractions.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "cumulativeFractions">The cumulative fractions.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartParetoResult(string[] labels, double[] values, double[] cumulativeFractions)
    {
        ChartCompactChartsOperations.ValidateLabels(labels, 1);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ChartStatisticsOperations.ValidateFiniteValues(cumulativeFractions);
        if (labels.Length != values.Length || labels.Length != cumulativeFractions.Length)
        {
            throw new System.ArgumentException("Pareto arrays must have matching lengths.");
        }

        Labels = ChartDataColumn.CopyStringArray(labels);
        Values = ChartDataMatrix.CopyDoubleArray(values);
        CumulativeFractions = ChartDataMatrix.CopyDoubleArray(cumulativeFractions);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartParetoResult Copy()
    {
        return new ChartParetoResult(Labels, Values, CumulativeFractions);
    }

    /// <summary>Returns the current labels.</summary>
    /// <returns>The current field value.</returns>
    public string[] GetLabels()
    {
        return Labels;
    }

    /// <summary>Replaces the current labels.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLabels(string[] value)
    {
        Labels = value;
    }

    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetValues()
    {
        return Values;
    }

    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValues(double[] value)
    {
        Values = value;
    }

    /// <summary>Returns the current cumulative fractions.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetCumulativeFractions()
    {
        return CumulativeFractions;
    }

    /// <summary>Replaces the current cumulative fractions.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCumulativeFractions(double[] value)
    {
        CumulativeFractions = value;
    }
}
