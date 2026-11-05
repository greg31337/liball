namespace csv;

/// <summary>
/// Provides instance-based CSV pipeline processing.
/// </summary>
public class CsvProcessorService : ICsvProcessor
{
    /// <summary>Initializes a CSV processing service.</summary>
    public CsvProcessorService()
    {
    }

    /// <summary>Processes rows using the default pipeline.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination writer.</param>
    /// <returns>The number of rows written.</returns>
    public long ProcessDefault(ICsvRowReader reader, ICsvRowWriter writer)
    {
        return CsvProcessor.Process(reader, writer);
    }

    /// <summary>Processes rows using a supplied pipeline.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="pipeline">Pipeline configuration; null uses defaults.</param>
    /// <returns>The number of rows written.</returns>
    public long Process(ICsvRowReader reader, ICsvRowWriter writer, CsvPipeline? pipeline)
    {
        return CsvProcessor.Process(reader, writer, pipeline);
    }

    /// <summary>Loads rows into a bounded in-memory table.</summary>
    /// <param name="reader">The source reader.</param>
    /// <param name="options">Table-loading options; null uses defaults.</param>
    /// <returns>The loaded table.</returns>
    public CsvTable LoadTable(ICsvRowReader reader, CsvTableOptions? options)
    {
        return CsvProcessor.LoadTable(reader, options);
    }
}
