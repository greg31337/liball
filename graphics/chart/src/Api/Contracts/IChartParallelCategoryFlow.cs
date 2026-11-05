namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelCategoryFlow"/>.</summary>
public interface IChartParallelCategoryFlow
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartParallelCategoryFlow Copy();
    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    int GetRowIndex();
    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowIndex(int value);
    /// <summary>Returns the current source band index.</summary>
    /// <returns>The current field value.</returns>
    int GetSourceBandIndex();
    /// <summary>Replaces the current source band index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSourceBandIndex(int value);
    /// <summary>Returns the current target band index.</summary>
    /// <returns>The current field value.</returns>
    int GetTargetBandIndex();
    /// <summary>Replaces the current target band index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTargetBandIndex(int value);
    /// <summary>Returns the current x1.</summary>
    /// <returns>The current field value.</returns>
    double GetX1();
    /// <summary>Replaces the current x1.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX1(double value);
    /// <summary>Returns the current y1.</summary>
    /// <returns>The current field value.</returns>
    double GetY1();
    /// <summary>Replaces the current y1.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY1(double value);
    /// <summary>Returns the current x2.</summary>
    /// <returns>The current field value.</returns>
    double GetX2();
    /// <summary>Replaces the current x2.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX2(double value);
    /// <summary>Returns the current y2.</summary>
    /// <returns>The current field value.</returns>
    double GetY2();
    /// <summary>Replaces the current y2.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY2(double value);
}
