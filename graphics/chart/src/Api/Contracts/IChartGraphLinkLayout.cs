namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartGraphLinkLayout"/>.</summary>
public interface IChartGraphLinkLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartGraphLinkLayout Copy();
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
    /// <summary>Returns the current value.</summary>
    /// <returns>The current field value.</returns>
    double GetValue();
    /// <summary>Replaces the current value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValue(double value);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
}
