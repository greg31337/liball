namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLayoutRequest"/>.</summary>
public interface IChartLayoutRequest
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLayoutRequest Copy();
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
    /// <summary>Returns the current side.</summary>
    /// <returns>The current field value.</returns>
    ChartSide GetSide();
    /// <summary>Replaces the current side.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSide(ChartSide value);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
    /// <summary>Returns the current spacing.</summary>
    /// <returns>The current field value.</returns>
    double GetSpacing();
    /// <summary>Replaces the current spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSpacing(double value);
    /// <summary>Returns the current overlay.</summary>
    /// <returns>The current field value.</returns>
    bool GetOverlay();
    /// <summary>Replaces the current overlay.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOverlay(bool value);
}
