namespace chart;
/// <summary>
/// Represents an ordinal scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartOrdinalScale : IChartOrdinalScale
{
    internal string[] Categories;
    internal double[] Values;
    /// <summary>
    /// Initializes an ordinal scale from the supplied categories and values.
    /// </summary>
    /// <param name = "categories">The categories.</param>
    /// <param name = "values">The input values to process.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartOrdinalScale(string[] categories, double[] values)
    {
        ChartCategoryBandScale.ValidateCategories(categories);
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (categories.Length != values.Length)
        {
            throw new System.ArgumentException("Ordinal categories and values must have the same length.");
        }

        ChartStatisticsOperations.ValidateFiniteValues(values);
        Categories = ChartDataColumn.CopyStringArray(categories);
        Values = ChartDataMatrix.CopyDoubleArray(values);
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public double Map(string category)
    {
        int index = 0;
        while (index < Categories.Length)
        {
            if (Categories[index] == category)
            {
                return Values[index];
            }

            index = index + 1;
        }

        throw new System.ArgumentException("Category was not found.");
    }

    /// <summary>Returns the current categories.</summary>
    /// <returns>The current field value.</returns>
    public string[] GetCategories()
    {
        return Categories;
    }

    /// <summary>Replaces the current categories.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategories(string[] value)
    {
        Categories = value;
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
}
