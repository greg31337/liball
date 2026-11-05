namespace jmap;

/// <summary>
/// Defines whether an Email submission can be canceled.
/// </summary>
public enum JmapSubmissionUndoStatus
{
    /// <summary>The submission may still be cancelable.</summary>
    Pending = 0,

    /// <summary>The submission can no longer be canceled.</summary>
    Final = 1,

    /// <summary>The submission was canceled.</summary>
    Canceled = 2
}
