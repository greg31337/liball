namespace html;

/// <summary>Identifies whether a serialization operation completed or stopped at its output limit.</summary>
public enum HtmlSerializationStatus
{
    /// <summary>Indicates that all requested markup was written.</summary>
    Complete = 0,
    /// <summary>Indicates that the configured output-character limit stopped serialization.</summary>
    LimitExceeded = 1
}
