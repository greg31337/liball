namespace csv;

/// <summary>
/// Observes progress after each row examined by a pipeline.
/// </summary>
public interface ICsvProgressHandler
{
    /// <summary>
    /// Reports current counters and the latest row.
    /// </summary>
    /// <param name="pipeline">The active pipeline and counters.</param>
    /// <param name="row">The latest row examined.</param>
    void Report(CsvPipeline pipeline, CsvRow row);
}
