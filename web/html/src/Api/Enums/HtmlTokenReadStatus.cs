namespace html;
/// <summary>Identifies whether an incremental token read produced a token, needs more input, completed, or stopped.</summary>
public enum HtmlTokenReadStatus
{
    /// <summary>Signals that another input chunk is required.</summary>
    NeedMoreInput = 0,
    /// <summary>Signals that a token is available from the read result.</summary>
    TokenAvailable = 1,
    /// <summary>Signals that tokenization has completed.</summary>
    Complete = 2,
    /// <summary>Signals that tokenization stopped at a configured resource limit.</summary>
    LimitExceeded = 3,
    /// <summary>Signals that tokenization stopped because cancellation was requested.</summary>
    Cancelled = 4,
    /// <summary>Signals that tokenization stopped because its input source failed.</summary>
    Failed = 5
}
