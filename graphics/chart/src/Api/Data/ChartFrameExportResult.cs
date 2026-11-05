namespace chart;
/// <summary>Contains one product and its timing metadata from a sequential frame export.</summary>
public class ChartFrameExportResult
{
    /// <summary>The stable frame identifier.</summary>
    public string FrameId;
    /// <summary>The zero-based sequence index.</summary>
    public int FrameIndex;
    /// <summary>The frame start time in milliseconds.</summary>
    public long StartMilliseconds;
    /// <summary>The positive frame duration in milliseconds.</summary>
    public int DurationMilliseconds;
    /// <summary>The independently exported frame product.</summary>
    public ChartExportResult Export;

    /// <summary>Initializes an empty sequential-frame export result.</summary>
    public ChartFrameExportResult()
    {
        FrameId = "";
        FrameIndex = 0;
        StartMilliseconds = 0;
        DurationMilliseconds = 1;
        Export = new ChartExportResult();
    }
}
