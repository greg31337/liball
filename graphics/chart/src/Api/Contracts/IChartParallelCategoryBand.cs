namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelCategoryBand"/>.</summary>
public interface IChartParallelCategoryBand
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartParallelCategoryBand Copy();
    /// <summary>Returns the current axis index.</summary>
    /// <returns>The current field value.</returns>
    int GetAxisIndex();
    /// <summary>Replaces the current axis index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxisIndex(int value);
    /// <summary>Returns the current category index.</summary>
    /// <returns>The current field value.</returns>
    int GetCategoryIndex();
    /// <summary>Replaces the current category index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategoryIndex(int value);
    /// <summary>Returns the current category.</summary>
    /// <returns>The current field value.</returns>
    string GetCategory();
    /// <summary>Replaces the current category.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategory(string value);
    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    double GetX();
    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX(double value);
    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    double GetY();
    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    int GetCount();
    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCount(int value);
}
