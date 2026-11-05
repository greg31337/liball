namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHistogramBin"/>.</summary>
public interface IChartHistogramBin
{
    /// <summary>
    /// Applies the width operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Width();
    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimum();
    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimum(double value);
    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximum();
    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximum(double value);
    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    int GetCount();
    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCount(int value);
}
