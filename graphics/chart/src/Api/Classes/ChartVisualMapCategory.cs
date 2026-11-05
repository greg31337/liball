namespace chart;
/// <summary>
/// Represents a visual map category used by the chart model or rendering pipeline.
/// </summary>
public class ChartVisualMapCategory : IChartVisualMapCategory
{
    internal string Category;
    internal ChartColor Color;
    /// <summary>
    /// Initializes a visual map category from the supplied category and color.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartVisualMapCategory(string category, ChartColor color)
    {
        ValidateCategory(category, "category");
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        Category = category;
        Color = color.Copy();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartVisualMapCategory Copy()
    {
        return new ChartVisualMapCategory(Category, Color);
    }

    internal static void ValidateCategory(string category, string name)
    {
        if (category == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (category.Length == 0)
        {
            throw new System.ArgumentException("Visual map category cannot be empty.");
        }
    }

    /// <summary>Returns the current category.</summary>
    /// <returns>The current field value.</returns>
    public string GetCategory()
    {
        return Category;
    }

    /// <summary>Replaces the current category.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategory(string value)
    {
        Category = value;
    }

    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetColor()
    {
        return Color;
    }

    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColor(ChartColor value)
    {
        Color = value;
    }
}
