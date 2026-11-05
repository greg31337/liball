namespace uspto;

/// <summary>
/// Describes the lifecycle state of a download.
/// </summary>
public enum DownloadState
{
    /// <summary>The download has not started.</summary>
    NotStarted = 0,

    /// <summary>The download is actively transferring content.</summary>
    Downloading = 1,

    /// <summary>The download completed and its basic response checks passed.</summary>
    Completed = 2,

    /// <summary>The download was cancelled by the caller.</summary>
    Cancelled = 3,

    /// <summary>The download failed.</summary>
    Failed = 4
}
