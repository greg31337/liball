namespace chart;
/// <summary>
/// Represents a tick used by the chart model or rendering pipeline.
/// </summary>
public class ChartTick : IChartTick
{
    internal ChartTickKind Kind;
    internal double Value;
    internal System.DateTime DateTimeValue;
    internal string Label;
    internal int Index;
    /// <summary>
    /// Initializes a tick from the supplied kind, value, label, and index.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartTick(ChartTickKind kind, double value, string label, int index)
    {
        if (label == null)
        {
            throw new System.ArgumentNullException("label");
        }

        Kind = kind;
        Value = value;
        DateTimeValue = new System.DateTime(0);
        Label = label;
        Index = index;
    }

    internal static ChartTick CreateTime(System.DateTime value, int index)
    {
        ChartTick tick = new ChartTick(ChartTickKind.Time, value.Ticks, "", index);
        tick.DateTimeValue = value;
        return tick;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartTick Copy()
    {
        ChartTick tick = new ChartTick(Kind, Value, Label, Index);
        tick.DateTimeValue = DateTimeValue;
        return tick;
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartTickKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartTickKind value)
    {
        Kind = value;
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

    /// <summary>Returns the current date time value.</summary>
    /// <returns>The current field value.</returns>
    public System.DateTime GetDateTimeValue()
    {
        return DateTimeValue;
    }

    /// <summary>Replaces the current date time value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDateTimeValue(System.DateTime value)
    {
        DateTimeValue = value;
    }

    /// <summary>Returns the current label.</summary>
    /// <returns>The current field value.</returns>
    public string GetLabel()
    {
        return Label;
    }

    /// <summary>Replaces the current label.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLabel(string value)
    {
        Label = value;
    }

    /// <summary>Returns the current index.</summary>
    /// <returns>The current field value.</returns>
    public int GetIndex()
    {
        return Index;
    }

    /// <summary>Replaces the current index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetIndex(int value)
    {
        Index = value;
    }
}
