namespace tiff;

/// <summary>Describes completed and total work units for a long-running TIFF operation.</summary>
public class TiffProgress
{
    /// <summary>Identifies the operation being reported.</summary>
    public TiffProgressOperation Operation;
    /// <summary>Contains the number of completed work units.</summary>
    public long CompletedUnits;
    /// <summary>Contains the total number of work units when known, or zero when unknown.</summary>
    public long TotalUnits;

    /// <summary>Creates an empty TIFF progress value.</summary>
    public TiffProgress()
    {
        Operation = TiffProgressOperation.Open;
        CompletedUnits = 0;
        TotalUnits = 0;
    }

    internal static TiffProgress Create(TiffProgressOperation operation, long completedUnits, long totalUnits)
    {
        TiffProgress value = new TiffProgress();
        value.Operation = operation;
        value.CompletedUnits = completedUnits;
        value.TotalUnits = totalUnits;
        return value;
    }
}
