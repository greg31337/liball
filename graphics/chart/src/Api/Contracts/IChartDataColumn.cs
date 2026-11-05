namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDataColumn"/>.</summary>
public interface IChartDataColumn
{
    /// <summary>
    /// Returns the requested number.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting double.</returns>
    double GetNumber(int index);
    /// <summary>
    /// Returns the requested text.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The text value, or <c>null</c> when the value is missing.</returns>
    string? GetText(int index);
    /// <summary>
    /// Returns the requested date time.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting date time.</returns>
    System.DateTime GetDateTime(int index);
    /// <summary>
    /// Returns the requested category.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The category value, or <c>null</c> when the value is missing.</returns>
    string? GetCategory(int index);
    /// <summary>
    /// Returns the requested matrix value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetMatrixValue(int row, int column);
    /// <summary>
    /// Returns the requested matrix.
    /// </summary>
    /// <returns>The resulting data matrix.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    ChartDataMatrix GetMatrix();
    /// <summary>
    /// Creates an independent copy of the number values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    double[] CopyNumberValues();
    /// <summary>Returns a read-only zero-copy view of the numeric values.</summary>
    /// <returns>A read-only span backed by this numeric column.</returns>
    System.ReadOnlySpan<double> GetNumberSpan();
    /// <summary>
    /// Creates an independent copy of the text values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string? [] CopyTextValues();
    /// <summary>
    /// Creates an independent copy of the date time values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    System.DateTime[] CopyDateTimeValues();
    /// <summary>
    /// Creates an independent copy of the category values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string? [] CopyCategoryValues();
    /// <summary>
    /// Creates an independent copy of the category order.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string? [] CopyCategoryOrder();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDataColumn Copy();
    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    string GetName();
    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetName(string value);
    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    ChartDataKind GetKind();
    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetKind(ChartDataKind value);
    /// <summary>Returns the current length.</summary>
    /// <returns>The current field value.</returns>
    int GetLength();
    /// <summary>Replaces the current length.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLength(int value);
    /// <summary>Returns the current number values.</summary>
    /// <returns>The current field value.</returns>
    double[] GetNumberValues();
    /// <summary>Replaces the current number values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetNumberValues(double[] value);
    /// <summary>Returns the current text values.</summary>
    /// <returns>The current field value.</returns>
    string? [] GetTextValues();
    /// <summary>Replaces the current text values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTextValues(string? [] value);
    /// <summary>Returns the current date time values.</summary>
    /// <returns>The current field value.</returns>
    System.DateTime[] GetDateTimeValues();
    /// <summary>Replaces the current date time values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetDateTimeValues(System.DateTime[] value);
    /// <summary>Returns the current category values.</summary>
    /// <returns>The current field value.</returns>
    string? [] GetCategoryValues();
    /// <summary>Replaces the current category values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategoryValues(string? [] value);
    /// <summary>Returns the current category order.</summary>
    /// <returns>The current field value.</returns>
    string? [] GetCategoryOrder();
    /// <summary>Replaces the current category order.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCategoryOrder(string? [] value);
    /// <summary>Replaces the current matrix value.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMatrixValue(ChartDataMatrix? value);
}
