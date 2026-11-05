namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHexbinCell"/>.</summary>
public interface IChartHexbinCell
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartHexbinCell Copy();
    /// <summary>Returns the current center x.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterX();
    /// <summary>Replaces the current center x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterX(double value);
    /// <summary>Returns the current center y.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterY();
    /// <summary>Replaces the current center y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterY(double value);
    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    int GetCount();
    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCount(int value);
}
