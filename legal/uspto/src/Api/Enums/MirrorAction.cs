namespace uspto;

/// <summary>
/// Describes how a mirror planner classifies a catalog file.
/// </summary>
public enum MirrorAction
{
    /// <summary>The file has not yet been classified.</summary>
    Unknown = 0,

    /// <summary>The local file and remote catalog entry agree.</summary>
    Unchanged = 1,

    /// <summary>The remote catalog contains a file absent from the manifest.</summary>
    New = 2,

    /// <summary>The remote catalog entry changed after it was mirrored.</summary>
    Changed = 3,

    /// <summary>A manifest entry refers to a missing local file.</summary>
    Missing = 4,

    /// <summary>A local file failed size, digest, archive, or manifest validation.</summary>
    Corrupt = 5,

    /// <summary>A verified partial download can be resumed.</summary>
    Resumable = 6,

    /// <summary>A local file is no longer represented by the selected catalog state.</summary>
    Orphaned = 7
}
