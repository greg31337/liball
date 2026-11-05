namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLayoutItem"/>.</summary>
public interface IChartLayoutItem
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLayoutItem Copy();
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartLayoutItemKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartLayoutItemKind value);
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetBounds();
    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBounds(ChartRect value);
    /// <summary>Returns the current side.</summary>
    /// <returns>The current field value.</returns>
    ChartSide GetSide();
    /// <summary>Replaces the current side.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSide(ChartSide value);
}
