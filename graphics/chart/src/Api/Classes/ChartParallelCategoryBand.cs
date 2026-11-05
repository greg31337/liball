namespace chart;
/// <summary>
/// Represents a parallel category band used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelCategoryBand : IChartParallelCategoryBand
{
    internal int AxisIndex;
    internal int CategoryIndex;
    internal string Category;
    internal double X;
    internal double Y;
    internal double Height;
    internal int Count;
    /// <summary>
    /// Initializes a parallel category band from the supplied axis index, category index, category, x, y, height, and count.
    /// </summary>
    /// <param name = "axisIndex">The zero-based axis index.</param>
    /// <param name = "categoryIndex">The zero-based category index.</param>
    /// <param name = "category">The category.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "count">The count.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCategoryBand(int axisIndex, int categoryIndex, string category, double x, double y, double height, int count)
    {
        ChartTextBlock.ValidateText(category, "category");
        ChartLinearScale.ValidateFinite(x, "x");
        ChartLinearScale.ValidateFinite(y, "y");
        ChartRenderStyle.ValidatePositive(height, "height");
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        AxisIndex = axisIndex;
        CategoryIndex = categoryIndex;
        Category = category;
        X = x;
        Y = y;
        Height = height;
        Count = count;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartParallelCategoryBand Copy()
    {
        return new ChartParallelCategoryBand(AxisIndex, CategoryIndex, Category, X, Y, Height, Count);
    }

    /// <summary>Returns the current axis index.</summary>
    /// <returns>The current field value.</returns>
    public int GetAxisIndex()
    {
        return AxisIndex;
    }

    /// <summary>Replaces the current axis index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxisIndex(int value)
    {
        AxisIndex = value;
    }

    /// <summary>Returns the current category index.</summary>
    /// <returns>The current field value.</returns>
    public int GetCategoryIndex()
    {
        return CategoryIndex;
    }

    /// <summary>Replaces the current category index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategoryIndex(int value)
    {
        CategoryIndex = value;
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

    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    public double GetX()
    {
        return X;
    }

    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX(double value)
    {
        X = value;
    }

    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    public double GetY()
    {
        return Y;
    }

    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY(double value)
    {
        Y = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }

    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCount(int value)
    {
        Count = value;
    }
}
