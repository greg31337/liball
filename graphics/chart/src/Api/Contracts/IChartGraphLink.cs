namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartGraphLink"/>.</summary>
public interface IChartGraphLink
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartGraphLink Copy();
    /// <summary>Returns the current source index.</summary>
    /// <returns>The current field value.</returns>
    int GetSourceIndex();
    /// <summary>Replaces the current source index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSourceIndex(int value);
    /// <summary>Returns the current target index.</summary>
    /// <returns>The current field value.</returns>
    int GetTargetIndex();
    /// <summary>Replaces the current target index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTargetIndex(int value);
    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    double GetValue();
    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValue(double value);
}
