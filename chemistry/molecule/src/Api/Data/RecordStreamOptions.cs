namespace molecule;


/// <summary>
/// Controls error continuation, failed-record collection, and flushing for molecule record streams.
/// </summary>
public class RecordStreamOptions
{
    /// <summary>
    /// Whether a record reader may advance after returning a failed record.
    /// </summary>
    public bool ContinueAfterError;
    /// <summary>
    /// Whether bulk reads include the molecule value from failed records.
    /// </summary>
    public bool IncludeFailedRecords;
    /// <summary>
    /// Whether the output writer is flushed after every molecule record.
    /// </summary>
    public bool FlushAfterRecord;

    /// <summary>
    /// Initializes streaming options that continue through errors and preserve existing bulk-read behavior.
    /// </summary>
    public RecordStreamOptions()
    {
        ContinueAfterError = true;
        IncludeFailedRecords = true;
        FlushAfterRecord = false;
    }
}
