namespace molecule;


/// <summary>
/// Configures optional interface-based filtering and progress reporting for batch conversion.
/// </summary>
public class BatchConversionOptions
{
    /// <summary>
    /// The optional filter applied to each successfully read input record.
    /// </summary>
    public IBatchConversionFilter? Filter;
    /// <summary>
    /// The optional recipient of per-record and completion count snapshots.
    /// </summary>
    public IBatchConversionProgress? Progress;

    /// <summary>
    /// Initializes batch conversion without filtering or progress reporting.
    /// </summary>
    public BatchConversionOptions()
    {
        Filter = null;
        Progress = null;
    }
}
