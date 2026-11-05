namespace uspto;

/// <summary>
/// Configures streamed and file-backed downloads.
/// </summary>
public class DownloadPolicy
{
    /// <summary>The total download timeout in milliseconds.</summary>
    public int TimeoutMilliseconds = UsptoDefaults.DownloadTimeoutMilliseconds;

    /// <summary>The transfer buffer size in bytes.</summary>
    public int BufferSize = UsptoDefaults.DownloadBufferSize;

    /// <summary>The maximum number of followed redirect hops.</summary>
    public int RedirectHopCount = UsptoDefaults.RedirectHopCount;

    /// <summary>An optional maximum number of bytes accepted for one download.</summary>
    public long MaximumBytes = 0L;

    /// <summary>Whether <see cref="MaximumBytes"/> contains a configured limit.</summary>
    public bool HasMaximumBytes = false;

    /// <summary>Whether an interrupted file download may use validated range resume.</summary>
    public bool AllowResume = true;

    /// <summary>Whether an existing completed destination may be replaced.</summary>
    public bool OverwriteCompletedFiles = false;
}
