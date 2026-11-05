namespace json;

/// <summary>Describes the state of a JSON write operation.</summary>
public enum JsonWriteStatus
{
    /// <summary>The writer can accept more JSON content.</summary>
    Ready = 0,

    /// <summary>The requested output is complete.</summary>
    Complete = 1,

    /// <summary>The write operation failed with a diagnostic.</summary>
    Error = 2
}
