namespace csv;

/// <summary>
/// Decides whether a row continues through a processing pipeline.
/// </summary>
public interface ICsvRowFilter
{
    /// <summary>
    /// Decides whether a row is included.
    /// </summary>
    /// <param name="row">The row to inspect.</param>
    /// <returns><c>true</c> to include the row; otherwise <c>false</c>.</returns>
    bool Include(CsvRow row);
}
