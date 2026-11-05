namespace chart;
/// <summary>
/// Represents a category point scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartCategoryPointScale : IChartCategoryPointScale
{
    internal string[] Categories;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal double Padding;
    internal double Step;
    /// <summary>
    /// Initializes a category point scale from the supplied categories, range minimum, range maximum, and padding.
    /// </summary>
    /// <param name = "categories">The categories.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <param name = "padding">The padding.</param>
    public ChartCategoryPointScale(string[] categories, double rangeMinimum, double rangeMaximum, double padding)
    {
        ChartCategoryBandScale.ValidateCategories(categories);
        ChartCategoryBandScale.ValidatePadding(padding, "padding");
        Categories = ChartDataColumn.CopyStringArray(categories);
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        Padding = padding;
        if (categories.Length == 1)
        {
            Step = 0.0;
        }
        else
        {
            Step = (rangeMaximum - rangeMinimum) / (categories.Length - 1.0 + padding * 2.0);
        }
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting double.</returns>
    public double Map(string category)
    {
        int index = CategoryIndex(category);
        if (Categories.Length == 1)
        {
            return (RangeMinimum + RangeMaximum) / 2.0;
        }

        return RangeMinimum + Step * (Padding + index);
    }

    /// <summary>
    /// Returns the zero-based category index.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public int CategoryIndex(string category)
    {
        int index = 0;
        while (index < Categories.Length)
        {
            if (Categories[index] == category)
            {
                return index;
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

    /// <summary>Returns the current range minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMinimum()
    {
        return RangeMinimum;
    }

    /// <summary>Replaces the current range minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMinimum(double value)
    {
        RangeMinimum = value;
    }

    /// <summary>Returns the current range maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetRangeMaximum()
    {
        return RangeMaximum;
    }

    /// <summary>Replaces the current range maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRangeMaximum(double value)
    {
        RangeMaximum = value;
    }

    /// <summary>Returns the current padding.</summary>
    /// <returns>The current field value.</returns>
    public double GetPadding()
    {
        return Padding;
    }

    /// <summary>Replaces the current padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPadding(double value)
    {
        Padding = value;
    }

    /// <summary>Returns the current step.</summary>
    /// <returns>The current field value.</returns>
    public double GetStep()
    {
        return Step;
    }

    /// <summary>Replaces the current step.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStep(double value)
    {
        Step = value;
    }
}
