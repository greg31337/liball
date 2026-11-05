namespace rtp;

/// <summary>
/// Identifies a session event returned to the application.
/// </summary>
public enum RtpSessionEventType
{
    /// <summary>A source was added or changed.</summary>
    Source = 0,

    /// <summary>Packet loss was detected or finalized.</summary>
    Loss = 1,

    /// <summary>A repair action was requested or completed.</summary>
    Repair = 2,

    /// <summary>An SSRC collision or transport loop was detected.</summary>
    Collision = 3,

    /// <summary>A source or operation timed out.</summary>
    Timeout = 4,

    /// <summary>A source entered the goodbye state.</summary>
    Goodbye = 5,

    /// <summary>An RTCP report was produced or processed.</summary>
    Report = 6,

    /// <summary>A recoverable input or processing error occurred.</summary>
    RecoverableError = 7
}
