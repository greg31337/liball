using System;

namespace csv;

/// <summary>
/// Wraps an optional callback that observes processing progress.
/// </summary>
public class CsvProgressHandler : ICsvProgressHandler
{
    /// <summary>
    /// Callback receiving pipeline state and the latest written row.
    /// </summary>
    internal Action<CsvPipeline, CsvRow> ReportCallback;
    /// <summary>
    /// Whether progress reporting invokes its callback.
    /// </summary>
    internal bool HasReportCallback;

    /// <summary>
    /// Initializes a progress handler with a callback.
    /// </summary>
    /// <param name="reportCallback">The callback invoked for processing progress.</param>
    public CsvProgressHandler(Action<CsvPipeline, CsvRow> reportCallback)
    {
        ReportCallback = reportCallback;
        HasReportCallback = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvProgressHandler CreateDefault()
    {
        CsvProgressHandler handler = new CsvProgressHandler(ReportDefault);
        handler.HasReportCallback = false;
        return handler;
    }

    /// <summary>
    /// Invokes the progress callback when enabled.
    /// </summary>
    /// <param name="pipeline">Pipeline configuration and counters.</param>
    /// <param name="row">The row to process.</param>
    public void Report(CsvPipeline pipeline, CsvRow row)
    {
        if (HasReportCallback)
        {
            ReportCallback(pipeline, row);
        }
    }

    internal static void ReportDefault(CsvPipeline pipeline, CsvRow row)
    {
    }
}
