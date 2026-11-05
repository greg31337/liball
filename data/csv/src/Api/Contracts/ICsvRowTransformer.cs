namespace csv;

/// <summary>
/// Transforms a row during streaming processing.
/// </summary>
public interface ICsvRowTransformer
{
    /// <summary>
    /// Produces the row passed to later pipeline stages.
    /// </summary>
    /// <param name="row">The input row.</param>
    /// <returns>The transformed row, or null to retain the input row.</returns>
    CsvRow? Transform(CsvRow row);
}
