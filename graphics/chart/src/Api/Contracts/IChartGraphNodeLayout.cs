namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartGraphNodeLayout"/>.</summary>
public interface IChartGraphNodeLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartGraphNodeLayout Copy();
    /// <summary>Returns the current node index.</summary>
    /// <returns>The current field value.</returns>
    int GetNodeIndex();
    /// <summary>Replaces the current node index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNodeIndex(int value);
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
    /// <summary>Returns the current layer.</summary>
    /// <returns>The current field value.</returns>
    int GetLayer();
    /// <summary>Replaces the current layer.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLayer(int value);
}
