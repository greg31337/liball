namespace json;

/// <summary>Describes the outcome of a streaming read operation.</summary>
public enum JsonReadStatus
{
    /// <summary>A token is available to the caller.</summary>
    TokenAvailable = 0,

    /// <summary>Additional input is required to continue.</summary>
    NeedMoreData = 1,

    /// <summary>The current JSON document is complete.</summary>
    DocumentComplete = 2,

    /// <summary>The complete framed input stream has been consumed.</summary>
    StreamComplete = 3,

    /// <summary>Processing was paused by the token handler.</summary>
    Paused = 4,

    /// <summary>Processing was stopped by the token handler.</summary>
    Stopped = 5,

    /// <summary>Processing failed with a diagnostic.</summary>
    Error = 6
}
