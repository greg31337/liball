namespace svg;

/// <summary>Configures bounded exact path cleanup.</summary>
public class SvgPathCleanupOptions
{
    /// <summary>The positive normalized and output command limit.</summary>
    public int MaximumCommands;

    /// <summary>Creates secure deterministic cleanup defaults.</summary>
    public SvgPathCleanupOptions() { MaximumCommands = 1000000; }
}

/// <summary>Reports an exact geometry-preserving path cleanup.</summary>
public class SvgPathCleanupResult
{
    /// <summary>Whether cleanup succeeded.</summary>
    public bool Success;
    /// <summary>The stable failure.</summary>
    public SvgPathCleanupError Error;
    /// <summary>The absolute explicit cleaned path, or <c>null</c> after failure.</summary>
    public SvgPathData? PathData;
    /// <summary>The normalized input command count.</summary>
    public int OriginalCommandCount;
    /// <summary>The cleaned output command count.</summary>
    public int OutputCommandCount;
    /// <summary>The number of normalized commands removed.</summary>
    public int RemovedCommandCount;
    /// <summary>The number of retained commands converted or merged exactly.</summary>
    public int CompactedCommandCount;

    /// <summary>Creates a path cleanup result.</summary>
    /// <param name="success">Whether cleanup succeeded.</param><param name="error">The failure.</param><param name="pathData">The cleaned path.</param><param name="originalCommandCount">The input count.</param><param name="outputCommandCount">The output count.</param><param name="removedCommandCount">The removed count.</param><param name="compactedCommandCount">The compacted count.</param>
    public SvgPathCleanupResult(bool success, SvgPathCleanupError error, SvgPathData? pathData, int originalCommandCount, int outputCommandCount, int removedCommandCount, int compactedCommandCount) { Success = success; Error = error; PathData = pathData; OriginalCommandCount = originalCommandCount; OutputCommandCount = outputCommandCount; RemovedCommandCount = removedCommandCount; CompactedCommandCount = compactedCommandCount; }
}
