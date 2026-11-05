namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelAxisLayout"/>.</summary>
public interface IChartParallelAxisLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartParallelAxisLayout Copy();
    /// <summary>Returns the current axis index.</summary>
    /// <returns>The current field value.</returns>
    int GetAxisIndex();
    /// <summary>Replaces the current axis index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxisIndex(int value);
    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    string GetLabel();
    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabel(string value);
    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    double GetX();
    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX(double value);
    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimum();
    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimum(double value);
    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximum();
    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximum(double value);
}
