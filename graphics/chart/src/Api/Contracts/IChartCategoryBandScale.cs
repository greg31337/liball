namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartCategoryBandScale"/>.</summary>
public interface IChartCategoryBandScale
{
    /// <summary>
    /// Maps the configured value through the configured input and output domains.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting double.</returns>
    double Map(string category);
    /// <summary>
    /// Returns the zero-based category index.
    /// </summary>
    /// <param name = "category">The category.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    int CategoryIndex(string category);
    /// <summary>
    /// Creates an independent copy of the categories.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string[] CopyCategories();
    /// <summary>Returns the current categories.</summary>
    /// <returns>The current field value.</returns>
    string[] GetCategories();
    /// <summary>Replaces the current categories.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategories(string[] value);
    /// <summary>Returns the current range minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetRangeMinimum();
    /// <summary>Replaces the current range minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRangeMinimum(double value);
    /// <summary>Returns the current range maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetRangeMaximum();
    /// <summary>Replaces the current range maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRangeMaximum(double value);
    /// <summary>Returns the current inner padding.</summary>
    /// <returns>The current field value.</returns>
    double GetInnerPadding();
    /// <summary>Replaces the current inner padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetInnerPadding(double value);
    /// <summary>Returns the current outer padding.</summary>
    /// <returns>The current field value.</returns>
    double GetOuterPadding();
    /// <summary>Replaces the current outer padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetOuterPadding(double value);
    /// <summary>Returns the current step.</summary>
    /// <returns>The current field value.</returns>
    double GetStep();
    /// <summary>Replaces the current step.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStep(double value);
    /// <summary>Returns the current bandwidth.</summary>
    /// <returns>The current field value.</returns>
    double GetBandwidth();
    /// <summary>Replaces the current bandwidth.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBandwidth(double value);
}
