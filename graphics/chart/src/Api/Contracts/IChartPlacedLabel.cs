namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPlacedLabel"/>.</summary>
public interface IChartPlacedLabel
{
    /// <summary>
    /// Applies the bounds operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting rect.</returns>
    ChartRect Bounds();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartPlacedLabel Copy();
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    string GetText();
    /// <summary>Replaces the current text.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetText(string value);
    /// <summary>Returns the current source index.</summary>
    /// <returns>The current field value.</returns>
    int GetSourceIndex();
    /// <summary>Replaces the current source index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSourceIndex(int value);
    /// <summary>Returns the current anchor x.</summary>
    /// <returns>The current field value.</returns>
    double GetAnchorX();
    /// <summary>Replaces the current anchor x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAnchorX(double value);
    /// <summary>Returns the current anchor y.</summary>
    /// <returns>The current field value.</returns>
    double GetAnchorY();
    /// <summary>Replaces the current anchor y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAnchorY(double value);
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
    /// <summary>Returns the current visible.</summary>
    /// <returns>The current field value.</returns>
    bool GetVisible();
    /// <summary>Replaces the current visible.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetVisible(bool value);
    /// <summary>Returns the current position.</summary>
    /// <returns>The current field value.</returns>
    ChartLabelPosition GetPosition();
    /// <summary>Replaces the current position.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPosition(ChartLabelPosition value);
}
