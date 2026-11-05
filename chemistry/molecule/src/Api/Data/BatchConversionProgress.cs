namespace molecule;


/// <summary>
/// Contains a point-in-time snapshot of deterministic batch conversion counts.
/// </summary>
public class BatchConversionProgress
{
    /// <summary>
    /// The number of input records read so far.
    /// </summary>
    public int ReadCount;
    /// <summary>
    /// The number of records written so far.
    /// </summary>
    public int WrittenCount;
    /// <summary>
    /// The number of successfully read records rejected by the filter so far.
    /// </summary>
    public int SkippedCount;
    /// <summary>
    /// The number of warning diagnostics observed so far.
    /// </summary>
    public int WarningCount;
    /// <summary>
    /// The number of input records with error diagnostics observed so far.
    /// </summary>
    public int FailureCount;
    /// <summary>
    /// Whether the input stream has been fully processed.
    /// </summary>
    public bool Completed;

    /// <summary>
    /// Initializes an incomplete snapshot with zero counts.
    /// </summary>
    public BatchConversionProgress()
    {
        ReadCount = 0;
        WrittenCount = 0;
        SkippedCount = 0;
        WarningCount = 0;
        FailureCount = 0;
        Completed = false;
    }
}
