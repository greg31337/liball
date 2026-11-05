namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTick"/>.</summary>
public interface IChartTick
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartTick Copy();
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartTickKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartTickKind value);
    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    double GetValue();
    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValue(double value);
    /// <summary>Returns the current date time value.</summary>
    /// <returns>The current field value.</returns>
    System.DateTime GetDateTimeValue();
    /// <summary>Replaces the current date time value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDateTimeValue(System.DateTime value);
    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    string GetLabel();
    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLabel(string value);
    /// <summary>Returns the current index.</summary>
    /// <returns>The current field value.</returns>
    int GetIndex();
    /// <summary>Replaces the current index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetIndex(int value);
}
