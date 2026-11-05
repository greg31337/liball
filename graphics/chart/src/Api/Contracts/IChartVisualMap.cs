namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartVisualMap"/>.</summary>
public interface IChartVisualMap
{
    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    void SetTitle(string title);
    /// <summary>
    /// Maps number through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    ChartColor MapNumber(double value);
    /// <summary>
    /// Maps category through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting color.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    ChartColor MapCategory(string category);
    /// <summary>
    /// Applies the build legend operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting legend.</returns>
    ChartLegend BuildLegend(string id);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartVisualMap Copy();
    /// <summary>
    /// Groups values into index using the supplied data and configuration.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    int BinIndex(double value);
    /// <summary>
    /// Returns the zero-based threshold index.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    int ThresholdIndex(double value);
    /// <summary>
    /// Returns the zero-based category index.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    int CategoryIndex(string category);
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartVisualMapKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartVisualMapKind value);
    /// <summary>Returns the current channel.</summary>
    /// <returns>The current field value.</returns>
    ChartEncodingChannel GetChannel();
    /// <summary>Replaces the current channel.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetChannel(ChartEncodingChannel value);
    /// <summary>Returns the current domain minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetDomainMinimum();
    /// <summary>Replaces the current domain minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainMinimum(double value);
    /// <summary>Returns the current domain maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetDomainMaximum();
    /// <summary>Replaces the current domain maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDomainMaximum(double value);
    /// <summary>Returns the current color map.</summary>
    /// <returns>The current field value.</returns>
    ChartColorMap GetColorMap();
    /// <summary>Replaces the current color map.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColorMap(ChartColorMap value);
    /// <summary>Returns the current palette.</summary>
    /// <returns>The current field value.</returns>
    ChartPalette GetPalette();
    /// <summary>Replaces the current palette.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPalette(ChartPalette value);
    /// <summary>Returns the current bins.</summary>
    /// <returns>The current field value.</returns>
    ChartVisualMapBin[] GetBins();
    /// <summary>Replaces the current bins.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBins(ChartVisualMapBin[] value);
    /// <summary>Returns the current bin count.</summary>
    /// <returns>The current field value.</returns>
    int GetBinCount();
    /// <summary>Replaces the current bin count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBinCount(int value);
    /// <summary>Returns the current thresholds.</summary>
    /// <returns>The current field value.</returns>
    double[] GetThresholds();
    /// <summary>Replaces the current thresholds.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetThresholds(double[] value);
    /// <summary>Returns the current threshold colors.</summary>
    /// <returns>The current field value.</returns>
    ChartColor[] GetThresholdColors();
    /// <summary>Replaces the current threshold colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetThresholdColors(ChartColor[] value);
    /// <summary>Returns the current categories.</summary>
    /// <returns>The current field value.</returns>
    ChartVisualMapCategory[] GetCategories();
    /// <summary>Replaces the current categories.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategories(ChartVisualMapCategory[] value);
    /// <summary>Returns the current category count.</summary>
    /// <returns>The current field value.</returns>
    int GetCategoryCount();
    /// <summary>Replaces the current category count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategoryCount(int value);
    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    string GetTitle();
}
