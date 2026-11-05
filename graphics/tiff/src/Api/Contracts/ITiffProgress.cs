namespace tiff;

/// <summary>Receives synchronous progress updates from long-running TIFF operations.</summary>
/// <remarks>Implementations should return promptly. A progress value is detached and may be retained by the application.</remarks>
public interface ITiffProgress
{
    /// <summary>Reports the current operation and completed work units.</summary>
    /// <param name="progress">The detached progress value.</param>
    void Report(TiffProgress progress);
}
