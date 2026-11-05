namespace molecule;


/// <summary>
/// Provides conversion report data and operations for molecular graphs.
/// </summary>
public class ConversionReport
{
    /// <summary>
    /// The read count.
    /// </summary>
    public int ReadCount;
    /// <summary>
    /// The written count.
    /// </summary>
    public int WrittenCount;
    /// <summary>
    /// The number of successfully read records rejected by the batch filter.
    /// </summary>
    public int SkippedCount;
    /// <summary>
    /// The number of warning diagnostics observed while reading records.
    /// </summary>
    public int WarningCount;
    /// <summary>
    /// The number of input records containing error diagnostics.
    /// </summary>
    public int FailureCount;
    /// <summary>
    /// The error count.
    /// </summary>
    public int ErrorCount;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionReport"/> class.
    /// </summary>
    public ConversionReport()
    {
        ReadCount = 0;
        WrittenCount = 0;
        SkippedCount = 0;
        WarningCount = 0;
        FailureCount = 0;
        ErrorCount = 0;
        Success = true;
        Report = new MoleculeReport();
    }

    /// <summary>
    /// Adds the error.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    internal void AddError(string code, string message)
    {
        ErrorCount = ErrorCount + 1;
        Success = false;
        Report.AddError(code, message);
    }

    /// <summary>
    /// Appends a copy of another molecule and remaps its atom and bond identifiers.
    /// </summary>
    /// <param name="report">The report.</param>
    internal void Merge(MoleculeReport? report)
    {
        if (report == null)
        {
            return;
        }

        int index = 0;
        while (index < report.GetCount())
        {
            Report.Add(report.Get(index));
            if (report.Get(index).IsError())
            {
                ErrorCount = ErrorCount + 1;
                Success = false;
            }

            if (report.Get(index).IsWarning())
            {
                WarningCount = WarningCount + 1;
            }

            index = index + 1;
        }
    }
}
