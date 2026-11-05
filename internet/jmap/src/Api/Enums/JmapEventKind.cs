namespace jmap;

/// <summary>
/// Classifies records read from a JMAP EventSource stream.
/// </summary>
public enum JmapEventKind
{
    /// <summary>The event name is not recognized by this version of the library.</summary>
    Unknown = 0,

    /// <summary>The event carries a JMAP StateChange object.</summary>
    State = 1,

    /// <summary>The event is a JMAP keep-alive ping.</summary>
    Ping = 2,

    /// <summary>The record is an EventSource comment.</summary>
    Comment = 3
}
