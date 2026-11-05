namespace chart;
/// <summary>
/// Represents a graph link used by the chart model or rendering pipeline.
/// </summary>
public class ChartGraphLink : IChartGraphLink
{
    internal int SourceIndex;
    internal int TargetIndex;
    internal double Value;
    /// <summary>
    /// Initializes a graph link from the supplied source index, target index, and value.
    /// </summary>
    /// <param name = "sourceIndex">The zero-based source index.</param>
    /// <param name = "targetIndex">The zero-based target index.</param>
    /// <param name = "value">The value.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartGraphLink(int sourceIndex, int targetIndex, double value)
    {
        if (sourceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceIndex");
        }

        if (targetIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("targetIndex");
        }

        ChartLinearScale.ValidateFinite(value, "value");
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        SourceIndex = sourceIndex;
        TargetIndex = targetIndex;
        Value = value;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartGraphLink Copy()
    {
        return new ChartGraphLink(SourceIndex, TargetIndex, Value);
    }

    /// <summary>Returns the current source index.</summary>
    /// <returns>The current field value.</returns>
    public int GetSourceIndex()
    {
        return SourceIndex;
    }

    /// <summary>Replaces the current source index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSourceIndex(int value)
    {
        SourceIndex = value;
    }

    /// <summary>Returns the current target index.</summary>
    /// <returns>The current field value.</returns>
    public int GetTargetIndex()
    {
        return TargetIndex;
    }

    /// <summary>Replaces the current target index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTargetIndex(int value)
    {
        TargetIndex = value;
    }

    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    public double GetValue()
    {
        return Value;
    }

    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValue(double value)
    {
        Value = value;
    }
}
