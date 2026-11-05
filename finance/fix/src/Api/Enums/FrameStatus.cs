namespace fix;

/// <summary>Describes the outcome of a framing operation.</summary>
public enum FrameStatus
{
    /// <summary>A complete frame is available.</summary>
    Complete = 0,
    /// <summary>More input is required.</summary>
    Incomplete = 1,
    /// <summary>The frame is invalid.</summary>
    Invalid = 2,
    /// <summary>The frame exceeds a configured limit.</summary>
    LimitExceeded = 3,
    /// <summary>Input was skipped while recovering a frame boundary.</summary>
    Recovered = 4
}
