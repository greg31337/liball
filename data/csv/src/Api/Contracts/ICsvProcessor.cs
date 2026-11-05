namespace csv;

/// <summary>
/// Runs streaming CSV pipelines and loads bounded in-memory tables.
/// </summary>
public interface ICsvProcessor
{
    /// <summary>Processes rows using the default pipeline.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination writer.</param>
    /// <returns>The number of rows written.</returns>
    long ProcessDefault(ICsvRowReader reader, ICsvRowWriter writer);

    /// <summary>Processes rows using a supplied pipeline.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">Pipeline configuration; null uses defaults.</param>
    /// <returns>The number of rows written.</returns>
    long Process(ICsvRowReader reader, ICsvRowWriter writer, CsvPipeline? pipeline);

    /// <summary>Loads rows into a bounded in-memory table.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="options">Table-loading options; null uses defaults.</param>
    /// <returns>The loaded table.</returns>
    CsvTable LoadTable(ICsvRowReader reader, CsvTableOptions? options);
}
