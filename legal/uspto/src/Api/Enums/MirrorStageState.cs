namespace uspto;

/// <summary>
/// Describes the persisted state of one mirror processing stage.
/// </summary>
public enum MirrorStageState
{
    /// <summary>The stage has not started.</summary>
    NotStarted = 0,

    /// <summary>The stage is currently in progress.</summary>
    InProgress = 1,

    /// <summary>The stage completed successfully.</summary>
    Complete = 2,

    /// <summary>The stage failed and may require retry or repair.</summary>
    Failed = 3
}
