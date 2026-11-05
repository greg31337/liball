namespace chart;
/// <summary>
/// Represents a category band scale used by the chart model or rendering pipeline.
/// </summary>
public class ChartCategoryBandScale : IChartCategoryBandScale
{
    internal string[] Categories;
    internal double RangeMinimum;
    internal double RangeMaximum;
    internal double InnerPadding;
    internal double OuterPadding;
    internal double Step;
    internal double Bandwidth;
    /// <summary>
    /// Initializes a category band scale from the supplied categories, range minimum, range maximum, inner padding, and outer padding.
    /// </summary>
    /// <param name = "categories">The categories.</param>
    /// <param name = "rangeMinimum">The range minimum.</param>
    /// <param name = "rangeMaximum">The range maximum.</param>
    /// <param name = "innerPadding">The inner padding.</param>
    /// <param name = "outerPadding">The outer padding.</param>
    public ChartCategoryBandScale(string[] categories, double rangeMinimum, double rangeMaximum, double innerPadding, double outerPadding)
    {
        ValidateCategories(categories);
        ValidatePadding(innerPadding, "innerPadding");
        ValidatePadding(outerPadding, "outerPadding");
        Categories = ChartDataColumn.CopyStringArray(categories);
        RangeMinimum = rangeMinimum;
        RangeMaximum = rangeMaximum;
        InnerPadding = innerPadding;
        OuterPadding = outerPadding;
        Step = (rangeMaximum - rangeMinimum) / (categories.Length - innerPadding + outerPadding * 2.0);
        Bandwidth = Step * (1.0 - innerPadding);
    }

    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting double.</returns>
    public double Map(string category)
    {
        int index = CategoryIndex(category);
        return RangeMinimum + Step * (OuterPadding + index);
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

    /// <summary>
    /// Creates an independent copy of the categories.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string[] CopyCategories()
    {
        return ChartDataColumn.CopyStringArray(Categories);
    }

    internal static void ValidatePadding(double padding, string name)
    {
        if (padding < 0.0 || padding >= 1.0 || double.IsNaN(padding) || double.IsInfinity(padding))
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateCategories(string[] categories)
    {
        if (categories == null)
        {
            throw new System.ArgumentNullException("categories");
        }

        if (categories.Length == 0)
        {
            throw new System.ArgumentException("Scale categories cannot be empty.");
        }

        int index = 0;
        while (index < categories.Length)
        {
            if (categories[index] == null || categories[index].Length == 0)
            {
                throw new System.ArgumentException("Scale category cannot be missing.");
            }

            index = index + 1;
        }
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

    /// <summary>Returns the current inner padding.</summary>
    /// <returns>The current field value.</returns>
    public double GetInnerPadding()
    {
        return InnerPadding;
    }

    /// <summary>Replaces the current inner padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetInnerPadding(double value)
    {
        InnerPadding = value;
    }

    /// <summary>Returns the current outer padding.</summary>
    /// <returns>The current field value.</returns>
    public double GetOuterPadding()
    {
        return OuterPadding;
    }

    /// <summary>Replaces the current outer padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetOuterPadding(double value)
    {
        OuterPadding = value;
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

    /// <summary>Returns the current bandwidth.</summary>
    /// <returns>The current field value.</returns>
    public double GetBandwidth()
    {
        return Bandwidth;
    }

    /// <summary>Replaces the current bandwidth.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBandwidth(double value)
    {
        Bandwidth = value;
    }
}
